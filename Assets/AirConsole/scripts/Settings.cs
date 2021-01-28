using UnityEngine;
using System.Collections;

namespace NDream.AirConsole {
	public static class Settings {

		public const string VERSION = "2.5";
		public const string AIRCONSOLE_BASE_URL = "https://www.airconsole.com/";
		public const string AIRCONSOLE_DEV_URL = "http://www.airconsole.com/";
        public const string AIRCONSOLE_PROFILE_PICTURE_URL = "https://www.airconsole.com/api/profile-picture?uid=";
		public const string WEBSOCKET_PATH = "/api";
		public const int DEFAULT_WEBSERVER_PORT = 7842;
		public const int DEFAULT_WEBSOCKET_PORT = 7843;
		public static int webServerPort = 7842;
		public static int webSocketPort = 7843;
		public static DebugLevel debug = new DebugLevel ();

		public static readonly string WEBTEMPLATE_PATH;

		static Settings() {
			// For Unity 2020 and up
			if (Application.unityVersion.Substring(0, 3) == "202") {
				WEBTEMPLATE_PATH = "/WebGLTemplates/AirConsole-2020";
			} else {
				WEBTEMPLATE_PATH = "/WebGLTemplates/AirConsole";
			}
		}
	}
}

