#!/usr/bin/env bash
# Installs and launches each APK in a folder on the connected Android device,
# then asks for a manual Pass/Fail verdict and appends it to <dir>/results.log.
# Usage: test-apks.sh [apk-dir]   (default: current directory)
set -euo pipefail

PKG=com.airconsole.plugin.unity
dir=${1:-.}
log="$dir/results.log"
apks=("$dir"/*.apk)
[[ -e ${apks[0]} ]] || { echo "No APKs in $dir" >&2; exit 1; }
total=${#apks[@]}
pass=0 fail=0

progress() {
  local done=$((pass + fail))
  printf '\n[%s%s] %d/%d done  pass %d  fail %d\n' \
    "$(printf '%*s' "$done" '' | tr ' ' '#')" \
    "$(printf '%*s' "$((total - done))" '' | tr ' ' '.')" \
    "$done" "$total" "$pass" "$fail"
}

for apk in "${apks[@]}"; do
  name=$(basename "$apk")
  if [[ $name == *gameactivity* ]]; then
    type=GameActivity activity=UnityPlayerGameActivity
  else
    type=Activity activity=UnityPlayerActivity
  fi
  version=$(grep -oE '[0-9]{4}\.[0-9]+' <<<"$name" || echo unknown)

  progress
  echo "$name"
  echo "  activity: $type   unity: $version"

  # adb must not read stdin, or it swallows the P/F keypress.
  adb uninstall "$PKG" </dev/null >/dev/null 2>&1 || true
  if adb install "$apk" </dev/null; then
    adb shell am start -n "$PKG/com.unity3d.player.$activity" \
      --ez webview_debuggable true --ez development_logging true </dev/null
    key=
    until [[ $key == [pPfF] ]]; do read -rsn1 -p "  [P]ass / [F]ail? " key; echo; done
  else
    echo "  install failed"
    key=F
  fi

  if [[ $key == [pP] ]]; then result=PASS pass=$((pass + 1)); else result=FAIL fail=$((fail + 1)); fi
  printf '%s  %s  %s  (%s, %s)\n' "$(date '+%F %T')" "$result" "$name" "$type" "$version" >>"$log"
done

progress
echo "Results appended to $log"
