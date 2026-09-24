#!/usr/bin/env python3
"""Publish the plugin release for the version in Settings.cs.

Creates the GitHub release v{VERSION} with the committed .unitypackage and the
CHANGELOG.md section as notes, then inserts a row at the top of the year tab of
the Release Log sheet.

Usage:
  scripts/release.py --dry-run   # validate and print everything, change nothing
  scripts/release.py             # needs `gh` auth and GOOGLE_ACCESS_TOKEN (CI runs this when the release PR merges)
"""
import argparse
import json
import os
import re
import subprocess
import sys
import urllib.request
from datetime import datetime
from pathlib import Path
from zoneinfo import ZoneInfo

ROOT = Path(__file__).resolve().parent.parent
SETTINGS = ROOT / "Assets/AirConsole/scripts/Runtime/Settings.cs"
CHANGELOG = ROOT / "CHANGELOG.md"
REPO_URL = "https://github.com/AirConsole/airconsole-unity-plugin"
SHEET_ID = "17Pf-JvrwkO03KmXkcHxd8CHg8_Q8vGyzR46Npz1JY0o"  # shared Release Log
MODULE = "Unity plugin"
TIMEZONE = ZoneInfo("Europe/Zurich")


def read_version(settings: str) -> str:
    match = re.search(r'public const string VERSION = "(\d+\.\d+\.\d+)"', settings)
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
                 "Rename '## [Unreleased]' (Tools > AirConsole > Package Plugin does this).")
    return match.group(1).strip()


def summary(notes: str, version: str) -> str:
    """The intro paragraph of the notes, or a generic line when they start with a list or heading."""
    first = notes.split("\n\n", 1)[0]
    if not first or first.startswith(("#", "-", "*")):
        return f"Releasing v{version}"
    return " ".join(first.split())


def git(*args: str) -> str:
    return subprocess.run(["git", *args], cwd=ROOT, check=True, capture_output=True, text=True).stdout.strip()


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


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--dry-run", action="store_true", help="validate and print, create nothing")
    parser.add_argument("--notes-only", action="store_true", help="print only the release notes")
    args = parser.parse_args()
    dry_run = args.dry_run

    version = read_version(SETTINGS.read_text())
    tag = f"v{version}"
    if args.notes_only:
        print(release_notes(CHANGELOG.read_text(), version))
        return
    if git("ls-remote", "--tags", "origin", f"refs/tags/{tag}"):
        print(f"{tag} is already tagged on origin. Nothing to release.")
        return

    package = ROOT / "Builds" / f"airconsole-unity-plugin-{tag}.unitypackage"
    if not package.is_file():
        sys.exit(f"{package.relative_to(ROOT)} is missing. Run Tools > AirConsole > Package Plugin and commit it.")

    notes = release_notes(CHANGELOG.read_text(), version)
    now = datetime.now(TIMEZONE)
    # CI passes the name of whoever merged the release PR; a squash merge commit may be authored by the bot.
    owner = (os.environ.get("RELEASE_OWNER") or git("log", "-1", "--format=%an")).split(" ")[0]
    release_url = f"{REPO_URL}/releases/tag/{tag}"
    # ponytail: owner is the release commit author's first name, not snapped to the sheet's Owner dropdown.
    row = [{"userEnteredValue": {"stringValue": value}}
           for value in (now.strftime("%d.%m"), MODULE, tag, owner, summary(notes, version))]
    row.append({"userEnteredValue": {"formulaValue": f'=HYPERLINK("{release_url}","{tag}")'}})

    print(f"Release:  {tag} at {git('rev-parse', 'HEAD')}\nPackage:  {package.relative_to(ROOT)}")
    print(f"Sheet:    tab {now.year}: " + " | ".join(next(iter(c['userEnteredValue'].values())) for c in row))
    print(f"Notes:\n{notes}")
    if dry_run:
        print("\nDry run: no tag, release or sheet row created.")
        return

    token = os.environ.get("GOOGLE_ACCESS_TOKEN")
    if not token:
        sys.exit("GOOGLE_ACCESS_TOKEN is not set. It is needed to write the Release Log row.")
    subprocess.run(["gh", "release", "create", tag, str(package), "--title", f"Release {version}",
                    "--notes", notes, "--target", git("rev-parse", "HEAD")], cwd=ROOT, check=True)
    insert_sheet_row(token, str(now.year), row)
    print(f"Released {release_url} and logged it in the Release Log sheet.")


if __name__ == "__main__":
    main()
