using UnityEngine;
using System.Collections;

namespace NDream.AirConsole {
	public static class Settings {

		public const string VERSION = "2.6.0";
		
		// ReSharper disable once UnusedMember.Global // Used by AirConsole on Android only
		// public const string AIRCONSOLE_BASE_URL = "https://www.airconsole.com/"; //"https://www.airconsole.com/";
		// public const string AIRCONSOLE_DEV_URL_HTTPS = "https://www.airconsole.com/"; // "https://www.airconsole.com/";
		// public const string AIRCONSOLE_DEV_URL_HTTP = "http://http.airconsole.com/"; // "http://http.airconsole.com/";
		
		
		// public const string AIRCONSOLE_BASE_URL = "https://local19.airconsole.com/"; //"https://www.airconsole.com/";
		// public const string AIRCONSOLE_DEV_URL_HTTPS = "https://local19.airconsole.com/"; // "https://www.airconsole.com/";
		// public const string AIRCONSOLE_DEV_URL_HTTP = "http://local19.airconsole.com/"; // "http://http.airconsole.com/";

		// public const string AIRCONSOLE_BASE_URL = "https://ci-bmw-android-native-dot-airconsole.appspot.com/"; //"https://www.airconsole.com/";
		// public const string AIRCONSOLE_DEV_URL_HTTPS = "https://ci-bmw-android-native-dot-airconsole.appspot.com/"; // "https://www.airconsole.com/";
		// public const string AIRCONSOLE_DEV_URL_HTTP = "http://ci-bmw-android-native-dot-airconsole.appspot.com/"; // "http://http.airconsole.com/";

		// local
		public const string AIRCONSOLE_BASE_URL = "http://10.0.2.2:8090/"; //"https://www.airconsole.com/";
		public const string AIRCONSOLE_DEV_URL_HTTPS = "http://10.0.2.2:8090/"; // "https://www.airconsole.com/";
		public const string AIRCONSOLE_DEV_URL_HTTP = "http://10.0.2.2:8090/"; // "http://http.airconsole.com/";
		
		// ReSharper disable once UnusedMember.Global // Used by AirConsole on Android only
		// public const string AIRCONSOLE_BASE_URL = "https://ci-marc-android-native-dot-airconsole.appspot.com/"; //"https://www.airconsole.com/";
		// public const string AIRCONSOLE_DEV_URL_HTTPS = "https://ci-marc-android-native-dot-airconsole.appspot.com/"; // "https://www.airconsole.com/";
		// public const string AIRCONSOLE_DEV_URL_HTTP = "https://ci-marc-android-native-dot-airconsole.appspot.com/"; // "http://http.airconsole.com/";

		public const string AIRCONSOLE_PROFILE_PICTURE_URL = "https://www.airconsole.com/api/profile-picture?uid=";
		public const string WEBSOCKET_PATH = "/api";
		public const int DEFAULT_WEBSERVER_PORT = 7842;
		public const int DEFAULT_WEBSOCKET_PORT = 7843;
		public static int webServerPort = 7842;
		public static int webSocketPort = 7843;
		public static DebugLevel debug = new DebugLevel ();
		public static string Python2Path = "/usr/local/bin/python2";

		public static readonly string WEBTEMPLATE_PATH;

        private const string TEMPLATE_NAME_2020 = "AirConsole-2020";
        private const string TEMPLATE_NAME_U6 = "AirConsole-U6";

		static Settings() {
            string templateName;
            if (IsUnity6OrHigher()) {
                templateName = TEMPLATE_NAME_U6;
            } else {
                templateName = TEMPLATE_NAME_2020;
            }
            WEBTEMPLATE_PATH = $"/WebGLTemplates/{templateName}";
        }

        private static bool IsUnity6OrHigher() {
            return int.Parse(Application.unityVersion.Split('.')[0]) >= 6000;
        }
	}
}
