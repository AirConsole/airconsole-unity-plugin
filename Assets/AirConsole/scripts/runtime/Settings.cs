using UnityEngine;
using System.Collections;

namespace NDream.AirConsole {
	public static class Settings {

		public const string VERSION = "2.6.0";
		
		// public const string AIRCONSOLE_BASE_URL = "https://ci-subbranch-marc-native-game-sizing-dot-airconsole.appspot.com/"; //"https://www.airconsole.com/";
		// public const string AIRCONSOLE_DEV_URL_HTTPS = "https://ci-subbranch-marc-native-game-sizing-dot-airconsole.appspot.com/"; // "https://www.airconsole.com/";
		// public const string AIRCONSOLE_DEV_URL_HTTP = "https://ci-subbranch-marc-native-game-sizing-dot-airconsole.appspot.com/"; // "http://http.airconsole.com/";
		
		// local
		public const string AIRCONSOLE_BASE_URL = "http://10.0.2.2:8090/"; //"https://www.airconsole.com/";
		public const string AIRCONSOLE_DEV_URL_HTTPS = "http://localhost:8090/"; // "https://www.airconsole.com/";
		public const string AIRCONSOLE_DEV_URL_HTTP = "http://localhost:8090/"; // "http://http.airconsole.com/";
		
		// ReSharper disable once UnusedMember.Global // Used by AirConsole on Android only
		// public const string AIRCONSOLE_BASE_URL = "https://ci-daniel-native-game-sizing-dot-airconsole.appspot.com/"; //"https://www.airconsole.com/";
		// public const string AIRCONSOLE_DEV_URL_HTTPS = "https://ci-daniel-native-game-sizing-dot-airconsole.appspot.com/"; // "https://www.airconsole.com/";
		// public const string AIRCONSOLE_DEV_URL_HTTP = "https://ci-daniel-native-game-sizing-dot-airconsole.appspot.com/"; // "http://http.airconsole.com/";

		public const string AIRCONSOLE_PROFILE_PICTURE_URL = "https://ci-daniel-native-game-sizing-dot-airconsole.appspot.com/api/profile-picture?uid=";//"https://www.airconsole.com/api/profile-picture?uid=";
		public const string WEBSOCKET_PATH = "/api";
		public const int DEFAULT_WEBSERVER_PORT = 7842;
		public const int DEFAULT_WEBSOCKET_PORT = 7843;
		public static int webServerPort = 7842;
		public static int webSocketPort = 7843;
		public static DebugLevel debug = new DebugLevel ();
		public static string Python2Path = "/usr/local/bin/python2";

		public static readonly string WEBTEMPLATE_PATH;

		static Settings() {
			string templateName = "";
			// For Unity 2020 and up
			if (Application.unityVersion.Substring(0, 3) == "202") {
				templateName = "AirConsole-2020";
			} else {
				templateName = "AirConsole";  
			}

			WEBTEMPLATE_PATH = $"/WebGLTemplates/{templateName}";

#if UNITY_EDITOR
			UnityEditor.PlayerSettings.WebGL.template = $"PROJECT:{templateName}";
#endif
		}
	}
}
