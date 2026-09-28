#!/usr/bin/env python3
"""Run the Create Release workflow locally with the installed Unity editor.

Same steps as .github/workflows/create-release.yaml: WebGL test build, package
export, validation, and the release PR (dates the CHANGELOG section) with a
preview of the notes and the Release Log row that merging it publishes.
For that preview alone, without Unity: scripts/release.py --open-pr --dry-run

Usage:
  scripts/release_local.py --dry-run   # build and validate; only print the branch, commit, push and PR
                                       # commands; then reset the files the build and export changed
  scripts/release_local.py             # on master: also create release/v{VERSION}, commit, push and open the PR

Close the project in the Unity Editor first. Set UNITY or AC_UNITY_BIN to use another editor binary.
"""
import argparse
import os
import re
import subprocess
import sys

from release import ROOT, check_unreleased, git, open_release_pr, package_path, read_version

# The files the WebGL build, the package export and the release PR change. The final reset touches only these.
EXPORT_PATHS = ["Builds", "CHANGELOG.md", "ProjectSettings", "Assets/WebGLTemplates"]


def unity_editor() -> str:
    if editor := os.environ.get("UNITY") or os.environ.get("AC_UNITY_BIN"):
        return editor
    version = re.search(r"m_EditorVersion: (\S+)", (ROOT / "ProjectSettings/ProjectVersion.txt").read_text()).group(1)
    return f"/Applications/Unity/Hub/Editor/{version}/Unity.app/Contents/MacOS/Unity"


def unity(method: str, *args: str) -> None:
    # -buildTarget WebGL: without it the editor can crash while CheckPlatform switches the build target on load.
    log = ROOT / "Logs" / f"release-{method.rsplit('.', 1)[-1]}.log"
    print(f"\n== Unity {method} (log: {log.relative_to(ROOT)})")
    result = subprocess.run([unity_editor(), "-batchmode", "-quit", "-nographics", "-projectPath", str(ROOT),
                             "-buildTarget", "WebGL", "-executeMethod", method, "-logFile", str(log), *args])
    if result.returncode:
        sys.exit(f"{method} failed with exit code {result.returncode}. See {log}.")


def release(dry_run: bool) -> None:
    branch = git("rev-parse", "--abbrev-ref", "HEAD")
    if not dry_run:
        if branch != "master":
            sys.exit("A release must run on master. Use --dry-run to check another branch.")
        git("fetch", "origin", "master")
        if git("rev-parse", "HEAD") != git("rev-parse", "origin/master"):
            sys.exit("Local master is not origin/master. Pull or push first.")
    check_unreleased()

    tag = f"v{read_version()}"

    build = f"release-local-{tag}"
    unity("NDream.Unity.Builder.BuildWebGL", "-buildName", build)
    if not (ROOT / "TestBuilds/Web" / build / "index.html").is_file():
        sys.exit(f"Build validation failed for WebGL: TestBuilds/Web/{build}/index.html is missing")

    package_path(tag).unlink(missing_ok=True)  # a failed export must not pass on an older file
    unity("NDream.Unity.Packager.Export")
    if not package_path(tag).is_file():
        sys.exit(f"Build validation of airconsole-unity-plugin-{tag} unitypackage failed")

    print("\n== Release PR")
    open_release_pr(dry_run)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--dry-run", action="store_true",
                        help="build and validate; print the branch, commit, push and PR commands")
    dry_run = parser.parse_args().dry_run

    # Clean start: the release commits only Builds/ and CHANGELOG.md, and the reset below may only undo this run.
    if git("status", "--porcelain", "--untracked-files=no"):
        sys.exit("Commit or stash your changes to tracked files first.")
    start = git("rev-parse", "--abbrev-ref", "HEAD")
    try:
        release(dry_run)
    finally:
        # A real run leaves its changes committed on release/v{VERSION}; a dry run or a failure drops them.
        git("restore", "--staged", "--worktree", "--", *EXPORT_PATHS)
        if git("rev-parse", "--abbrev-ref", "HEAD") != start:
            git("switch", start)
        print(f"\nReset {', '.join(EXPORT_PATHS)} and returned to {start}.")
        if leftovers := git("status", "--porcelain"):
            print(f"Other changes, left as they are:\n{leftovers}")


if __name__ == "__main__":
    main()
