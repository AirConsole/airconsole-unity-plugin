#if !DISABLE_AIRCONSOLE
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using Formatting = Newtonsoft.Json.Formatting;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace NDream.AirConsole.Editor {
    public abstract class ProjectDependencyValidator : IPreprocessBuildWithReport {
        private static bool AndroidBuildNotAllowed;
        private static ListRequest Request;
        private static readonly List<string> packages = new();
        private static readonly List<string> packagesFound = new();

        /// <summary>
        /// Dictionary containing the rejection reasons for each package.
        /// Must contain all the keys from the different lists of blocked packages.
        /// </summary>
        protected static Dictionary<string, string> rejectionReasons = new() {
            { "com.unity.ads.ios-support", "Unity Ads are not allowed for security and privacy reasons" },
            { "com.unity.ads", "Unity Ads are not allowed for security and privacy reasons" },
            { "com.unity.services.levelplay", "Ads mediation is not allowed for security and privacy reasons" },
            { "com.unity.purchasing", "Unity IAP is not allowed" },
            { "com.unity.purchasing.udp", "Unity IAP is not allowed" },
            { "com.unity.xr.arcore", "ARCore must not be present for Android builds" },
            { "com.unity.adaptiveperformance", "Adaptive Performance is not allowed on AirConsole" },
            { "com.unity.modules.unityanalytics", "Unity Analytics are not allowed on automotive for security reasons" }
        };

        protected static List<string> blockedPackages = new() {
            "com.unity.ads.ios-support",
            "com.unity.ads",
            "com.unity.services.levelplay",
            "com.unity.purchasing",
            "com.unity.purchasing.udp"
        };

        protected static List<string> blockedPackagesAndroid = new() {
            "com.unity.xr.arcore",
            "com.unity.adaptiveperformance"
        };

        protected static List<string> blockedPackagesAndroidAutomotive = new() {
            "com.unity.modules.unityanalytics"
        };

        protected static List<string> blockedPackagesWebGL = new() { };

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report) {
            switch (report.summary.platform) {
                case BuildTarget.Android:
                case BuildTarget.WebGL:
                    ReportDisallowedUnityPackages();
                    break;

                default:
                    throw new UnityException($"AirConsole Plugin does not support platform {report.summary.platform}");
            }
        }

        [InitializeOnLoadMethod]
        private static void ReportDisallowedUnityPackages() {
            packages.Clear();
            packages.AddRange(blockedPackages);
#if UNITY_WEBGL
            packages.AddRange(blockedPackagesWebGL);
#elif UNITY_ANDROID
            packages.AddRange(blockedPackagesAndroid);
#if AIRCONSOLE_AUTOMOTIVE
            packages.AddRange(blockedPackagesAndroidAutomotive);
#endif
#endif
            Request = Client.List(true, true);
            packagesFound.Clear();
            EditorApplication.update -= Progress;
            EditorApplication.update += Progress;
        }

        private static void Progress() {
            if (!Request.IsCompleted) {
                return;
            }

            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android) {
                switch (Request.Status) {
                    case StatusCode.Success: {
                        foreach (PackageInfo packageInfo in Request.Result) {
                            packages.Where(package => packageInfo.packageId.StartsWith($"{package}@"))
                                .ToList()
                                .ForEach(package => {
                                    packagesFound.Add(package);
                                    LogFoundDisallowedPackages();
                                });
                        }

                        AndroidBuildNotAllowed = packagesFound.Count > 0;
                        if (AndroidBuildNotAllowed && EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android) {
                            if (!EditorUtility.DisplayDialog("AirConsole Android Error",
                                    $"To deploy to AirConsole AndroidTV, please remove the following packages from the PackageManager:\n-{string.Join("\n-", packagesFound)}",
                                    $"I understand and will remove {(packagesFound.Count == 1 ? "it" : "them")}!",
                                    "Please remove them for me")) {
                                string manifestPath =
                                    Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Packages", "manifest.json"));
                                Dictionary<string, Dictionary<string, string>> manifest =
                                    JsonConvert
                                        .DeserializeObject<
                                            Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(manifestPath));
                                packagesFound.ForEach(package => manifest["dependencies"].Remove(package));
                                File.WriteAllText(manifestPath, JsonConvert.SerializeObject(manifest, Formatting.Indented));
                            }
                        }

                        break;
                    }
                    case StatusCode.Failure: {
                        Debug.LogError(Request.Error.message);
                        break;
                    }
                }
            }

            EditorApplication.update -= Progress;
        }

        private static void LogFoundDisallowedPackages() {
            packagesFound.ForEach(it =>
                Debug.LogError($"AirConsole Error: Please remove package \"{it}\" from Window > Package Manager\n"
                               + $"Reason: {rejectionReasons[it] ?? "Unknown reason"}"));
        }
    }
}
#endif