using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;
using System;
using WebSocketSharp;
using WebSocketSharp.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AirConsole {

    public delegate void OnReady();
    public delegate void OnMessage(JObject msg);
    public delegate void onDeviceStateChange(JObject msg);

    public class AirServer : WebSocketBehavior {

        public event OnReady onReady;
        public event OnMessage onMessage;
        public event onDeviceStateChange onDeviceStateChange;

        private bool debug = true;
        private bool isReady;
        private int serverTimeOffset;

        public AirServer(bool pDebug) {

            base.IgnoreExtensions = true;

            this.debug = pDebug;
        }

        protected override void OnMessage(MessageEventArgs e) {

            this.ProcessMessage(e.Data);
        }

        protected override void OnOpen() {
            base.OnOpen();

            Send(@"{ ""action"": ""debug"", ""data"": ""welcome screen.html!"" }");

            if (!debug) {
                return;
            }

            Debug.Log("AirConsole: screen.html connected!");
        }

        protected override void OnClose(CloseEventArgs e) {

            Debug.Log("AirConsole: screen.html disconnected");
            base.OnClose(e);
        }

        protected override void OnError(ErrorEventArgs e) {

            base.OnError(e);


            if (!debug) {
                return;
            }

            Debug.Log(e.Message);
            Debug.Log(e.Exception);
        }

        public void ProcessMessage(string data) {

            try {

                JObject msg = JObject.Parse(data);

                if ((string)msg["action"] == "onReady") {

                    this.isReady = true;

                    this.serverTimeOffset = (int)msg["server_time_offset"];

                    if (this.onReady != null) {
                        this.onReady();
                    }

                    if (debug) {
                        Debug.Log("AirConsole: Connections are ready!");
                    }
                }


                if ((string)msg["action"] == "onMessage") {

                    if (this.onMessage != null) {
                        this.onMessage(msg);
                    }
                }

                if ((string)msg["action"] == "onDeviceStateChange") {

                    if (msg["device_id"] != null) {

                        if (this.onDeviceStateChange != null) {
                            this.onDeviceStateChange(msg);
                        }
                    }

                }

            }

            catch (Exception e) {

                if (debug) {
                    Debug.LogWarning(e.Message);
                    Debug.LogWarning(e.StackTrace);
                }
            }


        }

        public bool IsReady() {
            return isReady;
        }

        public void Message(Dictionary<string, object> data) {

            if (Application.platform == RuntimePlatform.WebGLPlayer) {

                if ((string)data["action"] == "message") {
                    Application.ExternalCall("window.app.airconsole.message", data["from"], data["data"]);
                }

                if ((string)data["action"] == "broadcast") {
                    Application.ExternalCall("window.app.airconsole.broadcast", data["data"]);
                }
                
                //Application.ExternalCall("processUnityData", JsonConvert.SerializeObject(data));

            } else {

                Send(JsonConvert.SerializeObject(data, new JsonSerializerSettings() {
                    NullValueHandling = NullValueHandling.Ignore
                }));
            }
        }

        public int GetServerTimeOffset() {
            return this.serverTimeOffset;
        }
    }

}
