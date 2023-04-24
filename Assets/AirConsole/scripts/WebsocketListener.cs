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
	public delegate void OnReadyInternal (JObject data);

	public delegate void OnMessageInternal (JObject data);

	public delegate void OnDeviceStateChangeInternal (JObject data);

	public delegate void OnConnectInternal (JObject data);

	public delegate void OnDisconnectInternal (JObject data);

	public delegate void OnCustomDeviceStateChangeInternal (JObject data);

	public delegate void OnDeviceProfileChangeInternal (JObject data);

	public delegate void OnAdShowInternal (JObject data);

	public delegate void OnAdCompleteInternal (JObject data);

	public delegate void OnGameEndInternal (JObject data);

	public delegate void OnHighScoresInternal (JObject data);

	public delegate void OnHighScoreStoredInternal (JObject data);

	public delegate void OnPersistentDataStoredInternal (JObject data);

	public delegate void OnPersistentDataLoadedInternal (JObject data);

	public delegate void OnPremiumInternal (JObject data);

	public delegate void OnPauseInternal (JObject data);

	public delegate void OnResumeInternal (JObject data);

	public delegate void OnLaunchAppInternal (JObject data);

	public delegate void OnUnityWebviewResizeInternal (JObject data);

	public delegate void OnUnityWebviewPlatformReadyInternal (JObject data);

	public delegate void OnCloseInternal ();

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
        WebViewObject webViewObject;

        public WebsocketListener(WebViewObject webViewObject) {
            base.IgnoreExtensions = true;
            this.webViewObject = webViewObject;
        }
#else
		public WebsocketListener () {
			base.IgnoreExtensions = true;
		}
#endif

		protected override void OnMessage (MessageEventArgs e) {
			this.ProcessMessage (e.Data);
		}

		protected override void OnOpen () {
			base.OnOpen ();

			// send welcome debug message to screen.html
			Send (@"{ ""action"": ""debug"", ""data"": ""welcome screen.html!"" }");

			if (Settings.debug.info) {
				Debug.Log ("AirConsole: screen.html connected!");
			}
		}

		protected override void OnClose (CloseEventArgs e) {
			this.isReady = false;

			if (this.onClose != null) {
				this.onClose ();
			}

			if (Settings.debug.info) {
				Debug.Log ("AirConsole: screen.html disconnected");
			}

			base.OnClose (e);
		}

		protected override void OnError (ErrorEventArgs e) {
			base.OnError (e);

			if (Settings.debug.error) {
				Debug.LogError ("AirConsole: " + e.Message);
				Debug.LogError ("AirConsole: " + e.Exception);
			}
		}

		public void ProcessMessage (string data) {

			try {

				// parse json string
				JObject msg = JObject.Parse (data);

				if ((string)msg ["action"] == "onReady") {
					this.isReady = true;

					if (this.onReady != null) {
						this.onReady (msg);
					}

					if (Settings.debug.info) {
						Debug.Log ("AirConsole: Connections are ready!");
					}
				} else if ((string)msg ["action"] == "onMessage") {
					if (this.onMessage != null) {
						this.onMessage (msg);
					}
				} else if ((string)msg ["action"] == "onDeviceStateChange") {
					if (this.onDeviceStateChange != null) {
						this.onDeviceStateChange (msg);
					}
				} else if ((string)msg ["action"] == "onConnect") {
					if (this.onConnect != null) {
						this.onConnect (msg);
					}
				} else if ((string)msg ["action"] == "onDisconnect") {
					if (this.onDisconnect != null) {
						this.onDisconnect (msg);
					}
				} else if ((string)msg ["action"] == "onCustomDeviceStateChange") {
					if (this.onCustomDeviceStateChange != null) {
						this.onCustomDeviceStateChange (msg);
					}
				} else if ((string)msg ["action"] == "onDeviceProfileChange") {
					if (this.onDeviceProfileChange != null) {
						this.onDeviceProfileChange (msg);
					}
				} else if ((string)msg ["action"] == "onAdShow") {
					if (this.onAdShow != null) {
						this.onAdShow (msg);
					}
				} else if ((string)msg ["action"] == "onAdComplete") {
					if (this.onAdComplete != null) {
						this.onAdComplete (msg);
					}
				} else if ((string)msg ["action"] == "onGameEnd") {
					if (this.onGameEnd != null) {
						this.onGameEnd (msg);
					}
				} else if ((string)msg ["action"] == "onHighScores") {
					if (this.onHighScores != null) {
						this.onHighScores (msg);
					}
				} else if ((string)msg ["action"] == "onHighScoreStored") {
					if (this.onHighScoreStored != null) {
						this.onHighScoreStored (msg);
					}
				} else if ((string)msg ["action"] == "onPersistentDataStored") {
					if (this.onPersistentDataStored != null) {
						this.onPersistentDataStored (msg);
					}
				} else if ((string)msg ["action"] == "onPersistentDataLoaded") {

					if (this.onPersistentDataLoaded != null) {
						this.onPersistentDataLoaded (msg);
					}
				} else if ((string)msg ["action"] == "onPremium") {
					if (this.onPremium != null) {
						this.onPremium (msg);
					}
				} else if ((string)msg ["action"] == "onPause") {
				  if (this.onPause != null) {
					this.onPause (msg);
				  }
				} else if ((string)msg ["action"] == "onResume") {
				  if (this.onResume != null) {
					this.onResume (msg);
				  }
				} else if ((string)msg ["action"] == "onLaunchApp") {
					if (this.onLaunchApp != null) {
						this.onLaunchApp (msg);
					}
				} else if ((string)msg ["action"] == "onUnityWebviewResize") {
					if (this.onUnityWebviewResize != null) {
						this.onUnityWebviewResize (msg);
					}
				} else if ((string)msg ["action"] == "onUnityWebviewPlatformReady") {
					if (this.onUnityWebviewPlatformReady != null) {
						this.onUnityWebviewPlatformReady (msg);
					}
				}
			} catch (Exception e) {
				if (Settings.debug.error) {
					Debug.LogError (e.Message);
					Debug.LogError (e.StackTrace);
				}
			}
		}

		public bool IsReady () {
			return isReady;
		}

		public void Message (JObject data) {
			if (Application.platform == RuntimePlatform.WebGLPlayer) {
				Application.ExternalCall ("window.app.processUnityData", data.ToString ()); //TODO: External Call is obsolete?
			} else if (Application.platform == RuntimePlatform.Android) {

#if UNITY_ANDROID

				string serialized = JsonConvert.ToString(data.ToString());
				webViewObject.EvaluateJS("androidUnityPostMessage(" + serialized + ");");
#endif

			} else {

				Send (data.ToString ());
			}
		}

	}
}
#endif
