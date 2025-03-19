#if !DISABLE_AIRCONSOLE
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

    public delegate void OnConnectInternal(JObject data);

    public delegate void OnDisconnectInternal(JObject data);

    public delegate void OnCustomDeviceStateChangeInternal(JObject data);

    public delegate void OnDeviceProfileChangeInternal(JObject data);

    public delegate void OnAdShowInternal(JObject data);

    public delegate void OnAdCompleteInternal(JObject data);

    public delegate void OnGameEndInternal(JObject data);

    public delegate void OnHighScoresInternal(JObject data);

    public delegate void OnHighScoreStoredInternal(JObject data);

    public delegate void OnPersistentDataStoredInternal(JObject data);

    public delegate void OnPersistentDataLoadedInternal(JObject data);

    public delegate void OnPremiumInternal(JObject data);

    public delegate void OnPauseInternal(JObject data);

    public delegate void OnResumeInternal(JObject data);

    public delegate void OnLaunchAppInternal(JObject data);

    public delegate void OnUnityWebviewResizeInternal(JObject data);

    public delegate void OnUnityWebviewPlatformReadyInternal(JObject data);

    public delegate void OnCloseInternal();

    public class WebsocketListener : WebSocketBehavior {
        // events
        public event OnReadyInternal onReady;
        public event OnCloseInternal onClose;
        public event OnMessageInternal onMessage;
        public event OnDeviceStateChangeInternal onDeviceStateChange;
        public event OnConnectInternal onConnect;
        public event OnDisconnectInternal onDisconnect;
        public event OnCustomDeviceStateChangeInternal onCustomDeviceStateChange;
        public event OnDeviceProfileChangeInternal onDeviceProfileChange;
        public event OnAdShowInternal onAdShow;
        public event OnAdCompleteInternal onAdComplete;
        public event OnGameEndInternal onGameEnd;
        public event OnHighScoresInternal onHighScores;
        public event OnHighScoreStoredInternal onHighScoreStored;
        public event OnPersistentDataStoredInternal onPersistentDataStored;
        public event OnPersistentDataLoadedInternal onPersistentDataLoaded;
        public event OnPremiumInternal onPremium;
        public event OnPauseInternal onPause;
        public event OnResumeInternal onResume;
        public event OnLaunchAppInternal onLaunchApp;
        public event OnUnityWebviewResizeInternal onUnityWebviewResize;
        public event OnUnityWebviewPlatformReadyInternal onUnityWebviewPlatformReady;

        // private vars
        private bool isReady;

#if UNITY_ANDROID
        private WebViewObject webViewObject;

        public WebsocketListener(WebViewObject webViewObject) {
            IgnoreExtensions = true;
            this.webViewObject = webViewObject;
        }
#else
        public WebsocketListener () {
            IgnoreExtensions = true;
        }
#endif

        protected override void OnMessage(MessageEventArgs e) {
            ProcessMessage(e.Data);
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
            isReady = false;

            onClose?.Invoke();

            if (Settings.debug.info) {
                Debug.Log("AirConsole: screen.html disconnected");
            }

            base.OnClose(e);
        }

        protected override void OnError(ErrorEventArgs e) {
            base.OnError(e);

            if (Settings.debug.error) {
                Debug.LogError("AirConsole: " + e.Message);
                Debug.LogError("AirConsole: " + e.Exception);
            }
        }

        public void ProcessMessage(string data) {
            try {
                JObject msg = JObject.Parse(data);
                string action = msg.SelectToken("action")?.Value<string>();

                switch (action) {
                    case "onReady": {
                        isReady = true;

                        onReady?.Invoke(msg);

                        if (Settings.debug.info) {
                            Debug.Log("AirConsole: Connections are ready!");
                        }

                        break;
                    }
                    case "onMessage":
                        onMessage?.Invoke(msg);
                        break;
                    case "onDeviceStateChange":
                        onDeviceStateChange?.Invoke(msg);
                        break;
                    case "onConnect":
                        onConnect?.Invoke(msg);
                        break;
                    case "onDisconnect":
                        onDisconnect?.Invoke(msg);
                        break;
                    case "onCustomDeviceStateChange":
                        onCustomDeviceStateChange?.Invoke(msg);
                        break;
                    case "onDeviceProfileChange":
                        onDeviceProfileChange?.Invoke(msg);
                        break;
                    case "onAdShow":
                        onAdShow?.Invoke(msg);
                        break;
                    case "onAdComplete":
                        onAdComplete?.Invoke(msg);
                        break;
                    case "onGameEnd":
                        onGameEnd?.Invoke(msg);
                        break;
                    case "onHighScores":
                        onHighScores?.Invoke(msg);
                        break;
                    case "onHighScoreStored":
                        onHighScoreStored?.Invoke(msg);
                        break;
                    case "onPersistentDataStored":
                        onPersistentDataStored?.Invoke(msg);
                        break;
                    case "onPersistentDataLoaded":
                        onPersistentDataLoaded?.Invoke(msg);
                        break;
                    case "onPremium":
                        onPremium?.Invoke(msg);
                        break;
                    case "onPause":
                        onPause?.Invoke(msg);
                        break;
                    case "onResume":
                        onResume?.Invoke(msg);
                        break;
                    case "onLaunchApp":
                        onLaunchApp?.Invoke(msg);
                        break;
                    case "onUnityWebviewResize":
                        onUnityWebviewResize?.Invoke(msg);
                        break;
                    case "onUnityWebviewPlatformReady":
                        onUnityWebviewPlatformReady?.Invoke(msg);
                        break;
                }
            } catch (Exception e) {
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
            switch (Application.platform) {
                case RuntimePlatform.WebGLPlayer:
                    Application.ExternalCall("window.app.processUnityData", data.ToString()); //TODO: External Call is obsolete?
                    break;
                case RuntimePlatform.Android: {
#if UNITY_ANDROID
                    string serialized = JsonConvert.ToString(data.ToString());
                    webViewObject.EvaluateJS("androidUnityPostMessage(" + serialized + ");");
#endif
                    break;
                }
                default:
                    Send(data.ToString());
                    break;
            }
        }
    }
}
#endif