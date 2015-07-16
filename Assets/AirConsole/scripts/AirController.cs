using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using WebSocketSharp;
using WebSocketSharp.Server;
using Newtonsoft.Json.Linq;


namespace AirConsole {

    [Serializable]
    public class DebugLevel {
        public bool info = true;
        public bool warning = true;
        public bool error = true;
    }

    [Serializable]
    public class AdvancedSettings {

        public int webServerPort = 50003;
        [HideInInspector]
        public int webSocketPort = 7843;
        [HideInInspector]
        public string webSocketPath = "/api";
        public int maxConnections = 20;
    }

    public enum StartMode {
        Debug,
        VirtualControllers,
        Normal,
        NoBrowserStart
    }
   
    public class AirController : MonoBehaviour {

        public const string VERSION = "0.1";
        public const string AIRCONSOLE_URL = "http://airconsole.com/#";
        public const string AIRCONSOLE_NORMAL_URL = "http://airconsole.com/developers/#";
        public const string AIRCONSOLE_DEBUG_URL = "http://www.airconsole.com/developers/#debug:";
        public const string WEBTEMPLATE_PATH = "/WebGLTemplates/AirConsole";
        

        public StartMode browserStartMode;
        public AdvancedSettings settings;
        public DebugLevel debug;

        public event OnReady onReady;
        public event OnMessage onMessage;

        private WebSocketServer wssv;
        private AirServer screen;

        JToken[] devices;

        private readonly Queue<Action> executeOnMainThread = new Queue<Action>();

        void Start() {

            Application.runInBackground = true;

            devices = new JToken[this.settings.maxConnections];

            screen = new AirServer(this.debug);
            screen.onReady += this.OnReady;
            screen.onClose += this.OnClose;
            screen.onMessage += this.OnMessage;
            screen.onDeviceStateChange += OnDeviceStateChange;

            if (Application.platform != RuntimePlatform.WebGLPlayer) {

                // start local webserver
                AirWebserver ws = new AirWebserver(
                    this.settings.webServerPort, 
                    this.debug, this.browserStartMode, 
                    Application.dataPath+AirController.WEBTEMPLATE_PATH
                );
                
                ws.Start();

                // start websocket connection
                wssv = new WebSocketServer(this.settings.webSocketPort);

                if (this.settings.webSocketPath == "") {
                    this.settings.webSocketPath = "/";
                }

                wssv.AddWebSocketService<AirServer>(this.settings.webSocketPath, () => screen);

                wssv.Start();

                if (this.debug.info) {
                    Debug.Log("AirConsole: Dev-Server started!");
                }

            } else {

                Application.ExternalCall("onGameReady");
            }
            
        }

        void Update() {

            // dispatch stuff on main thread
            while (executeOnMainThread.Count > 0) {
                executeOnMainThread.Dequeue().Invoke();
            }
        }

        void OnReady() {

            if (this.onReady != null) {
                executeOnMainThread.Enqueue(() => this.onReady());
            }

            if (Application.platform != RuntimePlatform.WebGLPlayer) {

                if (devices[0] != null) {
                    this.SetCustomDeviceState(devices[0]);
                }
            }
        }

        void OnClose() {

            // delete all controller device states
            for (int i = 1; i < devices.Length; i++) {
                devices[i] = null;
            }
        }

        void OnMessage(JObject msg) {

            if (this.onMessage != null) {
                executeOnMainThread.Enqueue(() => this.onMessage(msg));
            }
        }

        void OnDeviceStateChange(JObject msg) {
            
            try {

                int deviceId = (int)msg["device_id"];
                devices[deviceId] = msg["device_data"];

                if (this.debug.info) {
                    Debug.Log("AirConsole: saved devicestate of " + deviceId);
                }

            } catch (Exception e){

                if (this.debug.error) {
                    Debug.LogError(e.Message);
                }
            }

        }

        void OnApplicationQuit() {

            if (wssv != null) {
                wssv.Stop();
            }
        }

        void OnDisable() {

            if (wssv != null) {
                wssv.Stop();
            }
        }

        /*
        void OnLevelWasLoaded() {
            this.onReady = null;
            this.onMessage = null;
        }
        */

        public bool IsReady() {
            return screen.IsReady();
        }

        public void Message(int to, object data) {

            if (!screen.IsReady()) {

                if (this.debug.warning) {
                    Debug.LogWarning("AirConsole: Can't send message. AirConsole is not ready yet!");
                }
  
                return;
            }

            JObject msg = new JObject();
            msg.Add("action", "message");
            msg.Add("from", to);
            msg.Add("data", JToken.FromObject(data));

            screen.Message(msg);
        }

        public void Broadcast(object data) {

            if (!screen.IsReady()) {

                if (this.debug.warning) {
                    Debug.LogWarning("AirConsole: Can't broadcast message. AirConsole is not yet ready!");
                }

                return;
            }

       
            JObject msg = new JObject();
            msg.Add("action", "broadcast");
            msg.Add("data", JToken.FromObject(data));

            screen.Message(msg);
        }

        public void SetCustomDeviceState(object data) {

            if (!screen.IsReady()) {

                if (this.debug.warning) {
                    Debug.LogWarning("AirConsole: Can't set custom device state. AirConsole is not yet ready!");
                }

                return;
            }

            JObject msg = new JObject();
            msg.Add("action", "setCustomDeviceState");
            msg.Add("data", JToken.FromObject(data));
            
            if (GetDevice(0) == null) {
                devices[0] = new JObject();
            }

            devices[0]["custom"] = msg["data"];

            screen.Message(msg);
        }

        public JToken GetCustomDeviceState(int deviceId) {

            if (GetDevice(deviceId) != null) {

                try {
                    return GetDevice(deviceId)["custom"];
                }
                catch (Exception e) {

                    if (this.debug.error) {
                        Debug.LogError("AirConsole: "+e.Message);
                    }
           
                    return null;
                }

            } else {

                if (this.debug.warning) {
                    Debug.LogWarning("AirConsole: GetCustomDeviceState: device_id not found");
                }

                return null;
            } 
        }

        public JToken GetDevice(int deviceId) {

            if (devices[deviceId] != null) {
                return devices[deviceId];
            } else {
                return null;
            }
        }

        public JToken[] GetDevices() {
            return devices;
        }

        public long GetServerTime() {

            if (!screen.IsReady()) {

                if (this.debug.warning) {
                    Debug.LogWarning("AirConsole: Can't get server time. AirConsole is not ready yet!");
                }

                return 0;
            }

            return (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds + screen.GetServerTimeOffset();
        }

        public int GetServerTimeOffset() {

            if (!screen.IsReady()) {

                if (this.debug.warning) {
                    Debug.LogWarning("AirConsole: Can't get server time. AirConsole is not ready yet!");
                }

                return 0;
            }

            return screen.GetServerTimeOffset();
        }

        public int GetConnectedDevices() {

            int counter = 0;

            // int i = 1 to ignore the screen
            for (int i = 1; i < devices.Length; i++) {

                if (devices[i] != null) {
                    counter++;
                }
            }
            
            return counter;
        }

        public void ProcessJS(string data) {
            screen.ProcessMessage(data);
        }
    }
}


