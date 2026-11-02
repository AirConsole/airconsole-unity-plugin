#!/usr/bin/env python3
"""Release test of the plugin in each supported Unity version, with the installed Unity editors.

Step 1, compile: a copy of this project runs the EditMode and PlayMode suites and builds the player
(NDream.Unity.Builder) for each target.
Step 2, import: exports the .unitypackage (NDream.Unity.Packager.Export) in the package Unity version of
create-release.yaml. Then a new project per Unity version, with the packages of a Unity Hub 3D project and the Unity
default settings, imports it, builds the player for each target and runs both suites with the plugin tests.

Prints one line per check. The exit code is 1 when a check fails.

Output, in the run directory (--run-dir, default Logs/ReleaseTest/<yyyyMMdd-HHmmss> in this project):
  report.md, report.json                      the checked aspects: commit, plugin version, Unity versions, package, checks
  logs/compile-<unity>/<target>-<suite>.log   Unity log of a test run; <target>-<suite>.xml has its test results
  logs/compile-<unity>/<target>-Build.log     Unity log of a player build
  logs/export/export.log                      Unity log of the package export
  logs/import-<unity>/*.log, *.xml            Unity logs and test results of the import into a new project
  airconsole-unity-plugin-v<version>.unitypackage   the exported package
The project copies (compile-<unity>, export, import-<unity>) are deleted after use, unless --keep is set.

Usage:
  scripts/release_test.py                    # newest installed patch of each LTS line: 2022.3, 6000.0, 6000.3, 6000.6
  scripts/release_test.py --unity 2022.3.62f3 --steps compile --targets Android --no-build
  scripts/release_test.py --allow-dirty      # test uncommitted changes (the report names the commit and the state)

Needs about 8 GB free disk for the IL2CPP builds. Set UNITY_HUB_EDITORS when the editors are not in the default folder.
"""
import argparse
import hashlib
import io
import json
import os
import platform
import re
import shutil
import signal
import subprocess
import sys
import tarfile
import time
import xml.etree.ElementTree as ElementTree
from contextlib import contextmanager
from dataclasses import asdict, dataclass, field
from datetime import datetime
from pathlib import Path

from release import ROOT, TIMEZONE, read_version

EDITORS_DIR = Path(os.environ.get("UNITY_HUB_EDITORS", "/Applications/Unity/Hub/Editor"))
LTS_LINES = ("2022.3", "6000.0", "6000.3", "6000.6")
TARGETS = ("WebGL", "Android")
SUITES = ("EditMode", "PlayMode")
TARGET_MODULES = {"WebGL": "WebGLSupport", "Android": "AndroidPlayer"}
COPY_EXCLUDES = {"Library", "Logs", "Temp", "obj", "UserSettings", "TestBuilds", "Builds", ".git", ".trail", ".idea"}
TEMPLATE_VARIABLES = ("PROGRESS_BAR_BACKGROUND_COLOR", "PROGRESS_BAR_COLOR", "PROGRESS_BAR_HEIGHT",
                      "PROGRESS_BAR_OFFSET_LEFT", "PROGRESS_BAR_OFFSET_TOP", "PROGRESS_BAR_WIDTH")
MIN_FREE_GB = 8
STOP_FREE_GB = 2
UNITY_TIMEOUT_SECONDS = 3600
WATCHED_IMPORT_TIMEOUT_SECONDS = 1200

# Quits the batchmode editor when ProjectCodeUpdater has imported the code package. [InitializeOnLoad] registers the poll
# again after each domain reload of the two-stage import.
IMPORT_WATCHER = """using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ReleaseTestImportWatcher {
    private const string StartKey = "ReleaseTestImportWatcher.Start";
    private static double settledSince = -1;

    static ReleaseTestImportWatcher() {
        if (!Application.isBatchMode || System.Environment.GetEnvironmentVariable("AC_IMPORT_WATCH") != "1") {
            return;
        }
        if (SessionState.GetFloat(StartKey, 0) == 0) {
            SessionState.SetFloat(StartKey, (float)EditorApplication.timeSinceStartup + 1);
        }
        EditorApplication.update += Poll;
    }

    private static void Poll() {
        double now = EditorApplication.timeSinceStartup;
        if (now - SessionState.GetFloat(StartKey, 0) > 900) {
            Debug.LogError("[ReleaseTest] TIMEOUT: the code package import did not finish");
            EditorApplication.Exit(2);
        }
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) {
            settledSince = -1;
            return;
        }
        if (settledSince < 0 || now - settledSince < 20) {
            settledSince = settledSince < 0 ? now : settledSince;
            return;
        }
        string airconsole = Path.Combine(Application.dataPath, "AirConsole");
        bool done = !File.Exists(Path.Combine(airconsole, "ProjectCodeUpdater.cs"))
                    && !File.Exists(Path.Combine(airconsole, "airconsole-code.unitypackage"))
                    && File.Exists(Path.Combine(airconsole, "scripts", "Runtime", "AirConsole.cs"));
        if (EditorUtility.scriptCompilationFailed) {
            Debug.LogError("[ReleaseTest] FAIL: script compilation failed after the import");
            EditorApplication.Exit(1);
        } else if (done) {
            Debug.Log("[ReleaseTest] PASS: import complete and compiled");
            EditorApplication.Exit(0);
        } else {
            settledSince = now;
        }
    }
}
"""

# A new project has no build scene, and its default Android package name comes from the folder name.
PROJECT_SETUP = """using UnityEditor;
using UnityEditor.Build;

public static class ReleaseTestSetup {
    public static void Apply() {
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/AirConsole/examples/basic/basic.unity", true) };
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.airconsole.releasetest");
        AssetDatabase.SaveAssets();
    }
}
"""


@dataclass
class Check:
    step: str
    unity: str
    name: str
    ok: bool
    detail: str = ""
    seconds: float = 0.0


@dataclass
class Report:
    started: str
    commit: str
    branch: str
    clean: bool
    status: str
    plugin_version: str
    host: str
    run_dir: str
    editors: dict = field(default_factory=dict)
    package: dict = field(default_factory=dict)
    checks: list = field(default_factory=list)

    def add(self, check: Check) -> Check:
        self.checks.append(check)
        seconds = f"{check.seconds:6.0f}s" if check.seconds else ""
        say(f"  {'PASS' if check.ok else 'FAIL'}  {check.name:<38} {check.detail:<58} {seconds}")
        return check

    @property
    def failed(self) -> list:
        return [check for check in self.checks if not check.ok]


def say(text: str) -> None:
    # After SIGHUP or a closed pipe, stdout is gone; the report must still be written.
    try:
        print(text, flush=True)
    except OSError:
        pass


def heading(text: str) -> None:
    say(f"\n== {text}")


def git(*args: str) -> str:
    return subprocess.run(["git", "-C", str(ROOT), *args], capture_output=True, text=True, check=True).stdout.strip()


def free_gb(path: Path) -> float:
    return shutil.disk_usage(path).free / 1e9


def is_unity_6(version: str) -> bool:
    return int(version.split(".")[0]) >= 6000


def expected_template(version: str) -> str:
    return "AirConsole-U6" if is_unity_6(version) else "AirConsole-2020"


def unity_binary(version: str) -> Path:
    return EDITORS_DIR / version / "Unity.app/Contents/MacOS/Unity"


def installed_editors() -> list:
    return sorted(path.name for path in EDITORS_DIR.glob("*") if unity_binary(path.name).exists())


def patch_key(version: str) -> tuple:
    return tuple(int(part) for part in re.findall(r"\d+", version))


def default_editors() -> list:
    installed = installed_editors()
    newest = []
    for line in LTS_LINES:
        patches = [version for version in installed if version.startswith(line + ".")]
        if patches:
            newest.append(max(patches, key=patch_key))
    return newest


def editor_modules(version: str) -> list:
    engines = EDITORS_DIR / version / "PlaybackEngines"
    return sorted(path.name for path in engines.glob("*")) if engines.exists() else []


def package_unity_version() -> str:
    workflow = (ROOT / ".github/workflows/create-release.yaml").read_text()
    return re.search(r"PACKAGE_UNITY_VERSION:\s*(\S+)", workflow).group(1)


def run_unity(version: str, project: Path | None, log: Path, *args: str, env: dict | None = None,
              timeout: int = UNITY_TIMEOUT_SECONDS) -> tuple:
    started = time.monotonic()
    project_args = ["-projectPath", str(project)] if project else []
    command = [str(unity_binary(version)), "-batchmode", "-nographics", *project_args, "-logFile", str(log), *args]
    # A process group of its own, so that a timeout or Ctrl-C also stops the asset import workers, il2cpp and Gradle.
    process = subprocess.Popen(command, env={**os.environ, **(env or {})}, start_new_session=True)
    try:
        returncode = process.wait(timeout=timeout)
    except subprocess.TimeoutExpired:
        os.killpg(process.pid, signal.SIGKILL)
        process.wait()
        returncode = -1
    except BaseException:
        os.killpg(process.pid, signal.SIGKILL)
        raise
    return returncode, time.monotonic() - started


def copy_project(destination: Path) -> None:
    def ignore(directory: str, names: list) -> set:
        if Path(directory) != ROOT:
            return set()
        return {name for name in names if name in COPY_EXCLUDES or name.endswith((".csproj", ".sln"))}

    shutil.rmtree(destination, ignore_errors=True)
    shutil.copytree(ROOT, destination, ignore=ignore)


def project_settings(project: Path) -> str:
    return (project / "ProjectSettings/ProjectSettings.asset").read_text()


def suite_counts(results: Path) -> dict:
    try:
        run = ElementTree.parse(results).getroot()
    except (OSError, ElementTree.ParseError):
        return {}
    counts: dict = {key: int(run.get(key, 0)) for key in ("total", "passed", "failed", "skipped")}
    counts["failures"] = [case.get("fullname") for case in run.iter("test-case") if case.get("result") == "Failed"]
    return counts


def run_suite(report: Report, step: str, version: str, project: Path, logs: Path, target: str, suite: str) -> Check:
    results = logs / f"{target}-{suite}.xml"
    returncode, seconds = run_unity(version, project, logs / f"{target}-{suite}.log", "-runTests", "-buildTarget", target,
                                    "-testPlatform", suite, "-testResults", str(results))
    counts = suite_counts(results)
    if not counts:
        return report.add(Check(step, version, f"{target} {suite}", False, f"no results, exit code {returncode}", seconds))
    detail = f"{counts['passed']}/{counts['total']} passed"
    if counts["skipped"]:
        detail += f", {counts['skipped']} skipped"
    if counts["failures"]:
        detail += ", failed: " + ", ".join(name.rsplit(".", 1)[-1] for name in counts["failures"][:3])
    ok = returncode == 0 and counts["failed"] == 0 and counts["total"] > 0
    return report.add(Check(step, version, f"{target} {suite}", ok, detail, seconds))


def build_player(report: Report, step: str, version: str, project: Path, logs: Path, target: str, name: str) -> Check:
    returncode, seconds = run_unity(version, project, logs / f"{target}-Build.log", "-quit", "-buildTarget", target,
                                    "-executeMethod", f"NDream.Unity.Builder.Build{target}", "-buildName", name)
    if target == "WebGL":
        artifact = project / "TestBuilds/Web" / name / "index.html"
        size = sum(path.stat().st_size for path in artifact.parent.rglob("*") if path.is_file()) if artifact.exists() else 0
    else:
        artifact = project / "TestBuilds/Android" / f"{name}.apk"
        size = artifact.stat().st_size if artifact.exists() else 0
    ok = returncode == 0 and artifact.exists()
    detail = f"{artifact.name}, {size / 1e6:.0f} MB" if artifact.exists() else f"no {artifact.name}, exit code {returncode}"
    for folder in ("Library/Bee", "Library/Il2cppBuildCache", "TestBuilds", "Temp"):
        shutil.rmtree(project / folder, ignore_errors=True)
    return report.add(Check(step, version, f"{target} player build", ok, detail, seconds))


def enough_disk(report: Report, step: str, version: str) -> bool:
    free = free_gb(Path(report.run_dir))
    if free >= STOP_FREE_GB:
        return True
    report.add(Check(step, version, "free disk", False, f"{free:.1f} GB free, stopped below {STOP_FREE_GB} GB"))
    return False


def preflight(report: Report, editors: list, targets: list, allow_dirty: bool, all_lines: bool) -> bool:
    heading("Preflight")
    report.add(Check("preflight", "", "git working tree", report.clean or allow_dirty,
                     f"{report.branch} @ {report.commit[:7]}, " + ("clean" if report.clean else "uncommitted changes")))
    unreleased = "## [Unreleased]" in (ROOT / "CHANGELOG.md").read_text()
    report.add(Check("preflight", "", "plugin version", unreleased, f"Settings.VERSION {report.plugin_version}, CHANGELOG "
                     + ("has [Unreleased]" if unreleased else "has no [Unreleased] section")))
    report.add(Check("preflight", "", "Unity versions", bool(editors),
                     ", ".join(editors) if editors else f"no editor in {EDITORS_DIR}"))
    if all_lines:
        for line in LTS_LINES:
            if not any(version.startswith(line + ".") for version in editors):
                report.add(Check("preflight", "", f"Unity {line}", False, f"no {line} editor in {EDITORS_DIR}"))
    for version in editors:
        modules = editor_modules(version)
        report.editors[version] = {"path": str(unity_binary(version)), "modules": modules}
        missing = [TARGET_MODULES[target] for target in targets if TARGET_MODULES[target] not in modules]
        report.add(Check("preflight", version, f"Unity {version}", unity_binary(version).exists() and not missing,
                         "missing " + ", ".join(missing) if missing else "editor and target modules installed"))
    free = free_gb(Path(report.run_dir))
    report.add(Check("preflight", "", "free disk", free >= MIN_FREE_GB, f"{free:.0f} GB free, {MIN_FREE_GB} GB needed"))
    return not report.failed


@contextmanager
def project_copy(project: Path, keep: bool):
    """Deletes the project copy after the step, also when the step stops early, unless --keep is set."""
    try:
        yield
    finally:
        if not keep:
            shutil.rmtree(project, ignore_errors=True)


def compile_step(report: Report, version: str, targets: list, build: bool, run_dir: Path) -> None:
    heading(f"Step 1, compile: Unity {version}")
    project, logs = run_dir / f"compile-{version}", run_dir / "logs" / f"compile-{version}"
    logs.mkdir(parents=True, exist_ok=True)
    copy_project(project)
    settings = project_settings(project)
    if is_unity_6(version) and "webGLTemplate: PROJECT:AirConsole-2020" in settings:
        # The plugin keeps a PROJECT template; in Unity 6 the developer selects AirConsole-U6.
        (project / "ProjectSettings/ProjectSettings.asset").write_text(
            settings.replace("webGLTemplate: PROJECT:AirConsole-2020", "webGLTemplate: PROJECT:AirConsole-U6"))
    for target in targets:
        for suite in SUITES:
            if enough_disk(report, "compile", version):
                run_suite(report, "compile", version, project, logs, target, suite)
        if build and enough_disk(report, "compile", version):
            build_player(report, "compile", version, project, logs, target, f"release-test-{version}")


def unitypackage_paths(data: bytes) -> dict:
    """Maps each asset path of a .unitypackage to its tar folder."""
    paths = {}
    with tarfile.open(fileobj=io.BytesIO(data), mode="r:gz") as package:
        for member in package.getmembers():
            if member.name.endswith("/pathname"):
                path = package.extractfile(member).read().decode().splitlines()[0].strip()
                paths[path] = member.name.rsplit("/", 1)[0]
    return paths


def export_package(report: Report, run_dir: Path) -> Path | None:
    version = package_unity_version()
    heading(f"Step 2, export: Unity {version} (PACKAGE_UNITY_VERSION)")
    if not unity_binary(version).exists():
        report.add(Check("export", version, "package Unity version", False, f"Unity {version} is not installed"))
        return None
    project, logs = run_dir / "export", run_dir / "logs" / "export"
    logs.mkdir(parents=True, exist_ok=True)
    copy_project(project)
    (project / "Builds").mkdir()
    returncode, seconds = run_unity(version, project, logs / "export.log", "-quit", "-buildTarget", "WebGL",
                                    "-executeMethod", "NDream.Unity.Packager.Export")
    exported = project / "Builds" / f"airconsole-unity-plugin-v{report.plugin_version}.unitypackage"
    if not report.add(Check("export", version, "export .unitypackage", returncode == 0 and exported.exists(),
                            exported.name if exported.exists() else f"missing, exit code {returncode}", seconds)).ok:
        return None
    package = run_dir / exported.name
    shutil.copy2(exported, package)

    data = package.read_bytes()
    outer = unitypackage_paths(data)
    code_package = "Assets/AirConsole/airconsole-code.unitypackage"
    report.add(Check("export", version, "package has the code updater",
                     {"Assets/AirConsole/ProjectCodeUpdater.cs", code_package} <= outer.keys(),
                     f"{len(outer)} entries"))
    inner = {}
    if code_package in outer:
        with tarfile.open(fileobj=io.BytesIO(data), mode="r:gz") as tar:
            inner = unitypackage_paths(tar.extractfile(f"{outer[code_package]}/asset").read())
    report.add(Check("export", version, "code package has the runtime",
                     "Assets/AirConsole/scripts/Runtime/AirConsole.cs" in inner, f"{len(inner)} entries"))
    tests = [path for path in inner if "/scripts/Tests" in path]
    report.add(Check("export", version, "code package has no tests", not tests, f"{len(tests)} test paths"))
    report.package = {"file": str(package), "unity": version, "entries": len(outer), "code_entries": len(inner),
                      "sha256": hashlib.sha256(data).hexdigest()}
    return package


def hub_3d_packages(version: str) -> dict:
    """The packages that a Unity Hub 3D project adds to the modules, at the editor's default versions."""
    manifest = json.loads((EDITORS_DIR / version / "Unity.app/Contents/Resources/PackageManager/Editor/manifest.json")
                          .read_text())
    defaults = manifest.get("packages") or manifest.get("dependencies") or {}
    wanted = ["com.unity.ugui", "com.unity.test-framework"] + ([] if is_unity_6(version) else ["com.unity.textmeshpro"])
    packages = {}
    for name in wanted:
        entry = defaults.get(name)
        if entry:
            packages[name] = entry["version"] if isinstance(entry, dict) else entry
    return packages


def import_step(report: Report, version: str, package: Path, targets: list, build: bool, run_dir: Path) -> None:
    heading(f"Step 2, import: Unity {version}")
    project, logs = run_dir / f"import-{version}", run_dir / "logs" / f"import-{version}"
    logs.mkdir(parents=True, exist_ok=True)
    shutil.rmtree(project, ignore_errors=True)

    returncode, seconds = run_unity(version, None, logs / "create.log", "-quit", "-createProject", str(project))
    if not report.add(Check("import", version, "create new project", returncode == 0, f"exit code {returncode}", seconds)).ok:
        return
    manifest_path = project / "Packages/manifest.json"
    manifest = json.loads(manifest_path.read_text())
    packages = hub_3d_packages(version)
    manifest["dependencies"].update(packages)
    manifest_path.write_text(json.dumps(manifest, indent=2))
    report.editors[version]["hub_3d_packages"] = packages
    editor_folder = project / "Assets/Editor"
    editor_folder.mkdir(parents=True)
    (editor_folder / "ReleaseTestImportWatcher.cs").write_text(IMPORT_WATCHER)
    (editor_folder / "ReleaseTestSetup.cs").write_text(PROJECT_SETUP)

    returncode, seconds = run_unity(version, project, logs / "importPackage.log", "-quit", "-importPackage", str(package))
    if not report.add(Check("import", version, "import .unitypackage", returncode == 0, f"exit code {returncode}", seconds)).ok:
        return
    returncode, seconds = run_unity(version, project, logs / "codePackageImport.log", env={"AC_IMPORT_WATCH": "1"},
                                    timeout=WATCHED_IMPORT_TIMEOUT_SECONDS)
    airconsole = project / "Assets/AirConsole"
    imported = (returncode == 0 and not (airconsole / "ProjectCodeUpdater.cs").exists()
                and (airconsole / "scripts/Runtime/AirConsole.cs").exists())
    if not report.add(Check("import", version, "code package import compiles", imported,
                            "updater removed, runtime present" if imported else f"exit code {returncode}", seconds)).ok:
        return

    shutil.copytree(ROOT / "Assets/AirConsole/scripts/Tests", airconsole / "scripts/Tests", dirs_exist_ok=True)
    shutil.copy2(ROOT / "Assets/AirConsole/scripts/Tests.meta", airconsole / "scripts/Tests.meta")
    returncode, seconds = run_unity(version, project, logs / "setup.log", "-quit", "-buildTarget", "WebGL",
                                    "-executeMethod", "ReleaseTestSetup.Apply")
    report.add(Check("import", version, "set build scene", returncode == 0, "examples/basic/basic.unity", seconds))

    settings = project_settings(project)
    template = re.search(r"webGLTemplate: (\S+)", settings).group(1)
    report.add(Check("import", version, "Unity template replaced",
                     template == f"PROJECT:{expected_template(version)}", f"webGLTemplate {template}"))
    missing = [key for key in TEMPLATE_VARIABLES if f"{key}:" not in settings]
    report.add(Check("import", version, "template variables registered", not missing,
                     "6 PROGRESS_BAR_* keys in m_TemplateCustomTags" if not missing else "missing " + ", ".join(missing)))

    if build:
        for target in targets:
            if enough_disk(report, "import", version):
                check = build_player(report, "import", version, project, logs, target, f"release-test-new-{version}")
                warning = "AirConsole recommends 'ASTC' or 'ETC2'" in (logs / f"{target}-Build.log").read_text(errors="ignore")
                if check.ok and warning:
                    report.add(Check("import", version, f"{target} texture check only warns", True,
                                     "warning logged, build continued"))
    for target in targets:
        for suite in SUITES:
            if enough_disk(report, "import", version):
                run_suite(report, "import", version, project, logs, target, suite)


def write_reports(report: Report, run_dir: Path) -> None:
    (run_dir / "report.json").write_text(json.dumps(asdict(report), indent=2))
    lines = [
        f"# AirConsole Unity plugin release test, {report.started}",
        "",
        f"- Verdict: **{'PASS' if not report.failed else 'FAIL'}**, {len(report.checks) - len(report.failed)}/"
        f"{len(report.checks)} checks passed",
        f"- Plugin: Settings.VERSION {report.plugin_version} with the CHANGELOG [Unreleased] section",
        f"- Commit: {report.branch} @ {report.commit} ({'clean' if report.clean else 'with uncommitted changes'})",
        f"- Host: {report.host}",
        "",
        "## Unity versions",
        "",
        "| Unity | Modules | Hub 3D packages in the new project |",
        "|---|---|---|",
    ]
    for version, editor in report.editors.items():
        packages = ", ".join(f"{name} {value}" for name, value in editor.get("hub_3d_packages", {}).items()) or "–"
        lines.append(f"| {version} | {', '.join(editor['modules'])} | {packages} |")
    if report.package:
        lines += ["", "## Package", "",
                  f"- {Path(report.package['file']).name}, exported in Unity {report.package['unity']}",
                  f"- {report.package['entries']} entries, code package {report.package['code_entries']} entries",
                  f"- sha256 {report.package['sha256']}"]
    lines += ["", "## Checks", "", "| Step | Unity | Check | Result | Detail | Time |", "|---|---|---|---|---|---|"]
    for check in report.checks:
        seconds = f"{check.seconds:.0f}s" if check.seconds else ""
        lines.append(f"| {check.step} | {check.unity} | {check.name} | {'PASS' if check.ok else '**FAIL**'} | "
                     f"{check.detail} | {seconds} |")
    lines += ["", "Logs and test results: `logs/` next to this report."]
    (run_dir / "report.md").write_text("\n".join(lines) + "\n")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--unity", nargs="+", help="Unity versions (default: newest installed patch of each LTS line)")
    parser.add_argument("--steps", nargs="+", choices=("compile", "import"), default=["compile", "import"])
    parser.add_argument("--targets", nargs="+", choices=TARGETS, default=list(TARGETS))
    parser.add_argument("--no-build", action="store_true", help="skip the player builds")
    parser.add_argument("--run-dir", type=Path, help="default: Logs/ReleaseTest/<timestamp> in this project")
    parser.add_argument("--keep", action="store_true", help="keep the project copies")
    parser.add_argument("--allow-dirty", action="store_true", help="test a working tree with uncommitted changes")
    args = parser.parse_args()

    started = datetime.now(TIMEZONE)
    run_dir = (args.run_dir or ROOT / "Logs/ReleaseTest" / started.strftime("%Y%m%d-%H%M%S")).resolve()
    # The project copies are made from ROOT: a run directory in ROOT outside Logs (excluded from the copy) would copy itself.
    if run_dir.is_relative_to(ROOT) and not run_dir.is_relative_to(ROOT / "Logs"):
        sys.exit(f"--run-dir must be outside this project or in its Logs folder: {run_dir}")
    run_dir.mkdir(parents=True, exist_ok=True)
    status = git("status", "--porcelain")
    report = Report(started=started.isoformat(timespec="seconds"), commit=git("rev-parse", "HEAD"),
                    branch=git("rev-parse", "--abbrev-ref", "HEAD"), clean=not status, status=status,
                    plugin_version=read_version(), host=f"{platform.system()} {platform.mac_ver()[0] or platform.release()}, "
                                                        f"{platform.machine()}", run_dir=str(run_dir))
    editors = args.unity or default_editors()
    say(f"Release test of {report.plugin_version} [Unreleased] at {report.commit[:7]} in Unity {', '.join(editors)}")
    say(f"Run directory: {run_dir}")
    say(f"  Report:      {run_dir / 'report.md'} (and report.json), written at the end")
    say(f"  Unity logs:  {run_dir / 'logs'}/<step>-<unity>/ (test results as .xml next to them)")

    for signal_number in (signal.SIGTERM, signal.SIGHUP):
        signal.signal(signal_number, lambda number, frame: sys.exit(128 + number))
    try:
        if preflight(report, editors, args.targets, args.allow_dirty, all_lines=not args.unity):
            if "compile" in args.steps:
                for version in editors:
                    with project_copy(run_dir / f"compile-{version}", args.keep):
                        compile_step(report, version, args.targets, not args.no_build, run_dir)
            if "import" in args.steps:
                with project_copy(run_dir / "export", args.keep):
                    package = export_package(report, run_dir)
                if package:
                    for version in editors:
                        with project_copy(run_dir / f"import-{version}", args.keep):
                            import_step(report, version, package, args.targets, not args.no_build, run_dir)
    except BaseException as error:
        report.add(Check("run", "", "run completed", False, f"stopped by {type(error).__name__}: {error}"))
    finally:
        # The project copies are made during the run: a change in the working tree after the start is a different state.
        unchanged = git("status", "--porcelain") == report.status
        report.add(Check("run", "", "working tree unchanged during the run", unchanged,
                         "same git status as at the start" if unchanged else "files changed during the run"))
        write_reports(report, run_dir)
    heading("Result")
    say(f"  {'PASS' if not report.failed else 'FAIL'}: {len(report.checks) - len(report.failed)}/{len(report.checks)}"
        f" checks passed")
    for check in report.failed:
        say(f"  FAIL  {check.step} {check.unity} {check.name}: {check.detail}")
    say(f"  Report: {run_dir / 'report.md'}")
    return 1 if report.failed else 0


if __name__ == "__main__":
    sys.exit(main())
