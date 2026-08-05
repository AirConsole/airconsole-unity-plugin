#!/usr/bin/env bash
#
# check-proprietary-source.sh
#
# Fail-closed guard that keeps AirConsole proprietary source out of this
# PUBLIC (open-source) repository.
#
# The native Android library (airconsole-unity-android-plugin) and the
# native webview build inputs (airconsole-unity-webview) are maintained in
# separate PRIVATE repositories. Only their COMPILED artifacts are allowed
# to ship here:
#
#   - unityandroidlibrary.aar        (Android native, compiled)
#   - WebViewPlugin-*.aar.tmpl        (webview native, compiled)
#   - *.unitypackage / *.bundle       (compiled)
#
# ONE EXCEPTION, already in the tree: Assets/AirConsole/unity-webview/Plugins/
# iOS/*.mm is third-party upstream (GREE / Keijiro Takahashi, zlib licence) and
# is public by licence, not by mistake. That is why *.mm is not on the denylist.
# AirConsole-authored native code must NEVER be added as *.mm.
#
# Source languages (*.java, *.kt), the Gradle build system, and the private
# build pipeline (shell/Rake scripts, version catalogs) reveal the internal
# technology and MUST NOT be committed here. A leak is effectively
# irreversible once pushed (git history, forks, indexers), so this check
# blocks by default and only whitelists Unity's own boilerplate + the
# content-free files listed below.
#
# Each source pattern also matches a trailing backup/merge/rename suffix
# (e.g. Foo.java.orig, build.gradle.bak, Plugin.kt~, Foo.java.txt) because
# git merge/rebase (.orig/.rej) and editors (~/.bak) create sibling files
# that hold the full verbatim source.
#
# KNOWN LIMITATIONS (a path denylist cannot see file CONTENT):
#   - Proprietary source hand-pasted into an accepted file (.md/.cs) is not
#     detected. Deliberately renaming source to an accepted extension is not
#     detected. Both are outside the ACCIDENTAL-leak threat model this guard
#     targets (git add -A after copying a native dir keeps real extensions,
#     which ARE blocked). If the native stack ever gains C/C++/NDK sources
#     (*.c/*.cpp/*.h/*.aidl) or loose native libs, add them below.
#
# Usage:
#   check-proprietary-source.sh [file ...]
#     - with file arguments: checks exactly those paths (pre-commit hook)
#     - with no arguments:   checks every tracked file (CI full-tree scan)
#
# Exit status: 0 = clean, 1 = proprietary source found.
#
# POSIX bash 3.2 compatible (stock macOS) — no mapfile / associative arrays.

set -euo pipefail

# Suffix that may trail a source extension: end-of-path, or a '.' / '~' that
# begins a backup/merge/rename tail (.orig .bak .rej .txt .BACKUP.N, ~).
S='($|[.~])'

# --- Denylist (parallel arrays; matched case-insensitively on the path) ------
DENY_PATTERN=(
  "\.(java|kt|kts)$S"
  "\.gradle$S"
  "\.pro$S"
  "\.sh$S"
  "\.toml$S"
  "\.rake$S"
  "(^|/)gradlew(\.bat)?$S"
  "(^|/)gradle/wrapper/"
  "(^|/)gradle\.properties$S"
  "(^|/)Rakefile$S"
  "(^|/)\.gitmodules$S"
  "(^|/)AndroidManifest\.xml$S"
  "(^|/)src/main/res/"
  "\.iml$S"
  "(^|/)\.idea/(modules|misc|gradle)\.xml$S"
)
DENY_REASON=(
  'native source (Java/Kotlin) — ships as a compiled .aar, never as source'
  'Gradle build script — reveals the private build setup'
  'ProGuard rules — reveal internal class structure'
  'shell build/install script — reveals the private build pipeline'
  'version catalog (.toml) — reveals the private dependency stack'
  'Rake build task — reveals the private build pipeline'
  'Gradle wrapper'
  'Gradle wrapper'
  'Gradle properties'
  'Ruby packaging build script — reveals the private build pipeline'
  'submodule reference — would disclose a private repository URL'
  'native Android manifest — declares internal components/permissions'
  'native Android resources — belong to the private library'
  'IntelliJ/Android Studio module file — reveals the private dependency stack'
  'IntelliJ/Android Studio project config — reveals the private module layout'
)

# --- Allowlist: the ONLY exceptions to the denylist above --------------------
# 1. Unity's own Android build templates (public boilerplate).
# 2. This guard script itself (the repo's own tooling).
# 3. *.meta — Unity import sidecars are content-free (GUID + import settings);
#    they never contain source, so a Foo.java.meta / mainTemplate.gradle.meta
#    is safe even though its base name trips a deny pattern.
ALLOW_PATTERN='(^|/)(mainTemplate|launcherTemplate|baseProjectTemplate|settingsTemplate)\.gradle$|(^|/)gradleTemplate\.properties$|(^|/)scripts/check-proprietary-source\.sh$|\.meta$'

# --- Combined deny regex for one fast first-pass grep ------------------------
combined=""
for p in "${DENY_PATTERN[@]}"; do
  if [ -n "$combined" ]; then
    combined="$combined|($p)"
  else
    combined="($p)"
  fi
done

# --- Emit the list of files to check -----------------------------------------
list_files() {
  if [ "$#" -gt 0 ]; then
    printf '%s\n' "$@"
  else
    # No args → scan the whole tracked tree (CI standing invariant).
    git ls-files
  fi
}

# --- reason_for <path>: echo the denylist reason that matched ----------------
reason_for() {
  local path="$1" i
  for i in "${!DENY_PATTERN[@]}"; do
    if printf '%s' "$path" | grep -Eiq -- "${DENY_PATTERN[$i]}"; then
      printf '%s' "${DENY_REASON[$i]}"
      return
    fi
  done
  printf '%s' 'proprietary source'
}

violations=0
while IFS= read -r path; do
  [ -n "$path" ] || continue
  # Skip explicitly allowlisted files.
  if printf '%s' "$path" | grep -Eiq -- "$ALLOW_PATTERN"; then
    continue
  fi
  if [ "$violations" -eq 0 ]; then
    echo "ERROR: proprietary source must not be committed to this public repository." >&2
    echo >&2
  fi
  printf '  \xe2\x9c\x97 %s\n      -> %s\n' "$path" "$(reason_for "$path")" >&2
  violations=$((violations + 1))
done < <(list_files "$@" | grep -Ei -- "$combined" || true)

if [ "$violations" -gt 0 ]; then
  echo >&2
  echo "Found $violations blocked file(s)." >&2
  echo "Native code lives in the private repos and ships here only as compiled" >&2
  echo "artifacts (.aar / .unitypackage / .bundle). Remove the source file(s) above." >&2
  echo "If a file is genuinely Unity boilerplate, add it to the ALLOW list in" >&2
  echo "scripts/check-proprietary-source.sh with justification." >&2
  exit 1
fi

exit 0
