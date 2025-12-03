#if !DISABLE_AIRCONSOLE
using System.ComponentModel;
using Newtonsoft.Json;

namespace NDream.AirConsole.Editor {
    internal class ProjectPreferences {
        [DefaultValue("")]
        [JsonProperty("pv")]
        private string pluginVersion;

        internal string PluginVersion {
            get => pluginVersion;
            set => pluginVersion = value;
        }
    }
}
#endif
