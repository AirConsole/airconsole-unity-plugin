#if !DISABLE_AIRCONSOLE
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Directory = System.IO.Directory;
using File = System.IO.File;

namespace NDream.AirConsole.Editor {
    public abstract class ProjectUpgradeEditor {
        [InitializeOnLoadMethod]
        public static void UpgradeProject() {
            PluginVersionUpdateState versionUpgradeInformation = PluginVersionTracker.GetPluginVersionUpdateState();

            if (versionUpgradeInformation == null || versionUpgradeInformation.RequiresUpdate == false) {
                AirConsoleLogger.Log(() => "AirConsole plugin upgrade check: No additional plugin upgrades pending.");
                return;
            }

            HandleVersionUpgrade(versionUpgradeInformation);
        }

        private static void HandleVersionUpgrade(PluginVersionUpdateState state) {
            switch (state.PreviousPluginVersion) {
                case "":
                    string targetVersion = "2.6.x";
                    UpgradeProjectFromUnknownTo260();
                    PluginVersionTracker.RecordPluginVersionUpdate(targetVersion);

                    PluginVersionUpdateState additionalUpgradesState = new(targetVersion);
                    if (additionalUpgradesState.RequiresUpdate) {
                        HandleVersionUpgrade(additionalUpgradesState);
                    }

                    AirConsoleLogger.Log(() => "AirConsole plugin successfully upgraded to 2.6.x");

                    break;

                default:
                    AirConsoleLogger.LogDevelopment(() => "Default Plugin Upgrade path hit. If this is expected, ignore this message");

                    break;
            }
        }

        private static void UpgradeProjectFromUnknownTo260() {
            EditorApplication.LockReloadAssemblies();
            string sourceCodeDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "AirConsole", "scripts"));

            string[] filesToMove = Directory.GetFiles(sourceCodeDirectory, "*.cs*");
            string targetDirectory = Path.Combine(Application.dataPath, "AirConsole", "scripts", "Runtime");
            if (!Directory.Exists(targetDirectory)) {
                Directory.CreateDirectory(targetDirectory);
            }

            foreach (string sourceFilePath in filesToMove) {
                string targetFilePath = Path.Combine(targetDirectory, Path.GetFileName(sourceFilePath));
                File.Move(sourceFilePath, targetFilePath);
            }

            // Fix folder casing for future upgrades
            string[] directoriesInSources = Directory.GetDirectories(sourceCodeDirectory);
            foreach (string directoryInSource in directoriesInSources) {
                bool isLowerCaseEditorDirectory = directoryInSource
                    .TrimEnd(Path.DirectorySeparatorChar)
                    .EndsWith($"{Path.DirectorySeparatorChar}editor", StringComparison.InvariantCulture);
                if (isLowerCaseEditorDirectory) {
                    string tmpPath = directoryInSource + "tmp";
                    Directory.Move(directoryInSource, tmpPath);
                    Directory.Move(tmpPath, Path.Combine(sourceCodeDirectory, "Editor"));

                    File.Move(directoryInSource + ".meta", tmpPath + ".meta");
                    File.Move(tmpPath + ".meta", Path.Combine(sourceCodeDirectory, "Editor") + ".meta");
                }
            }

            EditorApplication.UnlockReloadAssemblies();
            AssetDatabase.Refresh();
        }
    }

    internal abstract class PluginVersionTracker {
        private static string GetLastKnownPluginVersion() {
            ProjectPreferences preferences = ProjectPreferenceManager.LoadPreferences();
            string previousPluginVersion = preferences.PluginVersion ?? string.Empty;
            return string.IsNullOrEmpty(previousPluginVersion) ? string.Empty : previousPluginVersion;
        }

        internal static int CalculatePluginVersion(string versionString) {
            if (string.IsNullOrEmpty(versionString)) {
                return 0;
            }

            string[] version = versionString.Split('.');

            int lengthVersion = version.Length;
            int result = 0;

            for (int i = 0; i < version.Length; i++) {
                int versionNumber = int.Parse(version[i]);
                int numericBase = Mathf.FloorToInt(Mathf.Pow(100, lengthVersion - i - 1));
                result += numericBase * versionNumber;
            }

            return result;
        }

        internal static PluginVersionUpdateState GetPluginVersionUpdateState() {
            return new PluginVersionUpdateState(GetLastKnownPluginVersion());
        }

        internal static void RecordPluginVersionUpdate(string newVersionString) {
            ProjectPreferences preferences = ProjectPreferenceManager.LoadPreferences();
            preferences.PluginVersion = newVersionString;
            ProjectPreferenceManager.SavePreferences(preferences);
        }

        internal static void DeleteLastPluginVersionKey() {
            ProjectPreferences preferences = ProjectPreferenceManager.LoadPreferences();
            preferences.PluginVersion = string.Empty;
            ProjectPreferenceManager.SavePreferences(preferences);
        }

        [MenuItem("Tools/AirConsole/Force upgrade project")]
        public static void ForceUpgradeProject() {
            DeleteLastPluginVersionKey();
            ProjectUpgradeEditor.UpgradeProject();
        }
    }

    internal class PluginVersionUpdateState {
        internal string PreviousPluginVersion { get; }
        internal bool RequiresUpdate { get; }

        public PluginVersionUpdateState(string previousPluginVersion) {
            PreviousPluginVersion = GetUpgradeVersionComponent(previousPluginVersion);
            int previousVersion = PluginVersionTracker.CalculatePluginVersion(PreviousPluginVersion);
            int currentVersion = PluginVersionTracker.CalculatePluginVersion(GetUpgradeVersionComponent(Settings.VERSION));
            RequiresUpdate = currentVersion > previousVersion;
        }

        /// <summary>
        /// Creates a version string containing only of the MAJOR.MINOR part
        /// </summary>
        /// <param name="version">The version to build the version component from.</param>
        /// <returns>MAJOR.MINOR of the provided version or string.Empty if version was null or empty.</returns>
        private static string GetUpgradeVersionComponent(string version) {
            if (string.IsNullOrEmpty(version)) {
                return string.Empty;
            }

            string[] versionParts = version.Split('.');
            return $"{versionParts[0]}.{versionParts[1]}";
        }
    }
}
#endif