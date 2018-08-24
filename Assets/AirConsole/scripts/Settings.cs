using UnityEngine;
using System.Collections;

namespace NDream.AirConsole {
	public static class Settings {

		public const string VERSION = "2.0"; 
		public const string AIRCONSOLE_BASE_URL = "https://www.airconsole.com/";
		public const string AIRCONSOLE_URL = "http://airconsole.com/#";
		public const string AIRCONSOLE_SIMULATOR_URL = "http://airconsole.com/simulator/#";
		public const string AIRCONSOLE_DEBUG_URL = "http://airconsole.com/#debug:";
		public const string AIRCONSOLE_DEBUG_SIMULATOR_URL = "http://airconsole.com/simulator/#debug:";
		public const string AIRCONSOLE_PROFILE_PICTURE_URL = "https://www.airconsole.com/api/profile-picture?uid=";
		public const string WEBTEMPLATE_PATH = "/WebGLTemplates/AirConsole";
		public const string WEBSOCKET_PATH = "/api";
		public const int DEFAULT_WEBSERVER_PORT = 7842;
		public const int DEFAULT_WEBSOCKET_PORT = 7843;
		public static int webServerPort = 7842;
		public static int webSocketPort = 7843;
		public static DebugLevel debug = new DebugLevel ();

	}
}

