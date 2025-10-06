using System;
#if !DISABLE_AIRCONSOLE
namespace NDream.AirConsole.Editor {
    using UnityEditor;
    using UnityEngine;

    public abstract class ProjectDependencyCheck {
        [InitializeOnLoadMethod]
        private static void CheckUnityVersions() {
            ValidateUnityVersion(EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android);
        }

        /// <summary>
        /// To meet automotive requirements, we need to have certain minimum versions of Unity that have updated dependencies.
        /// To make the game developer experience consistent, we always check for this.
        /// </summary>
        internal static void ValidateUnityVersion(bool invokeErrorOnFail = false) {
            string unityVersion = GetUnityVersion();
            string[] versions = unityVersion.Split(".");

            Version requiredVersion = versions[0] switch {
                // 2022.3.?? -> 2022.3.62f2 first with CVE-2025-59489 fix
                "2022" => new Version(2022, 3, 62, 2),
                "6000" => versions[1] switch {
                    // 6000.0.?? -> 6000.0.58f2 first with CVE-2025-59489 fix
                    "0" => new Version(6000, 0, 58, 2),

                    // 6000.1.?? -> 6000.1.17f1 first with CVE-2025-59489 fix
                    "1" => new Version(6000, 1, 17, 1),

                    // 6000.2.?? -> 6000.2.6f2 first with CVE-2025-59489 fix
                    "2" => new Version(6000, 2, 6, 2),

                    // 6000.3.?? -> 6000.3.0b4 first with CVE-2025-59489 fix but we require official releases
                    _ => new Version(6000, 3, 0, 1)
                },
                _ => new Version(6000, 0, 58, 2)
            };

            if (!SemVerCheck.ValidateUnityVersionMinimum(requiredVersion, unityVersion)) {
                InvokeErrorOrLog(
                    $"For security (CVE-2025-59489), AirConsole requires at least Unity {StringFromVersion(requiredVersion)}",
                    $"Insecure version {unityVersion}", invokeErrorOnFail);
            }
        }

        private static void InvokeErrorOrLog(string message, string title, bool shallError = false) {
#if UNITY_INCLUDE_TESTS
            if (InvokeErrorOrLogOverride != null) {
                InvokeErrorOrLogOverride(message, title, shallError);
                return;
            }
#endif
            if (!shallError) {
                AirConsoleLogger.LogWarning(() => message);
                return;
            }

            EditorNotificationService.InvokeError(message, false, title);
        }

        private static string StringFromVersion(Version version) => $"{version.Major}.{version.Minor}.{version.Build}f{version.Revision}";

        private static string GetUnityVersion() {
#if UNITY_INCLUDE_TESTS
            return UnityVersionProvider?.Invoke() ?? Application.unityVersion;
#else
            return Application.unityVersion;
#endif
        }

#if UNITY_INCLUDE_TESTS
        internal static Func<string>? UnityVersionProvider;
        internal static Action<string, string, bool>? InvokeErrorOrLogOverride;
#endif
    }
}
#endif
