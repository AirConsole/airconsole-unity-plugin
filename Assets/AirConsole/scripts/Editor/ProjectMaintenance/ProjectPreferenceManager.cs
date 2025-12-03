#if !DISABLE_AIRCONSOLE
using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace NDream.AirConsole.Editor {
    internal abstract class ProjectPreferenceManager {
        private static string PreferencePath {
            get => Path.GetFullPath(Path.Combine(Application.dataPath, "AirConsole", "airconsole.prefs"));
        }

        /// <summary>
        /// Loads the Project Preferences.
        /// </summary>
        /// <returns >The loaded ProjectPreferences object.</returns>
        internal static ProjectPreferences LoadPreferences() {
            if (!File.Exists(PreferencePath)) {
                ProjectPreferences newPreferences = new();
                SavePreferences(newPreferences);
                return newPreferences;
            }

            ProjectPreferences result
                = JsonConvert.DeserializeObject<ProjectPreferences>(File.ReadAllText(PreferencePath));
            return result;
        }

        /// <summary>
        /// Saves new project preferences. This overwrites the complete preferences. This does not update single changed keys.
        /// </summary>
        /// <param name="preferences">The ProjectPreferences to persist.</param>
        /// <exception cref="ArgumentNullException">Thrown when preferences is null</exception>
        internal static void SavePreferences(ProjectPreferences preferences) {
            if (preferences == null) {
                throw new ArgumentNullException(nameof(preferences));
            }

            File.WriteAllText(PreferencePath, JsonConvert.SerializeObject(preferences, Formatting.None));
        }
    }
}
#endif
