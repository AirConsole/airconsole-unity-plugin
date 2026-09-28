#!/usr/bin/env python3
"""Release steps for the version in Settings.cs, shared by CI and scripts/release_local.py.

Publish (default): create the GitHub release and tag v{VERSION} for HEAD with the
committed .unitypackage and the CHANGELOG.md section as notes, then insert a row at
the top of the year tab of the Release Log sheet.
--open-pr: date the '## [Unreleased]' CHANGELOG section as '## [VERSION] - yyyy-MM-dd', commit it
with the exported package to release/v{VERSION} and open the "Release v{VERSION}" PR.

Usage:
  scripts/release.py --open-pr --dry-run   # preview the notes, sheet row and PR on master, change nothing (no Unity needed)
  scripts/release.py --open-pr             # CI runs this after the Unity export
  scripts/release.py --dry-run             # preview the tag, release, notes and sheet row, change nothing (works on master)
  scripts/release.py                       # needs `gh` auth and GOOGLE_ACCESS_TOKEN (CI runs this when the release PR merges)
  scripts/release.py --check               # fail when the tag or the release branch is already on origin
"""
import argparse
import json
import os
import re
import shlex
import subprocess
import sys
import tempfile
import urllib.request
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
SHEET_ID = "17Pf-JvrwkO03KmXkcHxd8CHg8_Q8vGyzR46Npz1JY0o"  # shared Release Log
MODULE = "Unity plugin"
TIMEZONE = ZoneInfo("Europe/Zurich")


def read_version() -> str:
    match = re.search(r'public const string VERSION = "(\d+\.\d+\.\d+)"', SETTINGS.read_text())
    if not match:
        sys.exit("Settings.VERSION is missing or not MAJOR.MINOR.PATCH.")
    return match.group(1)


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
    return re.sub(r"^## \[Unreleased\]", f"## [Unreleased]\n\n### Added\n\n## [{version}] - {date}", changelog,
                  count=1, flags=re.MULTILINE)


def summary(notes: str, version: str) -> str:
    """The intro paragraph of the notes, or a generic line when they start with a list or heading."""
    first = notes.split("\n\n", 1)[0]
    if not first or first.startswith(("#", "-", "*")):
        return f"Releasing v{version}"
    return " ".join(first.split())


def sheet_values(version: str, notes: str, now: datetime, owner: str) -> list[str]:
    """The Release Log row: date, module, tag, owner, summary, and a link formula to the GitHub release."""
    tag = f"v{version}"
    link = f'=HYPERLINK("{REPO_URL}/releases/tag/{tag}","{tag}")'
    # ponytail: owner is a first name, not snapped to the sheet's Owner dropdown.
    return [now.strftime("%d.%m"), MODULE, tag, owner.split(" ")[0], summary(notes, version), link]


def last_commit_author() -> str:
    return git("log", "-1", "--format=%an")


def git(*args: str) -> str:
    return subprocess.run(["git", *args], cwd=ROOT, check=True, stdout=subprocess.PIPE, text=True).stdout.strip()


def sheets(token: str, path: str, body: dict | None = None) -> dict:
    request = urllib.request.Request(
        f"https://sheets.googleapis.com/v4/spreadsheets/{SHEET_ID}{path}",
        data=json.dumps(body).encode() if body is not None else None,
        headers={"Authorization": f"Bearer {token}", "Content-Type": "application/json"},
    )
    with urllib.request.urlopen(request) as response:
        return json.load(response)


def insert_sheet_row(token: str, year: str, row: list[dict]) -> None:
    """Insert the row below the header of the year tab, as AirConsole/claude-plugins release-log.yml does."""
    tabs = sheets(token, "?fields=sheets(properties(sheetId,title))")["sheets"]
    tab_id = next((t["properties"]["sheetId"] for t in tabs if t["properties"]["title"] == year), None)
    if tab_id is None:
        sys.exit(f"Release Log sheet has no '{year}' tab. Create it, then add the row by hand.")
    sheets(token, ":batchUpdate", {"requests": [
        {"insertDimension": {"range": {"sheetId": tab_id, "dimension": "ROWS", "startIndex": 1, "endIndex": 2},
                             "inheritFromBefore": False}},
        {"updateCells": {"rows": [{"values": row}], "fields": "userEnteredValue",
                         "start": {"sheetId": tab_id, "rowIndex": 1, "columnIndex": 0}}},
    ]})


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


def check_unreleased() -> None:
    """Exit before the Unity build when a release or an open release PR already uses this version."""
    version = read_version()
    tag = f"v{version}"
    if tag_on_origin(tag):
        sys.exit(f"{tag} is already tagged on origin. Bump Settings.VERSION.")
    if git("ls-remote", "--heads", "origin", f"refs/heads/{release_branch(tag)}"):
        sys.exit(f"{release_branch(tag)} is already on origin. Open or merge its PR, or close it and delete the branch.")
    # --open-pr must not date a second [VERSION] section (for example after an unreverted failed release merge),
    # and entries added since then under Unreleased would miss the notes.
    changelog = CHANGELOG.read_text()
    if f"## [{version}]" in changelog:
        sys.exit(f"CHANGELOG.md already has a [{version}] section. Move its notes under '## [Unreleased]', "
                 "or revert the failed release merge.")
    if not re.search(r"^## \[Unreleased\]", changelog, re.MULTILINE):
        sys.exit("CHANGELOG.md has no '## [Unreleased]' section to release.")


def release_branch(tag: str) -> str:
    return f"release/{tag}"


def package_path(tag: str) -> Path:
    return ROOT / "Builds" / f"airconsole-unity-plugin-{tag}.unitypackage"


def open_release_pr(dry_run: bool) -> None:
    """Date the CHANGELOG, commit it with the exported package to release/v{VERSION} and open the release PR."""
    check_unreleased()
    version = read_version()
    tag = f"v{version}"
    branch = release_branch(tag)
    now = datetime.now(TIMEZONE)
    changelog = stamp_changelog(CHANGELOG.read_text(), version, now.strftime("%Y-%m-%d"))
    notes = release_notes(changelog, version)
    body = (f"{notes}\n\n---\n"
            f"Merging this PR creates the {tag} tag, the GitHub release and the Release Log sheet row.\n"
            f"If master changes, do not update this branch: close this PR, delete {branch}, and run Create Release again.")
    if os.environ.get("GITHUB_ACTIONS") == "true":
        body += ("\nThis PR was opened with the workflow token, so the required checks do not start by themselves: "
                 "close and reopen it to run them.")
    base = git("rev-parse", "HEAD")
    package = package_path(tag).relative_to(ROOT)
    missing = "" if package_path(tag).is_file() else " (missing: the Unity export creates it before this step)"
    print(f"Release:  {tag} from {base}\nPackage:  {package}{missing}")
    print(f"Sheet:    tab {now.year}: " + " | ".join(sheet_values(version, notes, now, last_commit_author())))
    print("          The owner is whoever merges the release PR. This preview uses the last commit author.")
    print(f"PR body:\n{body}\n")

    run(["git", "switch", "-C", branch], dry_run)
    if dry_run:
        print(f"would date '## [Unreleased]' in CHANGELOG.md as '## [{version}] - {now:%Y-%m-%d}'")
    else:
        CHANGELOG.write_text(changelog)
    # The glob also stages the deletion of the previous version's package, and no other file in Builds/.
    run(["git", "add", "-A", "--", "Builds/airconsole-unity-plugin-v*.unitypackage", "CHANGELOG.md"], dry_run)
    # The publish job checks this line against the commit the release PR merges onto.
    run(["git", "commit", "-m", f"Release {tag}", "-m", f"Exported from {base}"], dry_run)
    run(["git", "push", "origin", branch], dry_run)
    run(["gh", "pr", "create", "--base", "master", "--head", branch, "--title", f"Release {tag}",
         "--body-file", write_temp(body)], dry_run)


def publish(dry_run: bool) -> None:
    """Create the tag and GitHub release for HEAD and add the Release Log row."""
    version = read_version()
    tag = f"v{version}"
    branch = release_branch(tag)
    # CI passes the head branch of the merged PR; any other release/v* branch must not publish this version.
    if os.environ.get("RELEASE_BRANCH", branch) != branch:
        sys.exit(f"{os.environ['RELEASE_BRANCH']} is not {branch}. Nothing to release.")
    if tag_on_origin(tag):
        sys.exit(f"{tag} is already tagged on origin. If the GitHub release or the Release Log row is missing, "
                 "add it by hand.")

    package = package_path(tag)
    missing = ""
    if not package.is_file():
        if not dry_run:
            sys.exit(f"{package.relative_to(ROOT)} is missing. Run the Create Release workflow to export it.")
        missing = " (missing: the Create Release export creates it)"

    now = datetime.now(TIMEZONE)
    changelog = CHANGELOG.read_text()
    # A dry run before the release PR (on master) previews the section as --open-pr will date it.
    if dry_run and f"## [{version}]" not in changelog:
        print("CHANGELOG.md is not dated yet. This preview dates '## [Unreleased]' as the release PR will.")
        changelog = stamp_changelog(changelog, version, now.strftime("%Y-%m-%d"))
    notes = release_notes(changelog, version)
    # CI passes the name of whoever merged the release PR; a squash merge commit may be authored by the bot.
    values = sheet_values(version, notes, now, os.environ.get("RELEASE_OWNER") or last_commit_author())
    row = [{"userEnteredValue": {"stringValue": value}} for value in values[:-1]]
    row.append({"userEnteredValue": {"formulaValue": values[-1]}})
    release_url = f"{REPO_URL}/releases/tag/{tag}"

    print(f"Release:  {tag} at {git('rev-parse', 'HEAD')}\nPackage:  {package.relative_to(ROOT)}{missing}")
    print(f"Sheet:    tab {now.year}: " + " | ".join(values))
    if not os.environ.get("RELEASE_OWNER"):
        print("          The owner is whoever merges the release PR. This preview uses the last commit author.")
    print(f"Notes:\n{notes}\n")

    token = os.environ.get("GOOGLE_ACCESS_TOKEN")
    if not dry_run and not token:
        sys.exit("GOOGLE_ACCESS_TOKEN is not set. It is needed to write the Release Log row.")
    # `gh release create` also creates and pushes the tag on --target.
    run(["gh", "release", "create", tag, str(package.relative_to(ROOT)), "--title", f"Release {version}",
         "--notes-file", write_temp(notes), "--target", git("rev-parse", "HEAD")], dry_run)
    if dry_run:
        print(f"would add the Release Log row above to tab {now.year}")
        return
    insert_sheet_row(token, str(now.year), row)
    print(f"Released {release_url} and logged it in the Release Log sheet.")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--dry-run", action="store_true", help="validate and print the commands, change nothing")
    parser.add_argument("--open-pr", action="store_true", help="open the release PR instead of publishing")
    parser.add_argument("--check", action="store_true", help="only check that this version is not released yet")
    args = parser.parse_args()
    if args.check:
        check_unreleased()
    elif args.open_pr:
        open_release_pr(args.dry_run)
    else:
        publish(args.dry_run)


if __name__ == "__main__":
    main()
