using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System;
using WebSocketSharp;
using WebSocketSharp.Server;
using Newtonsoft.Json.Linq;


namespace NDream.AirConsole {

    public enum StartMode {
        VirtualControllers,
        Normal,
        NoBrowserStart
    }

    public delegate void OnReady(string code);

    public delegate void OnMessage(int from, JToken data);
    public delegate void OnDeviceStateChange(int device_id, JToken user_data);
	public delegate void OnConnect(int device_id);
	public delegate void OnDisconnect(int device_id);
	public delegate void OnCustomDeviceStateChange(int device_id, JToken custom_device_data);
	public delegate void OnDeviceProfileChange(int device_id);
   
    public class AirConsole : MonoBehaviour {
		#region airconsole api

		/// <summary>
		/// AirConsole Singleton Instance.
		/// This is your direct access to the AirConsole API.
		/// </summary>
		/// <value>AirConsole Singleton Instance</value>
		public static AirConsole instance {
			get {
				
				if (_instance == null) {
					_instance = GameObject.FindObjectOfType<AirConsole>();
					if (_instance != null) {
						DontDestroyOnLoad(_instance.gameObject);
					}
				}
				
				return _instance;
			}
		}
		
		/// <summary>
		/// Gets called when the game console is ready.
		/// This event also also fires onConnect for all devices that already are
		/// connected and have loaded your game.
		/// This event also fires OnCustomDeviceStateChange for all devices that are
		/// connected, have loaded your game and have set a custom Device State.
		/// </summary>
		/// <param name="code">The AirConsole join code.</param> 
        public event OnReady onReady;

		/// <summary>
		/// Gets called when a message is received from another device
		/// that called message() or broadcast().
		/// </summary>
		/// <param name="from">The device ID that sent the message.</param> 
		/// <param name="data">The data that was sent.</param>
        public event OnMessage onMessage;

		/// <summary>
		/// Gets called when a device joins/leaves a game session or updates its DeviceState (custom DeviceState, profile pic, nickname). 
		/// This is function is also called every time OnConnect, OnDisconnect or OnCustomDeviceStateChange is called. It's like their root function.
		/// </summary>
		/// <param name="device_id">the device ID that changed its DeviceState.</param>
		/// <param name="data"> the data of that device. If undefined, the device has left.</param>
        public event OnDeviceStateChange onDeviceStateChange;

		/// <summary>
		/// Gets called when a device has connected and loaded the game.
		/// </summary>
		/// <param name="device_id">the device ID that loaded the game.</param>
		public event OnConnect onConnect;

		/// <summary>
		/// Gets called when a device has left the game.
		/// </summary>
		/// <param name="device_id">the device ID that left the game.</param>
		public event OnDisconnect onDisconnect;

		/// <summary>
		/// Gets called when a device updates it's custom DeviceState by calling SetCustomDeviceState or SetCustomDeviceStateProperty. 
		/// Make sure you understand the power of device states: http://developers.airconsole.com/#/guides/device_ids_and_states
		/// </summary>
		/// <param name="device_id">the device ID that changed its customDeviceState.</param> 
		/// <param name="cutsom_data">The custom DeviceState data value.</param>
		public event OnCustomDeviceStateChange onCustomDeviceStateChange;

		/// <summary>
		/// Gets called when a device updates it's profile pic, nickname or email.
		/// </summary>
		/// <param name="device_id">The device_id that changed its profile.</param>
		public event OnDeviceProfileChange onDeviceProfileChange;

		/// <summary>
		/// Determines whether the AirConsole Unity Plugin is ready. Use onReady event instead if possible.
		/// </summary>
		/// <returns><c>true</c> if the AirConsole Unity Plugin is ready; otherwise, <c>false</c>.</returns>
		public bool IsAirConsoleUnityPluginReady() {
			return wsListener != null && wsListener.IsReady();
		}

		/// <summary>
		/// Sends a message to another device.
		/// </summary>
		/// <param name="to">The device ID to send the message to.</param>
		/// <param name="data">The data to send.</param>
		public void Message(int to, object data) {
			
			if (!IsAirConsoleUnityPluginReady()) {
				
				throw new NotReadyException();
				
			}
			
			JObject msg = new JObject();
			msg.Add("action", "message");
			msg.Add("from", to);
			msg.Add("data", JToken.FromObject(data));
			
			wsListener.Message(msg);
		}

		/// <summary>
		/// Sends a message to all devices.
		/// </summary>
		/// <param name="data">The message to send.</param>
		public void Broadcast(object data) {
			
			if (!IsAirConsoleUnityPluginReady()) {
				
				throw new NotReadyException();
				
			}
			
			JObject msg = new JObject();
			msg.Add("action", "broadcast");
			msg.Add("data", JToken.FromObject(data));
			
			wsListener.Message(msg);
		}

		/// <summary>
		/// Returns the device_id of this device.
		/// Every device in a AirConsole session has a device_id.
		/// The screen always has device_id 0. You can use the AirConsole.SCREEN
		/// constant instead of 0.
		/// All controllers also get a device_id. You can NOT assume that the device_ids
		/// of controllers are consecutive or that they start at 1.
		///
		/// DO NOT HARDCODE CONTROLLER DEVICE IDS!
		///
		/// If you want to have a logic with "players numbers" (Player 0, Player 1,
		/// Player 2, Player 3) use the setActivePlayers helper function! You can
		/// hardcode player numbers, but not device_ids.
		///
		/// Within an AirConsole session, devices keep the same device_id when they
		/// disconnect and reconnect. Different controllers will never get the same
		/// device_id in a session. Every device_id remains reserved for the device that
		/// originally got it.
		///
		/// For more info read
		/// http:// developers.airconsole.com/#/guides/device_ids_and_states
		/// </summary>
		/// <returns>The device identifier.</returns>
		public int GetDeviceId() {
			if (!IsAirConsoleUnityPluginReady()) {
				
				throw new NotReadyException();
				
			}
			return _device_id;
		}

		/// <summary>
		/// Takes all currently connected controllers and assigns them a player number.
		///  Can only be called by the screen. You don't have to use this helper
		/// function, but this mechanism is very convenient if you want to know which
		/// device is the first player, the second player, the third player ...
		/// The assigned player numbers always start with 0 and are consecutive.
		/// You can hardcode player numbers, but not device_ids.
		/// Once the screen has called setActivePlayers you can get the device_id of
		/// the first player by calling convertPlayerNumberToDeviceId(0), the device_id
		/// of the second player by calling convertPlayerNumberToDeviceId(1), ...
		/// You can also convert device_ids to player numbers by calling
		/// convertDeviceIdToPlayerNumber(device_id). You can get all device_ids that
		/// are active players by calling getActivePlayerDeviceIds().
		/// The screen can call this function every time a game round starts.
		/// </summary>
		/// <param name="data">The maximum number of controllers that should 
		/// get a player number assigned.</param>
		public void SetActivePlayers(int max_players=-1) {
			if (!IsAirConsoleUnityPluginReady()) {
				
				throw new NotReadyException();
				
			}

			List<int> device_ids = GetControllerDeviceIds ();
			_players.Clear ();
			if (max_players == -1) {
				max_players = device_ids.Count;
		    }
			for (int i = 0; i < device_ids.Count && i < max_players; ++i) {
				_players.Add(device_ids[i]);
			}
			JObject msg = new JObject();
			msg.Add("action", "setActivePlayers");
			msg.Add("max_players", max_players);
			
			wsListener.Message(msg);
	    }

		/// <summary>
		/// Returns an array of device_ids of the active players previously set by the
		/// screen by calling setActivePlayers. The first device_id in the array is the
		/// first player, the second device_id in the array is the second player, ...
		/// </summary>
		public ReadOnlyCollection<int> GetActivePlayerDeviceIds {
			get { return _players.AsReadOnly(); }
		}

		/// <summary>
		/// Returns the device_id of a player, if the player is part of the active
		/// players previously set by the screen by calling setActivePlayers. If fewer
		/// players are in the game than the passed in player_number or the active
		/// players have not been set by the screen, this function returns undefined.
		/// </summary>
		/// <param name="player_number">Player Number.</param>
		public int ConvertPlayerNumberToDeviceId(int player_number) {
			if (player_number >= 0 && player_number < _players.Count) {
				return _players[player_number];
		    }
			return -1;
	    }

		/// <summary>
		/// Returns the player number for a device_id, if the device_id is part of the
		/// active players previously set by the screen by calling setActivePlayers.
		/// Player numbers are zero based and are consecutive. If the device_id is not
		/// part of the active players, this function returns -1.
		/// </summary>
		/// <param name="device_id">Device id.</param>
		public int ConvertDeviceIdToPlayerNumber(int device_id) {
			return _players.IndexOf (device_id);
		}
		


		/// <summary>
		/// Returns the globally unique id of a device.
		/// </summary>
		/// <returns>The UID.</returns>
		/// <param name="device_id">The device id for which you want the uid. Default is this device.</param>
		public string GetUID(int device_id = -1) {
			if (!IsAirConsoleUnityPluginReady()) {
				
				throw new NotReadyException();
				
			}
			if (device_id == -1) {
				device_id = GetDeviceId();
			}
			return (string)GetDevice(GetDeviceId())["uid"];
			
		}
		
		/// <summary>
		/// Gets the custom DeviceState of a device.
		/// </summary>
		/// <param name="device_id">The device ID of which you want the custom state. Default is this device.</param>
		/// <returns> The custom data previously set by the device.</returns>
		public JToken GetCustomDeviceState(int device_id = -1) {
			if (!IsAirConsoleUnityPluginReady()) {
				
				throw new NotReadyException();
				
			}
			if (device_id == -1) {
				device_id = GetDeviceId();
			}
			if (GetDevice(device_id) != null) {
				
				try {
					return GetDevice(device_id)["custom"];
				}
				catch (Exception e) {
					
					if (Settings.debug.error) {
						Debug.LogError("AirConsole: " + e.Message);
					}
					return null;
				}
				
			} else {
				
				if (Settings.debug.warning) {
					Debug.LogWarning("AirConsole: GetCustomDeviceState: device_id " + device_id + " not found");
				}
				return null;
			}
		}
		
		/// <summary>
		/// Returns the nickname of a user.
		/// </summary>
		/// <param name="device_id">The device id for which you want the nickname. Default is this device. Screens don't have nicknames.</param>
		public string GetNickname(int device_id = -1) {
			
			if (!IsAirConsoleUnityPluginReady()) {
				
				throw new NotReadyException();
				
			}

			if (device_id == -1) {
				device_id = GetDeviceId();
			}

			if (GetDevice(device_id) != null) {
				
				try {
					if (GetDevice(device_id)["nickname"] != null) {
						return (string)GetDevice(device_id)["nickname"];
					} else {
						return "Guest " + device_id;
					}
				}
				catch (Exception) { 
					return "Guest " + device_id; 
				}
				
			} else {
				
				if (Settings.debug.warning) {
					Debug.LogWarning("AirConsole: GetNickname: device_id " + device_id + " not found");
				}
				return null;
			}
			
		}
		
		/// <summary>
		/// Returns the url to a profile picture of a user.
		/// </summary>
		/// <param name="device_id">The device id for which you want a profile picture. Defaults to this device. Screens don't have profile pictures.</param>
		/// <param name="size">The size of in pixels of the picture. Default is 64.</param>
		public string GetProfilePicture(int device_id = -1, int size = 64) {
			if (!IsAirConsoleUnityPluginReady()) {
				
				throw new NotReadyException();
				
			}
			if (device_id == -1) {
				device_id = GetDeviceId();
			}
			if (GetDevice(GetDeviceId()) != null) {
				
				try {
					return Settings.AIRCONSOLE_PROFILE_PICTURE_URL + (string)GetDevice(device_id)["uid"] + "&size=" + size;
				}
				catch (Exception) {
					
					if (Settings.debug.warning) {
						Debug.LogWarning("AirConsole: GetProfilePicture: can't find profile picture of device_id:" + device_id);
					}
					return null;
				}
				
			} else {
				
				if (Settings.debug.warning) {
					Debug.LogWarning("AirConsole: GetProfilePicture: " + device_id + " not found");
				}
				return null;
			}
			
		}
		
		/// <summary>
		/// Returns the current time on the game server. 
		/// This allows you to have a synchronized clock: You can send the servertime in a message to know exactly at what point something happened on a device. 
		/// </summary>
		/// <returns> Timestamp in milliseconds.</returns>
		public long GetServerTime() {
			
			if (!IsAirConsoleUnityPluginReady()) {
				
				throw new NotReadyException();
				
			}
			
			return (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds + _server_time_offset;
		}
		
		/// <summary>
		/// Request that all devices return to the AirConsole store.
		/// </summary>
		public void NavigateHome() {
			
			if (!IsAirConsoleUnityPluginReady()) {
				
				throw new NotReadyException();
				
			}
			
			JObject msg = new JObject();
			msg.Add("action", "navigateHome");
			
			wsListener.Message(msg);
		}
		
		/// <summary>
		/// Request that all devices load a game by url. Note that the custom DeviceStates are preserved. 
		/// If you don't want that, override SetCustomDeviceState(null) on every device before calling this function.
		/// </summary>
		public void NavigateTo(string url) {
			
			if (!IsAirConsoleUnityPluginReady()) {
				
				throw new NotReadyException();
				
			}
			
			JObject msg = new JObject();
			msg.Add("action", "navigateTo");
			msg.Add("data", url);
			
			wsListener.Message(msg);
		}
		
		/// <summary>
		/// Sets the custom DeviceState of this device.
		/// </summary>
		/// <param name="data">The custom data to set.</param>
		public void SetCustomDeviceState(object data) {
			
			if (!IsAirConsoleUnityPluginReady()) {
				
				throw new NotReadyException();
				
			}
			JObject msg = new JObject();
			msg.Add("action", "setCustomDeviceState");
			msg.Add("data", JToken.FromObject(data));

			AllocateDeviceSlots(0);
			if (GetDevice (0) == null) {
				_devices [0] = new JObject ();
			}
			_devices[0]["custom"] = msg["data"];
			
			wsListener.Message(msg);
		}
		
		/// <summary>
		/// Sets a property in the custom DeviceState of this device.
		/// </summary>
		/// <param name="data">The property name.</param>
		/// <param name="data">The property value.</param>
		public void SetCustomDeviceStateProperty(string key, object value) {
			
			if (!IsAirConsoleUnityPluginReady()) {
				
				throw new NotReadyException();
				
			}
			
			JObject msg = new JObject();
			msg.Add("action", "setCustomDeviceStateProperty");
			msg.Add("key", JToken.FromObject(key));
			msg.Add("value", JToken.FromObject(value));

			AllocateDeviceSlots(0);
			if (GetDevice(0) == null) {
				_devices[0] = new JObject();
			}
			
			JToken custom = _devices[0]["custom"];
			if (custom == null) {
				JObject new_custom = new JObject();
				_devices[0]["custom"] = JToken.FromObject(new_custom);
			}
			
			
			_devices[0]["custom"][key] = msg["value"];
			
			wsListener.Message(msg);
		}
		
		/// <summary>
		/// Shows or hides the default UI.
		/// </summary>
		/// <param name="visible">Whether to show or hide the default UI.</param>
		public void ShowDefaultUI(bool visible) {
			
			if (!IsAirConsoleUnityPluginReady()) {
				
				throw new NotReadyException();
				
			}
			
			JObject msg = new JObject();
			msg.Add("action", "showDefaultUI");
			msg.Add("data", visible);
			
			wsListener.Message(msg);
		}
		/// <summary>
		/// Returns the device ID of the master controller.
		/// </summary>
		public int GetMasterControllerDeviceId() {
			if (!IsAirConsoleUnityPluginReady()) {
				
				throw new NotReadyException();
				
			}
			List<int> result = GetControllerDeviceIds();
			if (result.Count > 0) {
				return result[0];
			}
			return 0;
		}
		
		/// <summary>
		/// Returns all controller device ids that have loaded your game.
		/// </summary>
		public List<int> GetControllerDeviceIds() {
			if (!IsAirConsoleUnityPluginReady()) {
				
				throw new NotReadyException();
				
			}
			List<int> result = new List<int> ();
			string game_url = GetGameUrl(_location);
			for (int i = 1; i < _devices.Count; ++i) {
				JToken device = GetDevice(i);
				if (device != null && GetGameUrl((string)device["location"]) == game_url) {
					result.Add(i);
				} 
			}
			return result;
		}


		/// <summary>
		/// Returns true if a user is logged in.
		/// </summary>
		public bool IsUserLoggedIn(int device_id = -1) {
				
			if (!IsAirConsoleUnityPluginReady ()) {
						
				throw new NotReadyException ();
						
			}
					
			if (device_id == -1) {
				device_id = GetDeviceId ();
			}
					
			if (GetDevice (device_id) != null) {
						
				try {
					if (GetDevice (device_id) ["auth"] != null) {
						return (bool)GetDevice (device_id) ["auth"];
					}
				} catch (Exception) { 
					return false;
				}
				return false;
			}

			return false;
		}


		/// <summary>
		/// Gets thrown when you call an API method before OnReady was called.
		/// </summary>
		public class NotReadyException: SystemException {
			public NotReadyException() : base() {}
		}


		#endregion

		#region airconsole unity config
		
		public StartMode browserStartMode;
		public UnityEngine.Object controllerHtml;
		public bool autoScaleCanvas = true;
		
		#endregion

        #region unity functions

        void Awake() {

            // unity singleton implementation
            if (_instance == null) {
                _instance = this;
                DontDestroyOnLoad(this);
            } else {
                if (this != _instance) {
                    Destroy(this.gameObject);
                }
            }

            // always set default object name 
            // important for unity webgl communication
            gameObject.name = "AirConsole";
        }

        void Start() {

            // application has to run in background
            Application.runInBackground = true;

            // register all incoming events
            wsListener = new WebsocketListener();
            wsListener.onReady += this.OnReady;
            wsListener.onClose += this.OnClose;
            wsListener.onMessage += this.OnMessage;
            wsListener.onDeviceStateChange += OnDeviceStateChange;
			wsListener.onConnect += OnConnect;
			wsListener.onDisconnect += OnDisconnect;
			wsListener.onCustomDeviceStateChange += OnCustomDeviceStateChange;
			wsListener.onDeviceProfileChange += OnDeviceProfileChange;

            // check if game is running in webgl build
            if (Application.platform != RuntimePlatform.WebGLPlayer) {

                // start websocket connection
                wsServer = new WebSocketServer(Settings.webSocketPort);
                wsServer.AddWebSocketService<WebsocketListener>(Settings.WEBSOCKET_PATH, () => wsListener);
                wsServer.Start();

                if (Settings.debug.info) {
                    Debug.Log("AirConsole: Dev-Server started!");
                }

            } else {

                // call external javascript init function
                Application.ExternalCall("onGameReady", this.autoScaleCanvas);
            }
            
        }

        void Update() {

            // dispatch event queue on main unity thread
            while (eventQueue.Count > 0) {
                eventQueue.Dequeue().Invoke();
            }
        }

        void OnApplicationQuit() {
            StopWebsocketServer();
        }

        void OnDisable() {
            StopWebsocketServer();
        }

        #endregion

        #region internal functions

		void OnDeviceStateChange(JObject msg) {
			
			if (msg["device_id"] == null) {
				return;
			}
			
			try {
				
				int deviceId = (int)msg["device_id"];
				AllocateDeviceSlots(deviceId);
				JToken deviceData = (JToken)msg["device_data"];
				if (deviceData != null && deviceData.HasValues) {
					_devices[deviceId] = deviceData;
				} else {
					_devices[deviceId] = null;
				}
				
				if (this.onDeviceStateChange != null) {
					eventQueue.Enqueue(() => this.onDeviceStateChange(deviceId, GetDevice(_device_id)));
				}
				
				if (Settings.debug.info) {
					Debug.Log("AirConsole: saved devicestate of " + deviceId);
				}
				
			} catch (Exception e){
				
				if (Settings.debug.error) {
					Debug.LogError(e.Message);
				}
			}
			
		}
		
		void OnConnect(JObject msg) {
			
			if (msg["device_id"] == null) {
				return;
			}
			
			try {
				
				int deviceId = (int)msg["device_id"];
				
				if (this.onConnect != null) {
					eventQueue.Enqueue(() => this.onConnect(deviceId));
				}
				
				if (Settings.debug.info) {
					Debug.Log("AirConsole: onConnect " + deviceId);
				}
				
			} catch (Exception e){
				
				if (Settings.debug.error) {
					Debug.LogError(e.Message);
				}
			}
			
		}
		
		
		void OnDisconnect(JObject msg) {
			
			if (msg["device_id"] == null) {
				return;
			}
			
			try {
				
				int deviceId = (int)msg["device_id"];
				
				if (this.onDisconnect != null) {
					eventQueue.Enqueue(() => this.onDisconnect(deviceId));
				}
				
				if (Settings.debug.info) {
					Debug.Log("AirConsole: onDisconnect " + deviceId);
				}
				
			} catch (Exception e){
				if (Settings.debug.error) {
					Debug.LogError(e.Message);
				}
			}
			
		}
		
		
		void OnCustomDeviceStateChange(JObject msg) {
			
			if (msg["device_id"] == null) {
				return;
			}
			
			try {
				
				int deviceId = (int)msg["device_id"];
				
				if (this.onCustomDeviceStateChange != null) {
					eventQueue.Enqueue(() => this.onCustomDeviceStateChange(deviceId, GetCustomDeviceState(deviceId)));
				}
				
				if (Settings.debug.info) {
					Debug.Log("AirConsole: onCustomDeviceStateChange " + deviceId);
				}
				
			} catch (Exception e){
				
				if (Settings.debug.error) {
					Debug.LogError(e.Message);
				}
			}
			
		}
		
		void OnMessage(JObject msg) {
			
			if (this.onMessage != null) {
				eventQueue.Enqueue(() => this.onMessage((int)msg["from"], (JToken)msg["data"]));
			}
		}
		
		
		void OnReady(JObject msg) {
			
			// parse server_time_offset
			_server_time_offset = (int)msg["server_time_offset"];
			
			// parse device_id
			_device_id = (int)msg["device_id"];
			
			// parse location
			_location = (string)msg["location"];
			
			// load devices
			_devices.Clear();
			foreach (JToken data in (JToken)msg["devices"]) {
				JToken assign = data;
				if (data != null && !data.HasValues) {
					assign = null;
				}
				_devices.Add(assign);
			}
			
			if (this.onReady != null) {
				eventQueue.Enqueue(() => this.onReady((string)msg["code"]));
			}
		}

		void OnDeviceProfileChange(JObject msg) {
			
			if (msg["device_id"] == null) {
				return;
			}
			
			try {
				
				int deviceId = (int)msg["device_id"];
				
				if (this.onDeviceProfileChange != null) {
					eventQueue.Enqueue(() => this.onDeviceProfileChange(deviceId));
				}
				
				if (Settings.debug.info) {
					Debug.Log("AirConsole: onDeviceProfileChange " + deviceId);
				}
				
			} catch (Exception e){
				
				if (Settings.debug.error) {
					Debug.LogError(e.Message);
				}
			}
		}

		[Obsolete("Please use GetServerTime(). This method will be removed in the next version.")]
		public int server_time_offset {
			get { return _server_time_offset; }
		}
		
		[Obsolete("device_id is deprecated, please use GetDeviceId instead. This method will be " +
		          "removed in the next version.")]
		public int device_id {
			get { return GetDeviceId(); }
		}
		
		[Obsolete("Do not use .devices directly. Use the getter and setter functions. Devices in " +
		          "this collection may not have loaded your game yet. This method will be removed in" +
		          "the next version.")]
		public ReadOnlyCollection<JToken> devices {
			get { 
				return _devices.AsReadOnly(); 
			}
		}
		
		// private vars
		private WebSocketServer wsServer;
		private WebsocketListener wsListener;
		private List<JToken> _devices = new List<JToken>();
		private int _device_id;
		private int _server_time_offset;
		private string _location;
		private List<int> _players = new List<int>();
		private readonly Queue<Action> eventQueue = new Queue<Action>();
		
		// unity singleton handling
		private static AirConsole _instance;

        private void StopWebsocketServer() {
            if (wsServer != null) {
                wsServer.Stop();
            }
        }

        private void OnClose() {
            _devices.Clear();
        }

        public static string GetUrl(StartMode mode) {

            switch (mode) {
                case StartMode.VirtualControllers:
                    return Settings.AIRCONSOLE_NORMAL_URL;
                case StartMode.Normal:
                    return Settings.AIRCONSOLE_URL;
                default:
                    return "";
            }
        }

        public void ProcessJS(string data) {
            wsListener.ProcessMessage(data);
        }

		private JToken GetDevice(int deviceId) {
			if (deviceId < _devices.Count && deviceId >= 0) {
				return _devices[deviceId];
			}
			return null;
		}

		private string GetGameUrl(string url) {
			if (url == null) {
				return null;
			}
			url = url.Split ('#') [0];
			url = url.Split ('?') [0];
			if (url.EndsWith ("screen.html")) {
				url = url.Substring(0, url.Length - 11);
			} else if (url.EndsWith ("controller.html")) {
				url = url.Substring(0, url.Length - 15);
			}
			if (url.StartsWith ("https://")) {
				url = "http://" + url.Substring(8);
			}
			return url;
		}

		private void AllocateDeviceSlots(int i) {
			while (i >= _devices.Count) {
				_devices.Add(null);
			}
		}

        #endregion
    }
}


