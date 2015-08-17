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
   
    public class AirConsole : MonoBehaviour {

        // inspector vars
        public StartMode browserStartMode;
        public UnityEngine.Object controllerHtml;
        public bool autoScaleCanvas = true;

        // public events
        public event OnReady onReady;
        public event OnMessage onMessage;
        public event OnDeviceStateChange onDeviceStateChange;

        // public vars (readonly)
        public int server_time_offset {
            get { return _server_time_offset; }
        }

        public int device_id {
            get { return _device_id; }
        }

        public ReadOnlyCollection<JToken> devices {
            get { return _devices.Values.ToList<JToken>().AsReadOnly(); }
        }

        // private vars
        private WebSocketServer wsServer;
        private WebsocketListener wsListener;
        private Dictionary<int, JToken> _devices = new Dictionary<int, JToken>();
        private int _device_id;
        private int _server_time_offset;
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

        #endregion

        #region airconsole api

        public void Broadcast(object data) {

            if (!IsReady()) {

                if (Settings.debug.warning) {
                    Debug.LogWarning("AirConsole: Can't broadcast message. AirConsole is not yet ready!");
                }
                return;
            }


            JObject msg = new JObject();
            msg.Add("action", "broadcast");
            msg.Add("data", JToken.FromObject(data));

            wsListener.Message(msg);
        }

        public JToken GetCustomDeviceState(int device_id) {

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

        public string GetNickname(int device_id) {

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

        public string GetProfilePicture(int device_id, int size = 64) {

            if (GetDevice(device_id) != null) {

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

        public long GetServerTime() {

            if (!IsReady()) {

                if (Settings.debug.warning) {
                    Debug.LogWarning("AirConsole: Can't get server time. AirConsole is not ready yet!");
                }
                return 0;
            }

            return (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds + _server_time_offset;
        }

        public void LoadScript(string src) {

            if (!IsReady()) {

                if (Settings.debug.warning) {
                    Debug.LogWarning("AirConsole: Can't load script. AirConsole is not yet ready!");
                }
                return;
            }


            JObject msg = new JObject();
            msg.Add("action", "loadScript");
            msg.Add("data", src);

            wsListener.Message(msg);
        }

        public void Message(int to, object data) {

            if (!IsReady()) {

                if (Settings.debug.warning) {
                    Debug.LogWarning("AirConsole: Can't send message. AirConsole is not ready yet!");
                }
                return;
            }

            JObject msg = new JObject();
            msg.Add("action", "message");
            msg.Add("from", to);
            msg.Add("data", JToken.FromObject(data));

            wsListener.Message(msg);
        }

        public void NavigateHome() {

            if (!IsReady()) {

                if (Settings.debug.warning) {
                    Debug.LogWarning("AirConsole: Can't navigate home. AirConsole is not yet ready!");
                }
                return;
            }

            JObject msg = new JObject();
            msg.Add("action", "navigateHome");

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

        public void SetCustomDeviceState(object data) {

            if (!IsReady()) {

                if (Settings.debug.warning) {
                    Debug.LogWarning("AirConsole: Can't set custom device state. AirConsole is not yet ready!");
                }
                return;
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

        public void ShowDefaultUI(bool visible) {

            if (!IsReady()) {

                if (Settings.debug.warning) {
                    Debug.LogWarning("AirConsole: Can't navigate home. AirConsole is not yet ready!");
                }
                return;
            }

            JObject msg = new JObject();
            msg.Add("action", "showDefaultUI");
            msg.Add("data", visible);

            wsListener.Message(msg);
        }

        #endregion
    }
}


