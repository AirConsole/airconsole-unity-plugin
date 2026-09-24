#!/usr/bin/env python3
"""Run the Create Release workflow locally with the installed Unity editor.

Same steps as .github/workflows/create-release.yaml: WebGL test build, package
export (dates the CHANGELOG section), validation, the release PR, and a preview
of what merging that PR publishes (tag, GitHub release, Release Log row).

Usage:
  scripts/release_local.py --dry-run   # build and validate; only print the branch, commit, push, PR,
                                       # tag and release commands; then reset the files the export changed
  scripts/release_local.py             # on master: also create release/v{VERSION}, commit, push and open the PR

Close the project in the Unity Editor first. Set UNITY to use another editor binary.
"""
import argparse
import os
import re
import subprocess
import sys

from release import ROOT, SETTINGS, git, open_release_pr, package_path, publish, read_version, run, tag_on_origin


def unity_editor() -> str:
    if os.environ.get("UNITY"):
        return os.environ["UNITY"]
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
    if not dry_run and branch != "master":
        sys.exit("A release must run on master. Use --dry-run to check another branch.")

    version = read_version(SETTINGS.read_text())
    tag = f"v{version}"
    if tag_on_origin(tag):
        sys.exit(f"Release with version {tag} already exists.")

    build = f"release-local-{tag}"
    unity("NDream.Unity.Builder.BuildWebGL", "-buildName", build)
    if not (ROOT / "TestBuilds/Web" / build / "index.html").is_file():
        sys.exit(f"Build validation failed for WebGL: TestBuilds/Web/{build}/index.html is missing")

    unity("NDream.Unity.Packager.Export")
    if not package_path(tag).is_file():
        sys.exit(f"Build validation of airconsole-unity-plugin-{tag} unitypackage failed")

    print("\n== Release preview (runs in CI when the release PR is merged)")
    publish(dry_run=True)

    print("\n== Release PR")
    open_release_pr(dry_run)
    if not dry_run:
        run(["git", "switch", branch], dry_run)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--dry-run", action="store_true",
                        help="build and validate; print the commit, push, PR, tag and release commands")
    dry_run = parser.parse_args().dry_run

    # Clean start: the release commits only Builds/ and CHANGELOG.md, and the reset below may only undo this run.
    if git("status", "--porcelain", "--untracked-files=no"):
        sys.exit("Commit or stash your changes to tracked files first.")
    try:
        release(dry_run)
    finally:
        # The export also rewrites ProjectSettings and template .meta files; a dry run also drops the package
        # and CHANGELOG changes.
        git("restore", "--staged", "--worktree", "--", ".")
        print("\nReset the files this run changed.")
        if leftovers := git("status", "--porcelain"):
            print(f"Untracked leftovers:\n{leftovers}")


if __name__ == "__main__":
    main()
