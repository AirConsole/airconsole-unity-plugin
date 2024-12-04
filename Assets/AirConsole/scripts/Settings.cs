using UnityEditor;
using UnityEngine;

namespace NDream.AirConsole {
    public static class Settings {
        public const string VERSION = "2.5.3";

        // ReSharper disable once UnusedMember.Global // Used by AirConsole on Android only
        public const string AIRCONSOLE_BASE_URL = "https://www.airconsole.com/";
        public const string AIRCONSOLE_DEV_URL_HTTPS = "https://www.airconsole.com/";
        public const string AIRCONSOLE_DEV_URL_HTTP = "http://http.airconsole.com/";

        public const string AIRCONSOLE_PROFILE_PICTURE_URL = "https://www.airconsole.com/api/profile-picture?uid=";
        public const string WEBSOCKET_PATH = "/api";
        public const int DEFAULT_WEBSERVER_PORT = 7842;
        public const int DEFAULT_WEBSOCKET_PORT = 7843;
        public static int webServerPort = 7842;
        public static int webSocketPort = 7843;
        public static DebugLevel debug = new DebugLevel();
        public static string Python2Path = "/usr/local/bin/python2";

        public static readonly string WEBTEMPLATE_PATH;

        static Settings() {
            string templateName;
            if (IsUnity6OrHigher()) {
                templateName = "AirConsole-U6";
            } else if (Application.unityVersion.Substring(0, 3) == "202") {
                templateName = "AirConsole-2020";
            } else {
                templateName = "AirConsole";  
            }

            WEBTEMPLATE_PATH = $"/WebGLTemplates/{templateName}";

#if UNITY_EDITOR
            string[] templateUri = UnityEditor.PlayerSettings.WebGL.template.Split(":");
            
            if (templateUri.Length != 2 || templateUri[0].ToUpper() == "APPLICATION:" || templateUri[1] != templateName) {
                Debug.LogError($"Unity version <b>{Application.unityVersion}</b> needs the AirConsole WebGL template <b>{templateName}</b> to work.\nPlease change the WebGL template in your Project Settings under Player > Resolution and Presentation > WebGL Template.");
            }
#endif
        }

        private static bool IsUnity6OrHigher() {
            return int.Parse(Application.unityVersion.Split('.')[0]) >= 6000;
        }
    }
}
