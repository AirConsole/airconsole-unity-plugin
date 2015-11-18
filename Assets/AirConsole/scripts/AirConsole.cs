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
        Debug,
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
   
    public class AirConsole : MonoBehaviour {

		public class NotReadyException: SystemException {
			public NotReadyException() : base() {}
		}

        // inspector vars
        public StartMode browserStartMode;
        public UnityEngine.Object controllerHtml;
        public bool autoScaleCanvas = true;

        // public events
		
		/// <summary>
		/// Gets called when the game console is ready.
		/// This event also also fires onConnect for all devices that already are
		/// connected and have loaded your game.
		/// This event also fires onCustomDeviceStateChange for all devices that are
		/// connected, have loaded your game and have set a custom Device State.
		/// </summary>
		/// <param name="code">The AirConsole join code.</param> 
        public event OnReady onReady;

		/// <summary>
		/// Gets called when a message is received from another device
		/// that called message() or broadcast().
		/// If you dont want to parse messages yourself and prefer an event driven
		/// approach, have a look at http://github.com/AirConsole/airconsole-events/
		/// </summary>
		/// <param name="from">The device ID that sent the message.</param> 
		/// <param name="data">The data that was sent.</param>
        public event OnMessage onMessage;

		/// <summary>
		/// Gets called when a device joins/leaves a game session or updates its DeviceState (custom DeviceState, profile pic, nickname, slow connection). 
		/// This is function is also called every time onConnect, onDisconnect or onCustomDeviceStateChange is called. It's like their root function.
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
		/// Gets called when a device updates it's custom DeviceState by calling setCustomDeviceState or setCustomDeviceStateProperty. 
		/// Make sure you understand the power of device states: http://developers.airconsole.com/#/guides/device_ids_and_states
		/// </summary>
		/// <param name="device_id">the device ID that changed its customDeviceState.</param> 
		/// <param name="cutsom_data">The custom DeviceState data value.</param>
		public event OnCustomDeviceStateChange onCustomDeviceStateChange;

        // public vars (readonly)
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
            get { return _devices.Values.ToList<JToken>().AsReadOnly(); }
        }

        // private vars
        private WebSocketServer wsServer;
        private WebsocketListener wsListener;
        private Dictionary<int, JToken> _devices = new Dictionary<int, JToken>();
        private int _device_id;
        private int _server_time_offset;
		private string _location;
        private readonly Queue<Action> eventQueue = new Queue<Action>();

        // unity singleton handling
        private static AirConsole _instance;
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

        private void StopWebsocketServer() {
            if (wsServer != null) {
                wsServer.Stop();
            }
        }

        private bool IsReady() {
            return wsListener.IsReady();
        }

        private void OnClose() {

            // delete all devices
            _devices.Clear();

            /*
            for (int i = 1; i < _devices.Count; i++) {
                _devices[i] = null;
            }*/
        }

        public static string GetUrl(StartMode mode) {

            switch (mode) {
                case StartMode.VirtualControllers:
                    return Settings.AIRCONSOLE_NORMAL_URL;
                case StartMode.Debug:
                    return Settings.AIRCONSOLE_DEBUG_URL;
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

            if (_devices.ContainsKey(deviceId)) {
                return _devices[deviceId];
            } else {
                return null;
            }
        }

		private string GetGameUrl(string url) {
			if (url == null) {
				return null;
			}
			url = url.Replace ("screen.html", "");
			url = url.Replace ("controller.html", "");
			return url;
		}

        #endregion

        #region airconsole api

		/// <summary>
		/// Sends a message to all devices.
		/// </summary>
		/// <param name="data">The message to send.</param>
        public void Broadcast(object data) {

            if (!IsReady()) {

                throw new NotReadyException();
                
            }

            JObject msg = new JObject();
            msg.Add("action", "broadcast");
            msg.Add("data", JToken.FromObject(data));

            wsListener.Message(msg);
        }

		/// <summary>
		/// Gets the custom DeviceState of a device. object.
		/// </summary>
		/// <param name="device_id">The device ID of which you want the custom state. Default is this device.</param>
		/// <returns> The custom data previously set by the device.</returns>
		public JToken GetCustomDeviceState() {
			return GetCustomDeviceState(GetDeviceId());
	    }

		/// <summary>
		/// Gets the custom DeviceState of a device. object.
		/// </summary>
		/// <param name="device_id">The device ID of which you want the custom state. Default is this device.</param>
		/// <returns> The custom data previously set by the device.</returns>
        public JToken GetCustomDeviceState(int device_id) {
			if (!IsReady()) {
				
				throw new NotReadyException();
				
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
		/// Returns the nickname of the user.
		/// </summary>
		/// <param name="device_id">The device id for which you want the nickname. Default is this device. Screens don't have nicknames.</param>
		public string GetNickname() {
			return GetNickname (GetDeviceId());
		}

		/// <summary>
		/// Returns the nickname of the user.
		/// </summary>
		/// <param name="device_id">The device id for which you want the nickname. Default is this device. Screens don't have nicknames.</param>
        public string GetNickname(int device_id) {

			if (!IsReady()) {
				
				throw new NotReadyException();
				
			}

			if (GetDevice(device_id) != null) {

                try {
                    if (GetDevice(device_id)["nickname"] != null) {
                        return (string)GetDevice(device_id)["nickname"];
                    } else {
                        return "Player " + device_id;
                    }
                }
                catch (Exception) { 
                    return "Player " + device_id; 
                }

            } else {

                if (Settings.debug.warning) {
                    Debug.LogWarning("AirConsole: GetNickname: device_id " + device_id + " not found");
                }
                return null;
            }

        }

		/// <summary>
		/// Returns the url to a profile picture of the user.
		/// </summary>
		/// <param name="device_id">The device id for which you want a profile picture. Default is this device. Screens don't have profile pictures.</param>
		/// <param name="size">The size of in pixels of the picture. Default is 64.</param>
		public string GetProfilePicture() {
			return GetProfilePicture (GetDeviceId());
		}

		/// <summary>
		/// Returns the url to a profile picture of the user.
		/// </summary>
		/// <param name="device_id">The device id for which you want a profile picture. Default is this device. Screens don't have profile pictures.</param>
		/// <param name="size">The size of in pixels of the picture. Default is 64.</param>
        public string GetProfilePicture(int device_id, int size = 64) {
			if (!IsReady()) {
				
				throw new NotReadyException();
				
			}
			if (GetDevice(GetDeviceId()) != null) {

                try {
                    return Settings.AIRCONSOLE_PROFILE_PICTURE_URL + (string)GetDevice(device_id)["uid"] + "&size=" + size;
                }
                catch (Exception) {

                    if (Settings.debug.warning) {
                        Debug.LogWarning("AirConsole: GetProfilePicture: can't find uid of device_id:" + device_id);
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

		public string GetUID() {
			if (!IsReady()) {
				
				throw new NotReadyException();
				
			}
			return (string)GetDevice(GetDeviceId())["uid"];

		}

		/// <summary>
		/// Returns the current time of the game server. 
		/// This allows you to have a synchronized clock: You can send the servertime in a message to know exactly at what point something happened on adevice. 
		/// This function can only be called if the AirConsole was instantiatedwith the "synchronize_time" opts set to true and after onReady was called.
		/// </summary>
		/// <returns> Timestamp in milliseconds.</returns>
        public long GetServerTime() {

			if (!IsReady()) {
				
				throw new NotReadyException();
				
			}
			
			return (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds + _server_time_offset;
        }

		public int GetDeviceId() {
			if (!IsReady()) {
				
				throw new NotReadyException();
				
			}
			return _device_id;
		}

		/// <summary>
		/// Sends a message to another device.
		/// </summary>
		/// <param name="to">The device ID to send the message to.</param>
		/// <param name="data">The data to send.</param>
        public void Message(int to, object data) {

			if (!IsReady()) {
				
				throw new NotReadyException();
				
			}
			
			JObject msg = new JObject();
			msg.Add("action", "message");
            msg.Add("from", to);
            msg.Add("data", JToken.FromObject(data));

            wsListener.Message(msg);
        }

		/// <summary>
		/// Request that all devices return to the AirConsole store.
		/// </summary>
        public void NavigateHome() {

			if (!IsReady()) {
				
				throw new NotReadyException();
				
			}
			
			JObject msg = new JObject();
			msg.Add("action", "navigateHome");

            wsListener.Message(msg);
        }

		/// <summary>
		/// Request that all devices load a game by url. Note that the custom DeviceStates are preserved. 
		/// If you don't want thatoverride setCustomDeviceState(undefined) on every device before calling this function.
		/// </summary>
		public void NavigateTo(string url) {
			
			if (!IsReady()) {
				
				throw new NotReadyException();
				
			}
			
			JObject msg = new JObject();
			msg.Add("action", "navigateTo");
			msg.Add("data", url);
			
			wsListener.Message(msg);
		}
 

        void OnDeviceStateChange(JObject msg) {

            if (msg["device_id"] == null) {
                return;
            }
            
            try {

                int deviceId = (int)msg["device_id"];
                _devices[deviceId] =  (JToken)msg["device_data"];
     
                if (this.onDeviceStateChange != null) {
                    eventQueue.Enqueue(() => this.onDeviceStateChange(deviceId, _devices[deviceId]));
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
            int deviceId = 0;

            foreach (JToken key in (JToken)msg["devices"]) {
                _devices[deviceId] = key;
                deviceId++;
            }

            if (this.onReady != null) {
                eventQueue.Enqueue(() => this.onReady((string)msg["code"]));
            }
        }

		/// <summary>
		/// Sets the custom DeviceState of this device.
		/// </summary>
		/// <param name="data">The custom data to set.</param>
        public void SetCustomDeviceState(object data) {

			if (!IsReady()) {
				
				throw new NotReadyException();
				
			}
			
			JObject msg = new JObject();
			msg.Add("action", "setCustomDeviceState");
            msg.Add("data", JToken.FromObject(data));
            
            if (GetDevice(0) == null) {
                _devices[0] = new JObject();
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
			
			if (!IsReady()) {
				
				throw new NotReadyException();
				
			}
			
			JObject msg = new JObject();
			msg.Add("action", "setCustomDeviceStateProperty");
			msg.Add("key", JToken.FromObject(key));
			msg.Add("value", JToken.FromObject(value));
			
			if (GetDevice(0) == null) {
				_devices[0] = new JObject();
			}

			JToken custom = _devices [0]["custom"];
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

			if (!IsReady()) {
				
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
			if (!IsReady()) {
				
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
			if (!IsReady()) {
				
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

        #endregion
    }
}


