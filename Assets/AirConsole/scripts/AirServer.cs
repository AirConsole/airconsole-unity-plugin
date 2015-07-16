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
    public delegate void OnClose();
    public delegate void OnMessage(JObject msg);
    public delegate void onDeviceStateChange(JObject msg);

    public class AirServer : WebSocketBehavior {

        public event OnReady onReady;
        public event OnClose onClose;
        public event OnMessage onMessage;
        public event onDeviceStateChange onDeviceStateChange;

        private DebugLevel debug;
        private bool isReady;
        private int serverTimeOffset;

        public AirServer(DebugLevel debug) {

            base.IgnoreExtensions = true;

            this.debug = debug;
        }

        protected override void OnMessage(MessageEventArgs e) {

            this.ProcessMessage(e.Data);
        }

        protected override void OnOpen() {
            base.OnOpen();

            Send(@"{ ""action"": ""debug"", ""data"": ""welcome screen.html!"" }");

            if (this.debug.info) {
                Debug.Log("AirConsole: screen.html connected!");
            }

            
        }

        protected override void OnClose(CloseEventArgs e) {

            this.isReady = false;

            if (this.onClose != null) {
                this.onClose();
            }

            if (this.debug.info) {
                Debug.Log("AirConsole: screen.html disconnected");
            }
           
            base.OnClose(e);
        }

        protected override void OnError(ErrorEventArgs e) {

            base.OnError(e);


            if (this.debug.error) {
                Debug.LogError("AirConsole: "+e.Message);
                Debug.LogError("AirConsole: "+e.Exception);
            }
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

                    if (this.debug.info) {
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

                if (this.debug.error) {
                    Debug.LogError(e.Message);
                    Debug.LogError(e.StackTrace);
                }
            }
        }

        public bool IsReady() {
            return isReady;
        }

        public void Message(JObject data) {

            if (Application.platform == RuntimePlatform.WebGLPlayer) {
                Application.ExternalCall("window.app.processUnityData", data.ToString());

            } else {
                Send(data.ToString());
            }
        }

        public int GetServerTimeOffset() {
            return this.serverTimeOffset;
        }
    }

}
