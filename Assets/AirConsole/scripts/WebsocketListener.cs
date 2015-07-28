using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;
using System;
using WebSocketSharp;
using WebSocketSharp.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NDream.AirConsole {

    // event delegates
    public delegate void OnReadyInternal(JObject data);
    public delegate void OnMessageInternal(JObject data);
    public delegate void OnDeviceStateChangeInternal(JObject data);
    public delegate void OnCloseInternal();

    public class WebsocketListener : WebSocketBehavior {

        // events
        public event OnReadyInternal onReady;
        public event OnCloseInternal onClose;
        public event OnMessageInternal onMessage;
        public event OnDeviceStateChangeInternal onDeviceStateChange;

        // private vars
        private bool isReady;

        public WebsocketListener() {
            base.IgnoreExtensions = true;
        }

        protected override void OnMessage(MessageEventArgs e) {
            this.ProcessMessage(e.Data);
        }

        protected override void OnOpen() {
            base.OnOpen();

            // send welcome debug message to screen.html
            Send(@"{ ""action"": ""debug"", ""data"": ""welcome screen.html!"" }");

            if (Settings.debug.info) {
                Debug.Log("AirConsole: screen.html connected!");
            }
        }

        protected override void OnClose(CloseEventArgs e) {
            this.isReady = false;

            if (this.onClose != null) {
                this.onClose();
            }

            if (Settings.debug.info) {
                Debug.Log("AirConsole: screen.html disconnected");
            }
           
            base.OnClose(e);
        }

        protected override void OnError(ErrorEventArgs e) {
            base.OnError(e);

            if (Settings.debug.error) {
                Debug.LogError("AirConsole: "+e.Message);
                Debug.LogError("AirConsole: "+e.Exception);
            }
        }

        public void ProcessMessage(string data) {

            try {

                // parse json string
                JObject msg = JObject.Parse(data);

                // catch onReady event
                if ((string)msg["action"] == "onReady") {

                    this.isReady = true;

                    if (this.onReady != null) {
                        this.onReady(msg);
                    }

                    if (Settings.debug.info) {
                        Debug.Log("AirConsole: Connections are ready!");
                    }
                }

                // catch onMessage event
                if ((string)msg["action"] == "onMessage") {

                    if (this.onMessage != null) {
                        this.onMessage(msg);
                    }
                }

                // catch onDeviceStateChange event
                if ((string)msg["action"] == "onDeviceStateChange") {

                    if (this.onDeviceStateChange != null) {
                        this.onDeviceStateChange(msg);
                    }
                }

            }

            catch (Exception e) {

                if (Settings.debug.error) {
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

    }

}
