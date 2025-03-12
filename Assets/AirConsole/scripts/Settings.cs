#if !DISABLE_AIRCONSOLE
using UnityEngine;

namespace NDream.AirConsole {
    public static class Settings {
        public const string VERSION = "2.5.7";

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

        private const string TEMPLATE_NAME = "AirConsole";
        private const string TEMPLATE_NAME_2020 = "AirConsole-2020";
        private const string TEMPLATE_NAME_U6 = "AirConsole-U6";

        public static readonly string[] TEMPLATE_NAMES = { TEMPLATE_NAME, TEMPLATE_NAME_2020, TEMPLATE_NAME_U6 };

        static Settings() {
            string templateName;
#if UNITY_6000_0_OR_NEWER
            templateName = TEMPLATE_NAME_U6;
#elif !UNITY_2020_1_OR_NEWER
            templateName = TEMPLATE_NAME;
#else
            templateName = TEMPLATE_NAME_2020;
#endif
            WEBTEMPLATE_PATH = $"/WebGLTemplates/{templateName}";
        }
        
        

        private static bool IsUnity6OrHigher() {
            return int.Parse(Application.unityVersion.Split('.')[0]) >= 6000;
        }
    }
}
#endif