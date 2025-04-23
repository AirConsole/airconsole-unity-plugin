#if !DISABLE_AIRCONSOLE
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace NDream.AirConsole.Editor {
	[InitializeOnLoad]
	class Extentions {

		public static WebListener webserver = new WebListener ();

		static Extentions () {

			InitSettings ();

			if (webserver != null) {
				webserver.Start ();
			}

			PlayMode.PlayModeChanged += OnPlayModeStateChanged;

		}

		[MenuItem("Assets/Create/AirConsole")]
		[MenuItem("GameObject/Create Other/AirConsole")]
		static void CreateAirConsoleObject () {

			AirConsole airConsole = AirConsole.ACFindObjectOfType<AirConsole> ();

			if (airConsole == null) {

				GameObject _tmp = new GameObject ("AirConsole");
				_tmp.AddComponent<AirConsole> ();

			} else {

				EditorUtility.DisplayDialog ("Already exists", "AirConsole object already exists in the current scene", "ok");
				EditorGUIUtility.PingObject (airConsole.GetInstanceID ());
			}
		}

		public static void OnPlayModeStateChanged (PlayModeState currentMode, PlayModeState changedMode) {

			if (currentMode == PlayModeState.Stopped && changedMode == PlayModeState.Playing ||
				currentMode == PlayModeState.AboutToPlay && changedMode == PlayModeState.Playing) {

				AirConsole controller = AirConsole.ACFindObjectOfType<AirConsole>();
				OpenBrowser (controller, Application.dataPath + Settings.WEBTEMPLATE_PATH);
			}
		}

		public static void InitSettings () {

			if (EditorPrefs.GetInt ("webServerPort") != 0) {
				Settings.webServerPort = EditorPrefs.GetInt ("webServerPort");
			}

			if (EditorPrefs.GetInt ("webSocketPort") != 0) {
				Settings.webSocketPort = EditorPrefs.GetInt ("webSocketPort");
			}

			if (EditorPrefs.GetBool ("debugInfo", true) != true) {
				Settings.debug.info = EditorPrefs.GetBool ("debugInfo");
			}

			if (EditorPrefs.GetBool ("debugWarning", true) != true) {
				Settings.debug.warning = EditorPrefs.GetBool ("debugWarning");
			}

			if (EditorPrefs.GetBool ("debugError", true) != true) {
				Settings.debug.error = EditorPrefs.GetBool ("debugError");
			}

			if (EditorPrefs.GetString("python2Path", "") != "") {
				Settings.Python2Path = EditorPrefs.GetString("python2Path");
			}
		}

		public static void ResetDefaultValues () {

			Settings.debug.info = DebugLevel.DEFAULT_INFO;
			Settings.debug.warning = DebugLevel.DEFAULT_WARNING;
			Settings.debug.error = DebugLevel.DEFAULT_ERROR;

			EditorPrefs.SetBool ("debugInfo", Settings.debug.info);
			EditorPrefs.SetBool ("debugWarning", Settings.debug.warning);
			EditorPrefs.SetBool ("debugError", Settings.debug.error);

			Settings.webServerPort = Settings.DEFAULT_WEBSERVER_PORT;
			Settings.webSocketPort = Settings.DEFAULT_WEBSOCKET_PORT;

			EditorPrefs.SetInt ("webServerPort", Settings.webServerPort);
			EditorPrefs.SetInt ("webSocketPort", Settings.webSocketPort);
		}

		public static void OpenBrowser (AirConsole controller, string startUpPath) {

			// set the root path for webserver
			webserver.SetPath (startUpPath);
			webserver.Start ();

			if (controller != null && controller.enabled) {

				if (controller.controllerHtml != null) {

					string sourcePath = Path.Combine (Directory.GetCurrentDirectory (), AssetDatabase.GetAssetPath (controller.controllerHtml));
					string targetPath = Path.Combine (Directory.GetCurrentDirectory (), "Assets" + Settings.WEBTEMPLATE_PATH + "/controller.html");

					// rename index.html to screen.html
					File.Copy (sourcePath, targetPath, true);

					if (controller.browserStartMode != StartMode.NoBrowserStart) {

						string url = AirConsole.GetUrl (controller.browserStartMode) + GetLocalAddress () + "/";

						// add port info if starting the unity editor version
						if (startUpPath.Contains (Settings.WEBTEMPLATE_PATH)) {
							url += "?unity-editor-websocket-port=" + Settings.webSocketPort + "&unity-plugin-version=" + Settings.VERSION;
                        }
						Application.OpenURL (url);
					} else {
						AirConsole.instance.ProcessJS ("{action:\"onReady\", code:\"0\", devices:[], server_time_offset: 0, device_id: 0, location: \"\" }");
					}

				} else {

					EditorUtility.DisplayDialog ("AirConsole", "Please link a controller file to the AirConsole object.", "ok");
					Debug.Break ();
				}
			}
		}

		public static string GetLocalAddress () {
			if(!string.IsNullOrEmpty(AirConsole.instance.LocalIpOverride)) {
				return AirConsole.instance.LocalIpOverride;
			}
			string localIP = "";

			using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0)) {
				socket.Connect("8.8.8.8", 65530);
				IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
				localIP = endPoint.Address.ToString();
			}
			localIP += ":" + Settings.webServerPort;
			return localIP.StartsWith("http") ? localIP : $"http://{localIP}";
		}
	}
}
#endif