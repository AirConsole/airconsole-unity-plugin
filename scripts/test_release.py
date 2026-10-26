"""Self-check for release.py version bump and changelog parsing. Run: python3 scripts/test_release.py"""
import os
import sys
import tempfile
from pathlib import Path

import release
from release import next_version, publish, read_version, release_notes, stamp_changelog, write_version

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

assert next_version("2.6.3", "patch") == "2.6.4"
assert next_version("2.6.3", "minor") == "2.7.0"
assert next_version("2.6.3", "major") == "3.0.0"
assert next_version("2.9.9", "patch") == "2.9.10"

# The version tests change a copy of Settings.cs, so a failed or killed run leaves the real file as it is.
settings_path = release.SETTINGS
settings_directory = tempfile.TemporaryDirectory()
release.SETTINGS = Path(settings_directory.name) / "Settings.cs"
settings = settings_path.read_text()
release.SETTINGS.write_text(settings)
version = read_version()

# write_version changes only the VERSION value in Settings.cs.
try:
    write_version("98.76.54")
    assert read_version() == "98.76.54"
    expected = settings.replace(f'VERSION = "{version}"', 'VERSION = "98.76.54"')
    assert release.SETTINGS.read_text() == expected
finally:
    release.SETTINGS.write_text(settings)

# A bump starts only from a released version, so no release is skipped.
tag_on_origin, publish_release, git = release.tag_on_origin, release.publish, release.git


def unexpected_publish(dry_run):
    raise AssertionError("--bump alone must not publish")


release.publish = unexpected_publish
try:
    release.tag_on_origin = lambda tag: False
    try:
        release.next_release("patch")
        raise AssertionError("a bump from an untagged version must be rejected")
    except SystemExit as error:
        assert "is not tagged on origin" in str(error.code), error.code
    release.tag_on_origin = lambda tag: tag == f"v{version}"
    assert release.next_release("minor") == next_version(version, "minor")

    sys.argv = ["release.py", "--bump", "patch", "--dry-run"]
    release.main()
    assert release.SETTINGS.read_text() == settings, "--bump --dry-run must not write Settings.cs"
    sys.argv = ["release.py", "--bump", "patch", "--open-pr"]
    try:
        release.main()
        raise AssertionError("--bump --open-pr without --dry-run must be rejected before the write")
    except SystemExit:
        assert release.SETTINGS.read_text() == settings
    sys.argv = ["release.py", "--bump", "patch"]
    release.main()
    assert read_version() == next_version(version, "patch")
finally:
    release.SETTINGS, release.tag_on_origin, release.publish = settings_path, tag_on_origin, publish_release
    settings_directory.cleanup()
assert settings_path.read_text() == settings, "the version tests must not change the real Settings.cs"

# check_unreleased rejects an Unreleased section without notes and any open release branch.
changelog_path = release.CHANGELOG
release.tag_on_origin = lambda tag: False
try:
    with tempfile.TemporaryDirectory() as directory:
        release.CHANGELOG = Path(directory) / "CHANGELOG.md"
        for unreleased in ("", "### Added\n\n"):
            release.CHANGELOG.write_text(f"# Releases\n\n## [Unreleased]\n\n{unreleased}"
                                         "## [0.0.1] - 2026-10-03\n\n- A fix\n")
            try:
                release.check_unreleased("0.0.2")
                raise AssertionError(f"an Unreleased section of {unreleased!r} must be rejected")
            except SystemExit as error:
                assert "has no notes" in str(error.code), error.code

        release.CHANGELOG.write_text("# Releases\n\n## [Unreleased]  \n\n- A fix")
        release.git = lambda *args: "abc\trefs/heads/release/v0.1.0"
        try:
            release.check_unreleased("0.0.2")
            raise AssertionError("an open release branch of another version must block the release")
        except SystemExit as error:
            assert "release/v0.1.0 is already on origin" in str(error.code), error.code
finally:
    release.CHANGELOG, release.tag_on_origin, release.git = changelog_path, tag_on_origin, git

# --open-pr dates the Unreleased section and opens a new, empty one above it.
stamped = stamp_changelog(CHANGELOG.replace("## [Unreleased]\n\n## [2.7.0] - 2026-10-01\n\n", "## [Unreleased]\n\n"),
                          "2.7.0", "2026-10-02")
assert stamped.startswith("# Releases\n\n## [Unreleased]\n\n## [2.7.0] - 2026-10-02\n\nIntro"), stamped
assert release_notes(stamped, "2.7.0") == notes
assert release_notes(stamped, "2.6.2") == "### Fixed\n\n- Older fix"

# Only release/v{Settings.VERSION} may publish; this check runs before any git or network call.
os.environ["RELEASE_BRANCH"] = "release/v0-docs"
try:
    publish(dry_run=True)
    raise AssertionError("another release/v* branch must not publish")
except SystemExit as error:
    assert "is not release/v" in str(error.code), error.code

# --open-pr must not commit a release without the exported package; this check runs before any git or network call.
def unexpected_origin_check(version):
    raise AssertionError("the package check must run before the origin check")


release.check_unreleased = unexpected_origin_check
release.package_path = lambda tag: release.ROOT / "Builds" / "missing.unitypackage"
try:
    release.open_release_pr(dry_run=False, version="9.9.9")
    raise AssertionError("a release PR without the package must be rejected")
except SystemExit as error:
    assert "missing.unitypackage is missing" in str(error.code), error.code
print("ok")
