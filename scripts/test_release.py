"""Self-check for release.py changelog parsing. Run: python3 scripts/test_release.py"""
import os

from release import publish, read_version, release_notes, stamp_changelog

CHANGELOG = """# Releases

## [Unreleased]

## [2.7.0] - 2026-10-01

Intro for 2.7.0
over two lines.

### Fixed

- A fix

## [2.6.2] - 2026-09-24

### Fixed

- Older fix
"""

notes = release_notes(CHANGELOG, "2.7.0")
assert notes.startswith("Intro for 2.7.0") and notes.endswith("- A fix"), notes
assert "Older fix" not in notes

try:
    release_notes(CHANGELOG.replace("## [2.7.0] - 2026-10-01", "## [2.7.0]"), "2.7.0")
    raise AssertionError("an undated section must be rejected")
except SystemExit:
    pass
assert read_version()

# --open-pr dates the Unreleased section and opens a new, empty one above it.
stamped = stamp_changelog(CHANGELOG.replace("## [Unreleased]\n\n## [2.7.0] - 2026-10-01\n\n", "## [Unreleased]\n\n"),
                          "2.7.0", "2026-10-02")
assert stamped.startswith("# Releases\n\n## [Unreleased]\n\n### Added\n\n## [2.7.0] - 2026-10-02\n\nIntro"), stamped
assert release_notes(stamped, "2.7.0") == notes
assert release_notes(stamped, "2.6.2") == "### Fixed\n\n- Older fix"

# Only release/v{Settings.VERSION} may publish; this check runs before any git or network call.
os.environ["RELEASE_BRANCH"] = "release/v0-docs"
try:
    publish(dry_run=True)
    raise AssertionError("another release/v* branch must not publish")
except SystemExit as error:
    assert "is not release/v" in str(error.code), error.code
print("ok")
