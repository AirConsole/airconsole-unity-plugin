#!/usr/bin/env python3
"""Release steps for the version in Settings.cs, shared by CI and scripts/release_local.py.

On master, Settings.VERSION is the last released version. The release raises it with --bump.

Publish (default): create the GitHub release and tag v{VERSION} for HEAD with the
committed .unitypackage and the CHANGELOG.md section as notes.
--bump patch|minor|major: write the next version to Settings.VERSION (CI runs this before the Unity export).
With --check or --open-pr, the step runs for the next version.
--open-pr: date the '## [Unreleased]' CHANGELOG section as '## [VERSION] - yyyy-MM-dd', commit it
with Settings.cs and the exported package to release/v{VERSION} and open the "Release v{VERSION}" PR.

Usage:
  scripts/release.py --bump patch --open-pr --dry-run   # preview the notes and PR on master, change nothing
                                                        # (no Unity needed)
  scripts/release.py --bump patch          # CI runs this before the Unity export
  scripts/release.py --open-pr             # CI runs this after the Unity export
  scripts/release.py --dry-run             # on release/v{VERSION}: preview the tag, release and notes, change nothing
  scripts/release.py                       # needs `gh` auth (CI runs this when the release PR merges)
  scripts/release.py --check               # fail when the tag or the release branch is already on origin
"""
import argparse
import os
import re
import shlex
import subprocess
import sys
import tempfile
from datetime import datetime
from pathlib import Path
from zoneinfo import ZoneInfo

# CI installs the version in .tool-versions; `mise install` does the same locally.
if sys.version_info < (3, 12):
    sys.exit(f"Python 3.12 or later is needed, this is {sys.version.split()[0]}. Run `mise install` in the repo.")

ROOT = Path(__file__).resolve().parent.parent
SETTINGS = ROOT / "Assets/AirConsole/scripts/Runtime/Settings.cs"
CHANGELOG = ROOT / "CHANGELOG.md"
REPO_URL = "https://github.com/AirConsole/airconsole-unity-plugin"
TIMEZONE = ZoneInfo("Europe/Zurich")
VERSION_PATTERN = r'(public const string VERSION = ")(\d+\.\d+\.\d+)(")'
BUMPS = ("patch", "minor", "major")


def read_version() -> str:
    match = re.search(VERSION_PATTERN, SETTINGS.read_text())
    if not match:
        sys.exit("Settings.VERSION is missing or not MAJOR.MINOR.PATCH.")
    return match.group(2)


def next_version(version: str, bump: str) -> str:
    major, minor, patch = (int(part) for part in version.split("."))
    if bump == "major":
        return f"{major + 1}.0.0"
    if bump == "minor":
        return f"{major}.{minor + 1}.0"
    return f"{major}.{minor}.{patch + 1}"


def write_version(version: str) -> None:
    SETTINGS.write_text(re.sub(VERSION_PATTERN, rf"\g<1>{version}\g<3>", SETTINGS.read_text(), count=1))


def release_notes(changelog: str, version: str) -> str:
    """Return the body of the `## [version] - yyyy-MM-dd` section."""
    match = re.search(
        rf"^## \[{re.escape(version)}\] - \d{{4}}-\d{{2}}-\d{{2}}\n(.*?)(?=^## \[|\Z)",
        changelog,
        re.MULTILINE | re.DOTALL,
    )
    if not match:
        sys.exit(f"CHANGELOG.md has no '## [{version}] - yyyy-MM-dd' section. "
                 "Keep the notes under '## [Unreleased]'; the release PR (scripts/release.py --open-pr) dates them.")
    return match.group(1).strip()


def stamp_changelog(changelog: str, version: str, date: str) -> str:
    """Date '## [Unreleased]' as '## [version] - date' and open a new, empty Unreleased section above it."""
    return re.sub(r"^## \[Unreleased\]", f"## [Unreleased]\n\n## [{version}] - {date}", changelog,
                  count=1, flags=re.MULTILINE)


def git(*args: str) -> str:
    return subprocess.run(["git", *args], cwd=ROOT, check=True, stdout=subprocess.PIPE, text=True).stdout.strip()


def run(cmd: list[str], dry_run: bool) -> None:
    """Run a command that changes git or GitHub. A dry run only prints it."""
    print(("would run: " if dry_run else "+ ") + shlex.join(cmd))
    if not dry_run:
        subprocess.run(cmd, cwd=ROOT, check=True)


def write_temp(text: str) -> str:
    with tempfile.NamedTemporaryFile("w", suffix=".md", delete=False) as file:
        file.write(text)
    return file.name


def tag_on_origin(tag: str) -> bool:
    return bool(git("ls-remote", "--tags", "origin", f"refs/tags/{tag}"))


def next_release(bump: str) -> str:
    """Return the version after Settings.VERSION. That version must be released, so that no release is skipped."""
    version = read_version()
    if not tag_on_origin(f"v{version}"):
        sys.exit(f"v{version} is not tagged on origin. Rerun the publish job of its release PR, "
                 "or revert the release merge on master.")
    return next_version(version, bump)


def check_unreleased(version: str) -> None:
    """Exit before the Unity build when a release or an open release PR already uses this version."""
    # --open-pr must not date a second [VERSION] section, and entries added since then under Unreleased would
    # miss the notes.
    changelog = CHANGELOG.read_text()
    if f"## [{version}]" in changelog:
        sys.exit(f"CHANGELOG.md already has a [{version}] section. If {version} is released, run with --bump. "
                 "Otherwise move its notes under '## [Unreleased]', or revert the failed release merge.")
    unreleased = re.search(r"^## \[Unreleased\][ \t]*$(.*?)(?=^## \[|\Z)", changelog, re.MULTILINE | re.DOTALL)
    if not unreleased:
        sys.exit("CHANGELOG.md has no '## [Unreleased]' section to release.")
    # Only headings, comments or blank lines: nothing to release.
    if not re.search(r"^(?!#|<!--)\S", unreleased.group(1), re.MULTILINE):
        sys.exit("The '## [Unreleased]' section of CHANGELOG.md has no notes. There is nothing to release.")
    tag = f"v{version}"
    if tag_on_origin(tag):
        sys.exit(f"{tag} is already tagged on origin. Run the release with --bump.")
    # Each bump gives another branch name, so any open release branch blocks the next release.
    if branches := git("ls-remote", "--heads", "origin", "refs/heads/release/v*"):
        names = ", ".join(line.split("refs/heads/")[-1] for line in branches.splitlines())
        sys.exit(f"{names} is already on origin. Merge its PR, or close it and delete the branch.")


def release_branch(tag: str) -> str:
    return f"release/{tag}"


def package_path(tag: str) -> Path:
    return ROOT / "Builds" / f"airconsole-unity-plugin-{tag}.unitypackage"


def open_release_pr(dry_run: bool, version: str) -> None:
    """Date the CHANGELOG, commit it with Settings.cs and the exported package to release/v{VERSION}
    and open the release PR."""
    tag = f"v{version}"
    package = package_path(tag).relative_to(ROOT)
    missing = "" if package_path(tag).is_file() else " (missing: the Unity export creates it before this step)"
    if missing and not dry_run:
        sys.exit(f"{package} is missing. Run scripts/release_local.py or the Create Release workflow to export it.")
    check_unreleased(version)
    branch = release_branch(tag)
    now = datetime.now(TIMEZONE)
    changelog = stamp_changelog(CHANGELOG.read_text(), version, now.strftime("%Y-%m-%d"))
    notes = release_notes(changelog, version)
    body = (f"{notes}\n\n---\n"
            f"Merging this PR creates the {tag} tag and the GitHub release.\n"
            f"If master changes, do not update this branch: close this PR, delete {branch}, and run Create Release again.")
    if os.environ.get("GITHUB_ACTIONS") == "true":
        body += ("\nThis PR was opened with the workflow token, so the required checks do not start by themselves: "
                 "close and reopen it to run them.")
    base = git("rev-parse", "HEAD")
    print(f"Release:  {tag} from {base}\nPackage:  {package}{missing}")
    print(f"PR body:\n{body}\n")

    run(["git", "switch", "-C", branch], dry_run)
    if dry_run:
        print(f"would date '## [Unreleased]' in CHANGELOG.md as '## [{version}] - {now:%Y-%m-%d}'")
    else:
        CHANGELOG.write_text(changelog)
    # The glob also stages the deletion of the previous version's package, and no other file in Builds/.
    run(["git", "add", "-A", "--", "Builds/airconsole-unity-plugin-v*.unitypackage", "CHANGELOG.md",
         str(SETTINGS.relative_to(ROOT))], dry_run)
    # The publish job checks this line against the commit the release PR merges onto.
    run(["git", "commit", "-m", f"Release {tag}", "-m", f"Exported from {base}"], dry_run)
    run(["git", "push", "origin", branch], dry_run)
    run(["gh", "pr", "create", "--base", "master", "--head", branch, "--title", f"Release {tag}",
         "--body-file", write_temp(body)], dry_run)


def publish(dry_run: bool) -> None:
    """Create the tag and GitHub release for HEAD."""
    version = read_version()
    tag = f"v{version}"
    branch = release_branch(tag)
    # CI passes the head branch of the merged PR; any other release/v* branch must not publish this version.
    if os.environ.get("RELEASE_BRANCH", branch) != branch:
        sys.exit(f"{os.environ['RELEASE_BRANCH']} is not {branch}. Nothing to release.")
    if tag_on_origin(tag):
        sys.exit(f"{tag} is already tagged on origin. If the GitHub release is missing, add it by hand.")

    package = package_path(tag)
    missing = ""
    if not package.is_file():
        if not dry_run:
            sys.exit(f"{package.relative_to(ROOT)} is missing. Run the Create Release workflow to export it.")
        missing = " (missing: the Create Release export creates it)"

    notes = release_notes(CHANGELOG.read_text(), version)

    print(f"Release:  {tag} at {git('rev-parse', 'HEAD')}\nPackage:  {package.relative_to(ROOT)}{missing}")
    print(f"Notes:\n{notes}\n")

    # `gh release create` also creates and pushes the tag on --target.
    run(["gh", "release", "create", tag, str(package.relative_to(ROOT)), "--title", f"Release {version}",
         "--notes-file", write_temp(notes), "--target", git("rev-parse", "HEAD")], dry_run)
    if not dry_run:
        print(f"Released {REPO_URL}/releases/tag/{tag}")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--dry-run", action="store_true", help="validate and print the commands, change nothing")
    parser.add_argument("--open-pr", action="store_true", help="open the release PR instead of publishing")
    parser.add_argument("--check", action="store_true", help="only check that this version is not released yet")
    parser.add_argument("--bump", choices=BUMPS,
                        help="write the next version to Settings.VERSION; with --dry-run, only print it")
    args = parser.parse_args()
    # A check or the PR could fail after the write and leave the bump behind.
    if args.bump and (args.check or args.open_pr) and not args.dry_run:
        parser.error("--bump with --check or --open-pr needs --dry-run. Run --bump alone first.")
    version = read_version()
    if args.bump:
        released, version = version, next_release(args.bump)
        print(f"Version:  {released} -> {version}")
        if not args.dry_run:
            write_version(version)
    if args.check:
        check_unreleased(version)
    elif args.open_pr:
        open_release_pr(args.dry_run, version)
    elif not args.bump:
        publish(args.dry_run)


if __name__ == "__main__":
    main()
