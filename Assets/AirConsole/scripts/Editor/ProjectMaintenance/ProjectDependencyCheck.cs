#if !DISABLE_AIRCONSOLE
namespace NDream.AirConsole.Editor {
    using UnityEditor;
    using UnityEngine;
    using System.Text.RegularExpressions;

    public abstract class ProjectDependencyCheck {
        [InitializeOnLoadMethod]
        private static void CheckUnityVersions() {
            ValidateUnityVersion(EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android);
        }

        /// <summary>
        /// To meet automotive requirements, we need to have certain minimum versions of Unity that have updated dependencies.
        /// </summary>
        private static void ValidateUnityVersion(bool invokeErrorOnFail = false) {
            string[] versions = Application.unityVersion.Split(".");
            switch (versions[0]) {
                case "2022": {
                    (bool versionCheck, bool patchCheck) = SemVerCheck.IfMajorMinorPatchAtLeast(2022, 3, 57, Application.unityVersion);
                    if (versionCheck && !patchCheck) {
                        if (invokeErrorOnFail) {
                            InvokeErrorOrLog("For Android automotive usage, AirConsole requires at least 2022.3.57f1",
                                "Unity 2022 version too old", invokeErrorOnFail);
                        }
                    }

                    break;
                }

                case "6000": {
                    (bool versionCheck, bool patchCheck) = SemVerCheck.IfMajorMinorPatchAtLeast(6000, 0, 36, Application.unityVersion);
                    if (versionCheck && !patchCheck) {
                        InvokeErrorOrLog("For Android automotive usage, AirConsole requires at least 6000.0.36f1",
                            "Unity 6 version too old", invokeErrorOnFail);
                    }

                    break;
                }

                default:
                    if (!SemVerCheck.IsAtLeast(6000, 0, 36, Application.unityVersion)) {
                        InvokeErrorOrLog(
                            "For Android automotive usage, AirConsole requires at least Unity 6000.0.36f1",
                            "Unity 6 or newer version too old", invokeErrorOnFail);
                    }

                    break;
            }
        }

        private static void InvokeErrorOrLog(string message, string title, bool shallError = false) {
            if (!shallError) {
                AirConsoleLogger.Log(message);
                return;
            }

            EditorNotificationService.InvokeError(message, false, title);
        }
    }
    

    public abstract class SemVerCheck {
        /// <summary>
        /// Determines if the specified version is at least the given major, minor, and patch version.
        /// </summary>
        /// <param name="major">The minimum required major version.</param>
        /// <param name="minor">The minimum required minor version.</param>
        /// <param name="patch">The minimum required patch version.</param>
        /// <param name="version">The version string to compare, expected in the format "major.minor.patch".</param>
        /// <returns>
        /// True if the version is at least the specified major, minor, and patch version; otherwise, false.
        /// </returns>
        internal static bool IsAtLeast(int major, int minor, int patch, string version) {
            (int foundMajor, int foundMinor, int foundPatch) = GetMajorMinorPatchFromVersion(version);
            return major >= foundMajor && minor >= foundMinor && patch >= foundPatch;
        }

        /// <summary>
        /// Checks if the given version is at least the specified major, minor, and patch version.
        /// </summary>
        /// <param name="major">The required major version.</param>
        /// <param name="minor">The required minor version.</param>
        /// <param name="patch">The required patch version.</param>
        /// <param name="version">The version string to check, expected in the format "major.minor.patch".</param>
        /// <returns>
        /// A tuple where the first value indicates if the major and minor versions match,
        /// and the second value indicates if the patch version is at least the required value.
        /// </returns>
        internal static (bool, bool) IfMajorMinorPatchAtLeast(int major, int minor, int patch, string version) {
            (int foundMajor, int foundMinor, int foundPatch) = GetMajorMinorPatchFromVersion(version);

            if (foundMajor != major || foundMinor != minor) {
                return (false, false);
            }

            return (true, foundPatch >= patch);
        }

        private static (int, int, int) GetMajorMinorPatchFromVersion(string version) {
            Regex versionExtractor = new("^(?<Major>\\d{4})\\.(?<Minor>\\d+)\\.(?<Patch>\\d+)f\\d+$");
            Match match = versionExtractor.Match(version);
            if (!match.Success) {
                throw new UnityException("No valid version found ");
            }

            return (int.Parse(match.Groups["Major"].Value), int.Parse(match.Groups["Minor"].Value), int.Parse(match.Groups["Patch"].Value));
        }
    }
}
#endif