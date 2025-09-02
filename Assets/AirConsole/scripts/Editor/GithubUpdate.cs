namespace NDream.AirConsole.Editor {
    using System;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;
    using UnityEngine.Networking;

    internal abstract class GithubUpdate {
        private const string GithubLatestAPI = "https://api.github.com/repos/airconsole/airconsole-unity-plugin/releases/latest";

        private static bool _updateCheckStarted;
        private static bool _updateCheckInProgress;
        private static bool _updateAvailable;
        private static Version _latestVersion;
        private static GithubRelease _cachedLatestRelease;
        private static UnityWebRequest _checkRequest;
        private static UnityWebRequestAsyncOperation _checkOp;

        internal static bool UpdateCheckInProgress => _updateCheckInProgress;
        internal static bool UpdateAvailable => _updateAvailable;
        internal static Version LatestVersion => _latestVersion;

        internal static void BeginBackgroundUpdateCheck() {
            if (_updateCheckStarted || _updateCheckInProgress)
                return;
            _updateCheckStarted = true;
            _updateCheckInProgress = true;
            try {
                _checkRequest = UnityWebRequest.Get(GithubLatestAPI);
                _checkRequest.SetRequestHeader("User-Agent", "AirConsole-Unity-Updater");
                _checkOp = _checkRequest.SendWebRequest();
                EditorApplication.update += PollUpdateCheck;
            } catch (Exception e) {
                _updateCheckInProgress = false;
                AirConsoleLogger.LogError(() => $"Failed to start update check: {e.Message}");
            }
        }

        private static void PollUpdateCheck() {
            if (_checkOp == null)
                return;
#if UNITY_2020_2_OR_NEWER
            bool done = _checkOp.isDone && _checkRequest.result != UnityWebRequest.Result.InProgress;
            bool success = _checkRequest.result == UnityWebRequest.Result.Success;
#else
            bool done = s_CheckOp.isDone;
            bool success = !s_CheckRequest.isNetworkError && !s_CheckRequest.isHttpError;
#endif
            if (!done) return;

            try {
                if (success) {
                    var json = _checkRequest.downloadHandler.text;
                    var release = JsonUtility.FromJson<GithubRelease>(json);
                    _cachedLatestRelease = release;
                    var latestTag = (release?.tag_name ?? release?.name ?? "").Trim();
                    var latestVersionStr = latestTag.StartsWith("v", StringComparison.OrdinalIgnoreCase)
                        ? latestTag.Substring(1)
                        : latestTag;
                    if (Version.TryParse(Settings.VERSION, out var current) && Version.TryParse(latestVersionStr, out var latest)) {
                        _latestVersion = latest;
                        _updateAvailable = latest > current;
                    } else {
                        _updateAvailable = false;
                    }
                } else {
                    AirConsoleLogger.LogWarning(() => $"Update check failed: {_checkRequest.responseCode} {_checkRequest.error}");
                    _updateAvailable = false;
                }
            } catch (Exception ex) {
                AirConsoleLogger.LogError(() => $"Update check error: {ex.Message}");
                _updateAvailable = false;
            } finally {
                _updateCheckInProgress = false;
                _checkRequest.Dispose();
                _checkRequest = null;
                _checkOp = null;
                EditorApplication.update -= PollUpdateCheck;
                InternalEditorUtility.RepaintAllViews();
            }
        }

        [Serializable]
        private class GithubAsset {
            public string name;
            public string browser_download_url;
        }

        [Serializable]
        private class GithubRelease {
            public string tag_name;
            public string name;
            public GithubAsset[] assets;
            public string html_url;
        }

        internal static void TryUpdatePluginFromGithub() {
            try {
                EditorUtility.DisplayProgressBar("AirConsole", "Checking latest release…", 0.1f);
                GithubRelease release = _cachedLatestRelease;
                if (release == null) {
                    string json = DownloadString(GithubLatestAPI, out long _);
                    if (string.IsNullOrEmpty(json)) {
                        EditorUtility.ClearProgressBar();
                        EditorUtility.DisplayDialog("AirConsole", "Failed to retrieve release info.", "OK");
                        return;
                    }

                    release = JsonUtility.FromJson<GithubRelease>(json);
                }

                if (release == null) {
                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog("AirConsole", "Could not parse release info.", "OK");
                    return;
                }

                string latestTag = (release.tag_name ?? release.name ?? "").Trim();
                string latestVersionStr = latestTag.StartsWith("v", StringComparison.OrdinalIgnoreCase)
                    ? latestTag.Substring(1)
                    : latestTag;

                Version current, latest;
                bool currentOk = Version.TryParse(Settings.VERSION, out current);
                bool latestOk = Version.TryParse(latestVersionStr, out latest);

                if (currentOk && latestOk && latest <= current) {
                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog("AirConsole", $"Plugin is up to date (v{Settings.VERSION}).", "OK");
                    return;
                }

                string msg = latestOk
                    ? $"Update available: v{Settings.VERSION} → v{latest}"
                    : $"A newer release may be available: '{latestTag}'.\nCurrent: v{Settings.VERSION}";
                bool doUpdate = EditorUtility.DisplayDialog("AirConsole", msg + "\n\nDownload and import the latest release?", "Update",
                    "Cancel");
                if (!doUpdate) {
                    EditorUtility.ClearProgressBar();
                    return;
                }

                GithubAsset pkg = null;
                if (release.assets != null && release.assets.Length > 0) {
                    pkg = release.assets.FirstOrDefault(a =>
                              a != null
                              && !string.IsNullOrEmpty(a.name)
                              && a.name.EndsWith(".unitypackage", StringComparison.OrdinalIgnoreCase))
                          ?? release.assets.FirstOrDefault(a =>
                              a != null && !string.IsNullOrEmpty(a.name) && a.name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
                }

                if (pkg == null || string.IsNullOrEmpty(pkg.browser_download_url)) {
                    EditorUtility.ClearProgressBar();
                    if (!string.IsNullOrEmpty(release.html_url)) {
                        bool open = EditorUtility.DisplayDialog("AirConsole", "Could not find a .unitypackage asset in the latest release.",
                            "Open Releases", "Close");
                        if (open) Application.OpenURL(release.html_url);
                    } else {
                        EditorUtility.DisplayDialog("AirConsole", "Could not find downloadable assets for the latest release.", "OK");
                    }

                    return;
                }

                string tempPath = Path.Combine(Path.GetTempPath(),
                    pkg.name ?? ("airconsole-plugin-" + Guid.NewGuid().ToString("N") + ".unitypackage"));
                EditorUtility.DisplayProgressBar("AirConsole", "Downloading package…", 0.4f);
                long contentLength;
                byte[] data = DownloadBytes(pkg.browser_download_url, out contentLength);
                if (data == null || data.Length == 0) {
                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog("AirConsole", "Failed to download the package.", "OK");
                    return;
                }

                File.WriteAllBytes(tempPath, data);
                EditorUtility.DisplayProgressBar("AirConsole", "Importing package…", 0.9f);

                if (tempPath.EndsWith(".unitypackage", StringComparison.OrdinalIgnoreCase)) {
                    AssetDatabase.ImportPackage(tempPath, true);
                } else {
                    bool open = EditorUtility.DisplayDialog("AirConsole",
                        "Downloaded asset is not a .unitypackage. Open in browser instead?", "Open", "Cancel");
                    if (open) Application.OpenURL(pkg.browser_download_url);
                }

                EditorUtility.ClearProgressBar();
            } catch (Exception ex) {
                EditorUtility.ClearProgressBar();
                AirConsoleLogger.LogError(() => $"Update failed: {ex.Message}");
                EditorUtility.DisplayDialog("AirConsole", "Update failed. See console for details.", "OK");
            }
        }

        private static string DownloadString(string url, out long contentLength) {
            contentLength = -1;
            using (UnityWebRequest req = UnityWebRequest.Get(url)) {
                req.SetRequestHeader("User-Agent", "AirConsole-Unity-Updater");
                var op = req.SendWebRequest();
                while (!op.isDone) {
                    EditorUtility.DisplayProgressBar("AirConsole", "Contacting GitHub…", req.downloadProgress);
                }
#if UNITY_2020_2_OR_NEWER
                if (req.result != UnityWebRequest.Result.Success)
#else
                if (req.isNetworkError || req.isHttpError)
#endif
                {
                    AirConsoleLogger.LogError(() => $"HTTP error: {req.responseCode} {req.error}");
                    return null;
                }

                contentLength = req.GetResponseHeaders() != null && req.GetResponseHeaders().ContainsKey("CONTENT-LENGTH")
                    ? long.Parse(req.GetResponseHeader("CONTENT-LENGTH"))
                    : -1;
                return req.downloadHandler.text;
            }
        }

        private static byte[] DownloadBytes(string url, out long contentLength) {
            contentLength = -1;
            using (UnityWebRequest req = UnityWebRequest.Get(url)) {
                req.SetRequestHeader("User-Agent", "AirConsole-Unity-Updater");
                var op = req.SendWebRequest();
                while (!op.isDone) {
                    EditorUtility.DisplayProgressBar("AirConsole", "Downloading…", req.downloadProgress);
                }
#if UNITY_2020_2_OR_NEWER
                if (req.result != UnityWebRequest.Result.Success)
#else
                if (req.isNetworkError || req.isHttpError)
#endif
                {
                    AirConsoleLogger.LogError(() => $"HTTP error: {req.responseCode} {req.error}");
                    return null;
                }

                contentLength = req.GetResponseHeaders() != null && req.GetResponseHeaders().ContainsKey("CONTENT-LENGTH")
                    ? long.Parse(req.GetResponseHeader("CONTENT-LENGTH"))
                    : -1;
                return req.downloadHandler.data;
            }
        }
    }
}
