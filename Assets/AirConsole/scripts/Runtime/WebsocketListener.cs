#if !DISABLE_AIRCONSOLE

namespace NDream.AirConsole {
    using UnityEngine;
    using System;
    using WebSocketSharp;
    using WebSocketSharp.Server;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    
    public class WebsocketListener : WebSocketBehavior {
        public event Action<JObject> onReady;
        public event Action onClose;
        public event Action<JObject> onMessage;
        public event Action<JObject> onDeviceStateChange;
        public event Action<JObject> onConnect;
        public event Action<JObject> onDisconnect;
        public event Action<JObject> onCustomDeviceStateChange;
        public event Action<JObject> onDeviceProfileChange;
        public event Action<JObject> onAdShow;
        public event Action<JObject> onAdComplete;
        public event Action<JObject> onGameEnd;
        public event Action<JObject> onHighScores;
        public event Action<JObject> onHighScoreStored;
        public event Action<JObject> onPersistentDataStored;
        public event Action<JObject> onPersistentDataLoaded;
        public event Action<JObject> onPremium;
        public event Action<JObject> onPause;
        public event Action<JObject> onResume;
        public event Action<JObject> onLaunchApp;
        public event Action<JObject> onUnityWebviewResize;
        public event Action<JObject> onUnityWebviewPlatformReady;
        public event Action<JObject> OnSetSafeArea;
        public event Action<JObject> OnUpdateContentProvider;

        private bool _isReady;

        private WebViewObject webViewObject;

        public WebsocketListener(WebViewObject webViewObject) {
            if (AirConsole.IsAndroidRuntime) {
                if (webViewObject == null) {
                    throw new ArgumentNullException(nameof(webViewObject));
                }
            }

            IgnoreExtensions = true;
            if (webViewObject != null) {
                this.webViewObject = webViewObject;
            }
        }

        public WebsocketListener() {
            IgnoreExtensions = true;
        }

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
            _isReady = false;

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
                        _isReady = true;

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
                    case "onSetSafeArea":
                        OnSetSafeArea?.Invoke(msg);
                        break;
                    case "client_update_content_provider":
                        OnUpdateContentProvider?.Invoke(msg);
                        break;
                }
            } catch (Exception e) {
                if (Settings.debug.error) {
                    Debug.LogError(e.Message);
                    Debug.LogError(e.StackTrace);
                }
            }
        }

        public bool IsReady() => _isReady;

        public void Message(JObject data) {
            switch (Application.platform) {
                case RuntimePlatform.WebGLPlayer:
                    Application.ExternalCall("window.app.processUnityData", data.ToString()); //TODO: External Call is obsolete?
                    break;
                case RuntimePlatform.Android: {
                    if (AirConsole.IsAndroidRuntime) {
                        string serialized = JsonConvert.ToString(data.ToString());
                        webViewObject.EvaluateJS("androidUnityPostMessage(" + serialized + ");");
                    }

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