using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using WebSocketSharp;
using WebSocketSharp.Server;
using Newtonsoft.Json.Linq;


namespace AirConsole {

   
    public class AirController : MonoBehaviour {

        public int maxConnections = 20;
        public int port = 7843;
        public string path = "/api";
        public bool runInBackground = true;
        public bool debug = true;

        public event OnReady onReady;
        public event OnMessage onMessage;

        private WebSocketServer wssv;
        private AirServer screen;

        JToken[] devices;

        private readonly Queue<Action> executeOnMainThread = new Queue<Action>();

        void Start() {

            if (this.runInBackground) {
                Application.runInBackground = true;
            }

            this.devices = new JToken[maxConnections];

            screen = new AirServer(debug);
            screen.onReady += this.OnReady;
            screen.onMessage += this.OnMessage;
            screen.onDeviceStateChange += this.OnDeviceStateChange;

            if (Application.platform != RuntimePlatform.WebGLPlayer) {

                Application.runInBackground = true;

                wssv = new WebSocketServer(port);

                if (path == "") {
                    path = "/";
                }

                wssv.AddWebSocketService<AirServer>(path, () => screen);

                wssv.Start();

                if (debug) {
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
        }

        void OnMessage(JObject msg) {

            if (this.onMessage != null) {
                executeOnMainThread.Enqueue(() => this.onMessage(msg));
            }
        }

        void OnDeviceStateChange(JObject msg) {
            
            try {

                int deviceId = (int)msg["device_id"];
                Debug.Log("saved devicestate of " + deviceId);
                this.devices[deviceId] = msg["device_data"];
 


            } catch (Exception e){
                Debug.LogError(e.Message);
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

                if (debug) {
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

                if (debug) {
                    Debug.LogWarning("AirConsole is not yet ready!");
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

                if (debug) {
                    Debug.LogWarning("AirConsole is not yet ready!");
                }

                return;
            }

            JObject msg = new JObject();
            msg.Add("action", "setCustomDeviceState");
            msg.Add("data", JToken.FromObject(data));
            
            if (this.GetDevice(0) == null) {
                this.devices[0] = new JObject();
            }

            this.devices[0]["custom"] = msg["data"];

            screen.Message(msg);
        }


        public JToken GetCustomDeviceState(int deviceId) {

            if (this.GetDevice(deviceId) != null) {

                try {
                    return this.GetDevice(deviceId)["custom"];
                }
                catch (Exception e) {

                    if (debug) {
                        Debug.LogWarning(e.Message);
                    }
           
                    return null;
                }

            } else {

                Debug.LogWarning("GetCustomDeviceState: device_id not found");
                return null;
            } 
        }

        public JToken GetDevice(int deviceId) {

            if (this.devices[deviceId] != null) {
                return this.devices[deviceId];
            } else {
                return null;
            }
        }

        public JToken[] GetDevices() {
            return this.devices;
        }

        public long GetServerTime() {

            if (!screen.IsReady()) {

                if (debug) {
                    Debug.LogWarning("AirConsole: Can't get server time. AirConsole is not ready yet!");
                }

                return 0;
            }

            return (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds + screen.GetServerTimeOffset();
        }

        public int GetServerTimeOffset() {

            if (!screen.IsReady()) {

                if (debug) {
                    Debug.LogWarning("AirConsole: Can't get server time. AirConsole is not ready yet!");
                }

                return 0;
            }

            return this.screen.GetServerTimeOffset();
        }

        public int GetConnectedDevices() {

            int counter = 0;

            for (int i = 0; i < this.devices.Length; i++) {

                if (this.devices[i] != null) {
                    counter++;
                }
            }

            if (counter > 0) {
                return counter - 1;
            } else {
                return counter;
            }
        }

        public void ProcessJS(string data) {
            screen.ProcessMessage(data);
        }
    }
}


