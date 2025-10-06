#if !DISABLE_AIRCONSOLE
using System;
using System.Text.RegularExpressions;
using UnityEditor.Build;

namespace NDream.AirConsole.Editor {
    internal abstract class SemVerCheck {
        /// <summary>
        /// Validates that Unity matches a required minimum version.
        /// </summary>
        /// <param name="requiredVersion">The required version. We use Version to form Unity's version string of MAJOR.MINOR.BUILDfREVISION</param>
        /// <param name="detectedVersion">The version string to check, expected in the format "major.minor.patch".</param>
        /// <returns> True, if the detected version is greater than or equal to the required version.</returns>
        internal static bool ValidateUnityVersionMinimum(Version requiredVersion, string detectedVersion) {
            Version foundVersion = GetMajorMinorPatchFromVersion(detectedVersion);
            return foundVersion.CompareTo(requiredVersion) >= 0;
        }

        private static Version GetMajorMinorPatchFromVersion(string version) {
            Regex versionExtractor = new(@"^(?<Major>\d{4})\.(?<Minor>\d+)\.(?<Build>\d+)f(?<Revision>\d+)$");
            Match match = versionExtractor.Match(version);
            if (!match.Success) {
                throw new BuildFailedException(
                    "No valid Unity Application version found. Format should be <MAJOR>.<MINOR>.<BUILD>f<REVISION>");
            }

            return new Version(
                int.Parse(match.Groups["Major"].Value),
                int.Parse(match.Groups["Minor"].Value),
                int.Parse(match.Groups["Build"].Value),
                int.Parse(match.Groups["Revision"].Value)
            );
        }
    }
}
#endif
