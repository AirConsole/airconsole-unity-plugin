#if !UNITY_EDITOR && UNITY_ANDROID
#define AIRCONSOLE_ANDROID_RUNTIME
#endif

namespace NDream.AirConsole {
    using NDream.AirConsole.Android.Plugin;
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Threading;
    using System;
    using UnityEngine.SceneManagement;
    using UnityEngine.Serialization;
    using UnityEngine;
    using WebSocketSharp.Server;

    public enum StartMode {
        VirtualControllers,
        Debug,
        DebugVirtualControllers,
        Normal,
        NoBrowserStart
    }

    public enum AndroidUIResizeMode {
        NoResizing,
        ResizeCamera,
        ResizeCameraAndReferenceResolution
    }

    public delegate void OnReady(string code);

    public delegate void OnMessage(int from, JToken data);

    public delegate void OnDeviceStateChange(int deviceId, JToken deviceData);

    public delegate void OnConnect(int deviceId);

    public delegate void OnDisconnect(int deviceId);

    public delegate void OnCustomDeviceStateChange(int deviceId, JToken customDeviceStateData);

    public delegate void OnDeviceProfileChange(int deviceId);

    public delegate void OnAdShow();

    public delegate void OnAdComplete(bool adWasShown);

    public delegate void OnGameEnd();

    public delegate void OnHighScores(JToken highscores);

    public delegate void OnHighScoreStored(JToken highscore);

    public delegate void OnPersistentDataStored(string uid);

    public delegate void OnPersistentDataLoaded(JToken data);

    public delegate void OnPremium(int deviceId);

    public delegate void OnPause();

    public delegate void OnResume();

    /// <summary>
    /// Used to notify the game about changes to audio focus and maximum allowed volume.
    /// </summary>
    /// <param name="hasAudioFocus">True, if the application has AudioFocus. Otherwise the application should stop all audio playback e.g. AudioListener.pause = !hasAudioFocus</param>
    /// <param name="newMaximumVolume">The received volume is a float value between 0.0 (muted) and 1.0 (maximum volume).</param>
    public delegate void OnGameAudioFocusChanged(bool hasAudioFocus, float newMaximumVolume);

    /// <summary>
    /// Gets called when the safe area of the screen changes.
    /// This event provides information about the visible area of the screen where your
    /// game should display important content.
    /// </summary>
    /// <param name="newSafeArea">The new safe area rectangle in pixel coordinates.</param>
    public delegate void OnSafeAreaChanged(Rect newSafeArea);

    public class AirConsole : MonoBehaviour {
        #region airconsole unity config
        [Tooltip("The controller html file for your game")]
        public UnityEngine.Object controllerHtml;

        [Tooltip("Automatically scale the game canvas")]
        public bool autoScaleCanvas = true;

        [FormerlySerializedAs("androidTvGameVersion")]
        [Header("Android Settings")]
        [Tooltip(
            "The uploaded web version on the AirConsole Developer Console where your game retrieves its controller data. See details: https://developers.airconsole.com/#!/guides/unity-androidtv")]
        public string androidGameVersion;

        [Tooltip(
            "Resize mode to allow space for AirConsole Default UI. See https://developers.airconsole.com/#!/guides/unity-androidtv\n"
            + "Use this together with OnSafeAreaChanged")]
        public AndroidUIResizeMode androidUIResizeMode;

        [Tooltip("Loading Sprite to be displayed at the start of the game.")]
        public Sprite webViewLoadingSprite;

        [Tooltip("Enable SafeArea support with fullscreen webview overlay for Android.")]
        [SerializeField]
        private bool nativeGameSizingSupported = true;

        [Header("Development Settings")]
        [Tooltip("Start your game normally, with virtual controllers or in debug mode.")]
        public StartMode browserStartMode;

        [Tooltip("Game Id to use for persistentData, HighScore and Translation functionalities")]
        public string devGameId;

        [Tooltip("Language used in the simulator during play mode.")]
        public string devLanguage;

        [Tooltip(
            "Used as local IP instead of your public IP in Unity Editor. Use this to use the controller together with ngrok")]
        public string LocalIpOverride;
        #endregion

#if !DISABLE_AIRCONSOLE

        #region airconsole api
        // ReSharper disable MemberCanBePrivate.Global UnusedMember.Global

        /// <summary>
        /// Device ID of the screen
        /// </summary>
        public const int SCREEN = 0;

        /// <summary>
        /// Gets the Version string to provide it to remote addressable path.
        /// </summary>
        /// <remarks>This is designed to be used with Remote Addressable Configuration as {NDream.AirConsole.AirConsole.Version} path fragment</remarks>
        public static string Version {
            get => IsAndroidOrEditor ? instance.androidGameVersion : string.Empty;
        }

        /// <summary>
        /// AirConsole Singleton Instance.
        /// This is your direct access to the AirConsole API.
        /// </summary>
        /// <value>AirConsole Singleton Instance</value>
        public static AirConsole instance {
            get {
                if (_instance == null) {
                    _instance = ACFindObjectOfType<AirConsole>();
                    if (_instance != null && Application.isPlaying) {
                        DontDestroyOnLoad(_instance.gameObject);
                    }
                }

                return _instance;
            }
        }

        /// <summary>
        /// Gets called when the game console is ready.
        /// This event also fires onConnect for all devices that already are
        /// connected and have loaded your game.
        /// This event also fires OnCustomDeviceStateChange for all devices that are
        /// connected, have loaded your game and have set a custom Device State.
        /// </summary>
        /// <param name="code">The AirConsole join code.</param>
        public event OnReady onReady;

        /// <summary>
        /// Gets called when a message is received from another device
        /// that called message() or broadcast().
        /// </summary>
        /// <param name="from">The device ID that sent the message.</param>
        /// <param name="data">The data that was sent.</param>
        public event OnMessage onMessage;

        /// <summary>
        /// Gets called when a device joins/leaves a game session or updates its DeviceState (custom DeviceState, profile pic, nickname).
        /// This is function is also called every time OnConnect, OnDisconnect or OnCustomDeviceStateChange is called. It's like their root function.<br />
        /// Check <see cref="OnDeviceStateChange"/> for the event handler parameters.
        /// </summary>
        /// <param name="deviceId">the device ID that changed its DeviceState.</param>
        /// <param name="deviceData"> the data of that device. If undefined, the device has left.</param>
        public event OnDeviceStateChange onDeviceStateChange;

        /// <summary>
        /// Gets called when a device has connected and loaded the game.<br />
        /// Check <see cref="OnConnect"/> for the event handler parameters.
        /// </summary>
        /// <param name="deviceId">the device ID that loaded the game.</param>
        public event OnConnect onConnect;

        /// <summary>
        /// Gets called when a device has left the game.
        /// </summary>
        /// <param name="deviceId">the device ID that left the game.</param>
        public event OnDisconnect onDisconnect;

        /// <summary>
        /// Gets called when a device updates it's custom DeviceState by calling SetCustomDeviceState or SetCustomDeviceStateProperty.
        /// Make sure you understand the power of device states: http://developers.airconsole.com/#/guides/device_ids_and_states
        /// </summary>
        /// <param name="deviceId">the device ID that changed its customDeviceState.</param>
        /// <param name="customDeviceStateData">The custom DeviceState data value.</param>
        public event OnCustomDeviceStateChange onCustomDeviceStateChange;

        /// <summary>
        /// Gets called when a device updates it's profile pic, nickname or email.
        /// </summary>
        /// <param name="deviceId">The device_id that changed its profile.</param>
        public event OnDeviceProfileChange onDeviceProfileChange;

        /// <summary>
        /// Gets called if a fullscreen advertisement is shown on this screen.
        /// In case this event gets called, please mute all sounds.
        /// </summary>
        public event OnAdShow onAdShow;

        /// <summary>
        /// Gets called when an advertisement is finished or no advertisement was shown.
        /// </summary>
        /// <param name="adWasShown">True if an ad was shown and onAdShow was called.</param>
        public event OnAdComplete onAdComplete;

        /// <summary>
        /// Gets called when the game should be terminated.
        /// In case this event gets called, please mute all sounds and stop all animations.
        /// </summary>
        public event OnGameEnd onGameEnd;

        /// <summary>
        /// Gets called when high scores are returned after calling requestHighScores.
        /// <param name="highscores">The high scores.</param>
        /// </summary>
        public event OnHighScores onHighScores;

        /// <summary>
        /// Gets called when a high score was successfully stored.
        /// </summary>
        /// <param name="highscore">The stored high score if it is a new best for the user or else null.</param>
        public event OnHighScoreStored onHighScoreStored;

        /// <summary>
        /// Gets called when persistent data was stored from StorePersistentData().
        /// </summary>
        /// <param name="uid">The uid for which the data was stored.</param>
        public event OnPersistentDataStored onPersistentDataStored;

        /// <summary>
        /// Gets called when persistent data was loaded from RequestPersistentData().
        /// </summary>
        /// <param name="data">An object mapping uids to all key value pairs.</param>
        public event OnPersistentDataLoaded onPersistentDataLoaded;

        /// <summary>
        /// Gets called when a device becomes premium or when a premium device connects.
        /// <param name="deviceId">The device id of the premium device.</param>
        /// </summary>
        public event OnPremium onPremium;

        /// <summary>
        /// Gets called when the game should be paused.
        /// </summary>
        public event OnPause onPause;

        /// <summary>
        /// Gets called when the game should be resumed.
        /// </summary>
        public event OnResume onResume;

        /// <summary>
        /// Used to notify the game about changes to audio focus and maximum allowed volume.
        /// This must be implemented for Android based games and must be registered before OnReady is invoked.
        /// </summary>
        /// <param name="hasAudioFocus">True, if the application has AudioFocus. Otherwise the application should stop all audio playback e.g. AudioListener.pause = !hasAudioFocus</param>
        /// <param name="newMaximumVolume">The received volume is a float value between 0.0 (muted) and 1.0 (maximum volume).</param>
        public event OnGameAudioFocusChanged OnGameAudioFocusChanged;

        private bool _canHaveAudioFocus = true;
        private bool _ignoreAudioFocusLoss = true;

        internal event Action UnityDestroy;
        internal event Action UnityResume;
        internal event Action UnityPause;

        /// <summary>
        /// Is invoked when the SafeArea of the device changes through the platform.
        /// </summary>
        /// <param name="newSafeArea">The new safe area as a camera pixelRect the game and game's UI must respect</param>
        /// <remarks>On Android Automotive targets, this event must be used to drive changes to Unity GUI / UIElement reference resolutions where necessary.</remarks>
        public event OnSafeAreaChanged OnSafeAreaChanged;

        /// <summary>
        /// Determines whether the AirConsole Unity Plugin is ready. Use onReady event instead if possible.
        /// </summary>
        /// <returns><c>true</c> if the AirConsole Unity Plugin is ready; otherwise, <c>false</c>.</returns>
        public bool IsAirConsoleUnityPluginReady() => wsListener != null && wsListener.IsReady();

        /// <summary>
        /// Sends a message to another device.
        /// </summary>
        /// <param name="to">The device ID to send the message to.</param>
        /// <param name="data">The data to send.</param>
        public void Message(int to, object data) {
            if (!IsAirConsoleUnityPluginReady()) {
                throw new NotReadyException();
            }

            JObject msg = new();
            msg.Add("action", "message");
            msg.Add("from", to);
            msg.Add("data", JToken.FromObject(data));

            wsListener.Message(msg);
        }

        /// <summary>
        /// Sends a message to all devices.
        /// </summary>
        /// <param name="data">The message to send.</param>
        public void Broadcast(object data) {
            if (!IsAirConsoleUnityPluginReady()) {
                throw new NotReadyException();
            }

            JObject msg = new();
            msg.Add("action", "broadcast");
            msg.Add("data", JToken.FromObject(data));

            wsListener.Message(msg);
        }

        /// <summary>
        /// Returns the device_id of this device.
        /// Every device in a AirConsole session has a device_id.
        /// The screen always has device_id 0. You can use the AirConsole.SCREEN
        /// constant instead of 0.
        /// All controllers also get a device_id. You can NOT assume that the device_ids
        /// of controllers are consecutive or that they start at 1.
        ///
        /// DO NOT HARDCODE CONTROLLER DEVICE IDS!
        ///
        /// If you want to have a logic with "players numbers" (Player 0, Player 1,
        /// Player 2, Player 3) use the setActivePlayers helper function! You can
        /// hardcode player numbers, but not device_ids.
        ///
        /// Within an AirConsole session, devices keep the same device_id when they
        /// disconnect and reconnect. Different controllers will never get the same
        /// device_id in a session. Every device_id remains reserved for the device that
        /// originally got it.
        ///
        /// For more info read
        /// http:// developers.airconsole.com/#/guides/device_ids_and_states
        /// </summary>
        /// <returns>The device identifier.</returns>
        public int GetDeviceId() {
            if (!IsAirConsoleUnityPluginReady()) {
                throw new NotReadyException();
            }

            return _device_id;
        }

        /// <summary>
        /// Takes all currently connected controllers and assigns them a player number.
        ///  Can only be called by the screen. You don't have to use this helper
        /// function, but this mechanism is very convenient if you want to know which
        /// device is the first player, the second player, the third player ...
        /// The assigned player numbers always start with 0 and are consecutive.
        /// You can hardcode player numbers, but not device_ids.
        /// Once the screen has called setActivePlayers you can get the device_id of
        /// the first player by calling convertPlayerNumberToDeviceId(0), the device_id
        /// of the second player by calling convertPlayerNumberToDeviceId(1), ...
        /// You can also convert device_ids to player numbers by calling
        /// convertDeviceIdToPlayerNumber(device_id). You can get all device_ids that
        /// are active players by calling getActivePlayerDeviceIds().
        /// The screen can call this function every time a game round starts.
        /// </summary>
        /// <param name="max_players">The maximum number of controllers that should
        /// get a player number assigned.</param>
        public void SetActivePlayers(int max_players = -1) {
            if (!IsAirConsoleUnityPluginReady()) {
                throw new NotReadyException();
            }

            List<int> device_ids = GetControllerDeviceIds();
            _players.Clear();
            if (max_players == -1) {
                max_players = device_ids.Count;
            }

            for (int i = 0; i < device_ids.Count && i < max_players; ++i) {
                _players.Add(device_ids[i]);
            }

            JObject msg = new();
            msg.Add("action", "setActivePlayers");
            msg.Add("max_players", max_players);

            wsListener.Message(msg);
        }

        /// <summary>
        /// Returns the device_id of a player, if the player is part of the active
        /// players previously set by the screen by calling setActivePlayers. If fewer
        /// players are in the game than the passed in player_number or the active
        /// players have not been set by the screen, this function returns undefined.
        /// </summary>
        /// <param name="player_number">Player Number.</param>
        public int ConvertPlayerNumberToDeviceId(int player_number) {
            if (player_number >= 0 && player_number < _players.Count) {
                return _players[player_number];
            }

            return -1;
        }

        /// <summary>
        /// Returns the player number for a device_id, if the device_id is part of the
        /// active players previously set by the screen by calling setActivePlayers.
        /// Player numbers are zero based and are consecutive. If the device_id is not
        /// part of the active players, this function returns -1.
        /// </summary>
        /// <param name="device_id">Device id.</param>
        public int ConvertDeviceIdToPlayerNumber(int device_id) => _players.IndexOf(device_id);

        /// <summary>
        /// Returns the globally unique id of a device.
        /// </summary>
        /// <returns>The UID.</returns>
        /// <param name="device_id">The device id for which you want the uid. Default is this device.</param>
        public string GetUID(int device_id = -1) {
            if (!IsAirConsoleUnityPluginReady()) {
                throw new NotReadyException();
            }

            if (device_id == -1) {
                device_id = GetDeviceId();
            }

            JToken device = GetDevice(device_id);
            if (device == null) {
                return null;
            }

            return (string)device["uid"];
        }

        /// <summary>
        /// Gets the custom DeviceState of a device.
        /// </summary>
        /// <param name="device_id">The device ID of which you want the custom state. Default is this device.</param>
        /// <returns> The custom data previously set by the device.</returns>
        public JToken GetCustomDeviceState(int device_id = -1) {
            if (!IsAirConsoleUnityPluginReady()) {
                throw new NotReadyException();
            }

            if (device_id == -1) {
                device_id = GetDeviceId();
            }

            if (GetDevice(device_id) != null) {
                try {
                    return GetDevice(device_id)["custom"];
                } catch (Exception e) {
                    if (Settings.debug.error) {
                        AirConsoleLogger.LogError(() => $"AirConsole: {e.Message}");
                    }

                    return null;
                }
            } else {
                if (Settings.debug.warning) {
                    AirConsoleLogger.LogWarning(() => $"AirConsole: GetCustomDeviceState: device_id {device_id} not found");
                }

                return null;
            }
        }

        /// <summary>
        /// Returns the nickname of a user.
        /// </summary>
        /// <param name="device_id">The device id for which you want the nickname. Default is this device. Screens don't have nicknames.</param>
        public string GetNickname(int device_id = -1) {
            if (!IsAirConsoleUnityPluginReady()) {
                throw new NotReadyException();
            }

            if (device_id == -1) {
                device_id = GetDeviceId();
            }

            if (GetDevice(device_id) != null) {
                try {
                    if (GetDevice(device_id)["nickname"] != null) {
                        return (string)GetDevice(device_id)["nickname"];
                    } else {
                        return "Guest " + device_id;
                    }
                } catch (Exception) {
                    return "Guest " + device_id;
                }
            } else {
                if (Settings.debug.warning) {
                    AirConsoleLogger.LogWarning(() => "AirConsole: GetNickname: device_id " + device_id + " not found");
                }

                return null;
            }
        }

        /// <summary>
        /// Returns the current IETF language tag of a device e.g. "en" or "en-US"
        /// </summary>
        /// <param name="device_id">The device id for which you want the language. Default is this device.</param>
        public string GetLanguage(int device_id = -1) {
            if (!IsAirConsoleUnityPluginReady()) {
                throw new NotReadyException();
            }

            if (device_id == -1) {
                device_id = GetDeviceId();
            }

            if (GetDevice(device_id) != null) {
                return (string)GetDevice(device_id)["language"];
            } else {
                if (Settings.debug.warning) {
                    AirConsoleLogger.LogWarning(() => "AirConsole: GetLanguage: device_id " + device_id);
                }

                return null;
            }
        }

        /// <summary>
        /// Gets a translation for the users current language See http://developers.airconsole.com/#!/guides/translations
        /// </summary>
        /// <param name="id">The id of the translation string.</param>
        /// <param name="values">Values that should be used for replacement in the translated string. E.g. if a translated string is "Hi %name%" and values is {"name": "Tom"} then this will be replaced to "Hi Tom".</param>
        public string GetTranslation(string id, Dictionary<string, string> values = null) {
            string result = null;

            if (_translations != null) {
                if (_translations.ContainsKey(id)) {
                    result = _translations[id];

                    if (values != null) {
                        string[] parts = result.Split('%');

                        for (int i = 1; i < parts.Length; i += 2) {
                            if (parts[i].Length > 0) {
                                if (values.ContainsKey(parts[i])) {
                                    parts[i] = values[parts[i]];
                                } else {
                                    parts[i] = "";
                                }
                            } else {
                                parts[i] = "%";
                            }
                        }

                        result = string.Join("", parts);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Translates an array of UI Text or TextMesh Components. The existing text in the UI Text or Text Mesh has to contain a string ID within double curly brackets. {{example}}
        /// </summary>
        /// <param name="elements">The Array of elements that should be translated.</param>
        public void TranslateUIElements(object[] elements) {
            for (int i = 0; i < elements.Length; ++i) {
                string id = null;
                string translation = null;
                if (elements[i].GetType() == typeof(UnityEngine.UI.Text)) {
                    id = ((UnityEngine.UI.Text)elements[i]).text;
                } else if (elements[i] is TextMesh) {
                    id = ((TextMesh)elements[i]).text;
                } else {
                    throw new Exception("Translate UI Elements only supports UI Text and TextMesh Components.");
                }

                if (id != null) {
                    if (id.StartsWith("{{", StringComparison.Ordinal) && id.EndsWith("}}", StringComparison.Ordinal)) {
                        id = id.Substring(2, id.Length - 4).Trim();

                        translation = GetTranslation(id);
                    }
                }

                if (translation == null) {
                    AirConsoleLogger.LogWarning(() => "Translation not found: " + id);
                } else {
                    if (elements[i].GetType() == typeof(UnityEngine.UI.Text)) {
                        ((UnityEngine.UI.Text)elements[i]).text = translation;
                    } else if (elements[i] is TextMesh) {
                        ((TextMesh)elements[i]).text = translation;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the url to a profile picture of a user.
        /// </summary>
        /// <param name="uid">The uid for which you want a profile picture. Screens don't have profile pictures.</param>
        /// <param name="size">The size of in pixels of the picture. Default is 64.</param>
        public string GetProfilePicture(string uid, int size = 64) =>
            $"{Settings.AIRCONSOLE_PROFILE_PICTURE_URL}{uid}&size={size}";

        /// <summary>
        /// Returns the url to a profile picture of a user.
        /// </summary>
        /// <param name="device_id">The device id for which you want a profile picture. Defaults to this device. Screens don't have profile pictures.</param>
        /// <param name="size">The size of in pixels of the picture. Default is 64.</param>
        public string GetProfilePicture(int device_id = -1, int size = 64) {
            if (!IsAirConsoleUnityPluginReady()) {
                throw new NotReadyException();
            }

            if (device_id == -1) {
                device_id = GetDeviceId();
            }

            if (GetDevice(GetDeviceId()) != null) {
                try {
                    return Settings.AIRCONSOLE_PROFILE_PICTURE_URL
                           + (string)GetDevice(device_id)["uid"]
                           + "&size="
                           + size;
                } catch (Exception) {
                    if (Settings.debug.warning) {
                        AirConsoleLogger.LogWarning(() =>
                            "AirConsole: GetProfilePicture: can't find profile picture of device_id:" + device_id);
                    }

                    return null;
                }
            }

            if (Settings.debug.warning) {
                AirConsoleLogger.LogWarning(() => "AirConsole: GetProfilePicture: " + device_id + " not found");
            }

            return null;
        }

        /// <summary>
        /// Returns the current time on the game server.
        /// This allows you to have a synchronized clock: You can send the servertime in a message to know exactly at what point something happened on a device.
        /// </summary>
        /// <returns> Timestamp in milliseconds.</returns>
        public long GetServerTime() {
            if (!IsAirConsoleUnityPluginReady()) {
                throw new NotReadyException();
            }

            return (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds + _server_time_offset;
        }

        /// <summary>
        /// Request that all devices return to the AirConsole store.
        /// </summary>
        public void NavigateHome() {
            if (!IsAirConsoleUnityPluginReady()) {
                throw new NotReadyException();
            }

            JObject msg = new();
            msg.Add("action", "navigateHome");

            wsListener.Message(msg);
        }

        /// <summary>
        /// Request that all devices load a game by url. Note that the custom DeviceStates are preserved.
        /// If you don't want that, override SetCustomDeviceState(null) on every device before calling this function.
        /// </summary>
        public void NavigateTo(string url) {
            if (!IsAirConsoleUnityPluginReady()) {
                throw new NotReadyException();
            }

            JObject msg = new();
            msg.Add("action", "navigateTo");
            msg.Add("data", url);

            wsListener.Message(msg);
        }

        /// <summary>
        /// Sets the custom DeviceState of this device.
        /// </summary>
        /// <param name="data">The custom data to set.</param>
        public void SetCustomDeviceState(object data) {
            if (!IsAirConsoleUnityPluginReady()) {
                throw new NotReadyException();
            }

            JObject msg = new();
            msg.Add("action", "setCustomDeviceState");
            msg.Add("data", JToken.FromObject(data));

            AllocateDeviceSlots(0);
            if (GetDevice(0) == null) {
                _devices[0] = new JObject();
            }

            _devices[0]["custom"] = msg["data"];

            wsListener.Message(msg);
        }

        /// <summary>
        /// Sets a property in the custom DeviceState of this device.
        /// </summary>
        /// <param name="key">The property name.</param>
        /// <param name="value">The property value.</param>
        public void SetCustomDeviceStateProperty(string key, object value) {
            if (!IsAirConsoleUnityPluginReady()) {
                throw new NotReadyException();
            }

            JObject msg = new();
            msg.Add("action", "setCustomDeviceStateProperty");
            msg.Add("key", JToken.FromObject(key));
            msg.Add("value", JToken.FromObject(value));

            AllocateDeviceSlots(0);
            if (GetDevice(0) == null) {
                _devices[0] = new JObject();
            }

            JToken custom = _devices[0]["custom"];
            if (custom == null) {
                JObject newCustom = new();
                _devices[0]["custom"] = JToken.FromObject(newCustom);
            }


            _devices[0]["custom"][key] = msg["value"];

            wsListener.Message(msg);
        }

        /// <summary>
        /// Requests that AirConsole shows a multiscreen advertisement.
        /// onAdShow is called on all connected devices if an advertisement
        /// is shown (in this event please mute all sounds).
        /// onAdComplete is called on all connected devices when the
        /// advertisement is complete or no advertisement was shown.
        /// </summary>
        public void ShowAd() {
            if (!IsAirConsoleUnityPluginReady()) {
                throw new NotReadyException();
            }

            JObject msg = new();
            msg.Add("action", "showAd");

            wsListener.Message(msg);
        }

        /// <summary>
        /// Returns the device ID of the master controller. Premium devices are prioritized.
        /// </summary>
        public int GetMasterControllerDeviceId() {
            if (!IsAirConsoleUnityPluginReady()) {
                throw new NotReadyException();
            }

            List<int> result_premium = GetPremiumDeviceIds();
            if (result_premium.Count > 0) {
                return result_premium[0];
            } else {
                List<int> result = GetControllerDeviceIds();
                if (result.Count > 0) {
                    return result[0];
                }
            }

            return 0;
        }

        /// <summary>
        /// Returns all controller device ids that have loaded your game.
        /// </summary>
        public List<int> GetControllerDeviceIds() {
            if (!IsAirConsoleUnityPluginReady()) {
                throw new NotReadyException();
            }

            List<int> result = new();
            string game_url = GetGameUrl(_location);
            for (int i = 1; i < _devices.Count; ++i) {
                JToken device = GetDevice(i);
                if (device != null && GetGameUrl((string)device["location"]) == game_url) {
                    result.Add(i);
                }
            }

            return result;
        }

        /// <summary>
        /// Returns true if a user is logged in.
        /// </summary>
        public bool IsUserLoggedIn(int device_id = -1) {
            if (!IsAirConsoleUnityPluginReady()) {
                throw new NotReadyException();
            }

            if (device_id == -1) {
                device_id = GetDeviceId();
            }

            if (GetDevice(device_id) == null) {
                return false;
            }

            try {
                if (GetDevice(device_id)["auth"] != null) {
                    return (bool)GetDevice(device_id)["auth"];
                }
            } catch (Exception) {
                return false;
            }

            return false;
        }

        /// <summary>
        /// Requests high score data of players (including global high scores and friends).
        /// Will call onHighScores when data was received.
        /// </summary>
        /// <param name="level_name">The name of the level.</param>
        /// <param name="level_version">The version of the level.</param>
        /// <param name="uids">An array of UIDs of the users should be included in the result. Default is all connected controllers.</param>
        /// <param name="ranks">An array of high score rank types. High score rank types can include data from across the world, only a specific area or a users friends. Valid array entries are "world",  "country",  "region", "city", "friends", "partner". Default is ["world"].</param>
        /// <param name="total">Amount of high scores to return per rank type. Default is 8.</param>
        /// <param name="top">Amount of top high scores to return per rank type. top is part of total. Default is 5.</param>
        public void RequestHighScores(string level_name, string level_version, List<string> uids = null,
            List<string> ranks = null,
            int total = -1, int top = -1) {
            if (!IsAirConsoleUnityPluginReady()) {
                throw new NotReadyException();
            }

            JObject msg = new();
            msg.Add("action", "requestHighScores");
            msg.Add("level_name", level_name);
            msg.Add("level_version", level_version);

            if (uids != null) {
                JArray uidsJArray = new();
                foreach (string uid in uids) {
                    uidsJArray.Add(uid);
                }

                msg.Add("uids", uidsJArray);
            }

            if (ranks != null) {
                JArray ranksJArray = new();
                foreach (string rank in ranks) {
                    ranksJArray.Add(rank);
                }

                msg.Add("ranks", ranksJArray);
            }

            if (total != -1) {
                msg.Add("total", total);
            }

            if (top != -1) {
                msg.Add("top", top);
            }

            wsListener.Message(msg);
        }

        /// <summary>
        /// Stores a high score of the current user on the AirConsole servers.
        /// High scores may be returned to anyone. Do not include sensitive data. Only updates the high score if it was a higher or same score.
        /// Calls onHighScoreStored when the request is done.
        /// <param name="level_name">The name of the level the user was playing. This should be a human readable string because it appears in the high score sharing image. You can also just pass an empty string.</param>
        /// <param name="level_version">The version of the level the user was playing. This is for your internal use.</param>
        /// <param name="score">The score the user has achieved</param>
        /// <param name="uid">The UID of the user that achieved the high score.</param>
        /// <param name="data">Custom high score data (e.g. can be used to implement Ghost modes or include data to verify that it is not a fake high score).</param>
        /// <param name="score_string">A short human readable representation of the score. (e.g. "4 points in 3s"). Defaults to "X points" where x is the score converted to an integer.</param>
        /// </summary>
        public void StoreHighScore(string level_name, string level_version, float score, string uid,
            JObject data = null,
            string score_string = null) {
            List<string> uids = new();
            uids.Add(uid);
            StoreHighScore(level_name, level_version, score, uids, data, score_string);
        }

        /// <summary>
        /// Stores a high score of the current user on the AirConsole servers.
        /// High scores may be returned to anyone. Do not include sensitive data. Only updates the high score if it was a higher or same score.
        /// Calls onHighScoreStored when the request is done.
        /// <param name="level_name">The name of the level the user was playing. This should be a human readable string because it appears in the high score sharing image. You can also just pass an empty string.</param>
        /// <param name="level_version">The version of the level the user was playing. This is for your internal use.</param>
        /// <param name="score">The score the user has achieved</param>
        /// <param name="uids">The UIDs of the users that achieved the high score.</param>
        /// <param name="data">Custom high score data (e.g. can be used to implement Ghost modes or include data to verify that it is not a fake high score).</param>
        /// <param name="score_string">A short human readable representation of the score. (e.g. "4 points in 3s"). Defaults to "X points" where x is the score converted to an integer.</param>
        /// </summary>
        public void StoreHighScore(string level_name, string level_version, float score, List<string> uids,
            JObject data = null,
            string score_string = null) {
            if (!IsAirConsoleUnityPluginReady()) {
                throw new NotReadyException();
            }

            JObject msg = new();
            msg.Add("action", "storeHighScore");
            msg.Add("level_name", level_name);
            msg.Add("level_version", level_version);
            msg.Add("score", score);


            JArray uidJArray = new();
            foreach (string uid in uids) {
                uidJArray.Add(uid);
            }

            msg.Add("uid", uidJArray);

            if (data != null) {
                msg.Add("data", data);
            }

            if (score_string != null) {
                msg.Add("score_string", score_string);
            }

            wsListener.Message(msg);
        }

        /// <summary>
        /// Gets thrown when you call an API method before OnReady was called.
        /// </summary>
        public class NotReadyException : SystemException { }

        /// <summary>
        /// Requests persistent data from the servers.
        /// Will call onPersistentDataLoaded when data was received.
        /// </summary>
        /// <param name="uids">The uids for which you would like to request the persistent data.</param>
        public void RequestPersistentData(List<string> uids) {
            if (!IsAirConsoleUnityPluginReady()) {
                throw new NotReadyException();
            }

            if (uids == null) {
                throw new ArgumentNullException(nameof(uids));
            }

            if (uids.Count < 1) {
                throw new ArgumentException("uids must contain at least one uid");
            }

            JObject msg = new();
            msg.Add("action", "requestPersistentData");

            if (uids != null) {
                JArray uidJArray = new();
                foreach (string uid in uids) {
                    uidJArray.Add(uid);
                }

                msg.Add("uids", uidJArray);
            }


            wsListener.Message(msg);
        }

        /// <summary>
        /// Stores a key-value pair persistently on the AirConsole servers.
        /// Storage is per game. Total storage can not exceed 1 MB per game and uid.
        /// Will call onPersistentDataStored when the request is done.
        /// </summary>
        /// <param name="key">The key of the data entry.</param>
        /// <param name="value">The value of the data entry.</param>
        /// <param name="uid">The uid for which the data should be stored.</param>
        public void StorePersistentData(string key, JToken value, string uid) {
            if (!IsAirConsoleUnityPluginReady()) {
                throw new NotReadyException();
            }

            if (string.IsNullOrEmpty(uid)) {
                throw new ArgumentNullException(nameof(uid));
            }

            JObject msg = new();
            msg.Add("action", "storePersistentData");
            msg.Add("key", key);
            msg.Add("value", value);
            msg.Add("uid", uid);

            wsListener.Message(msg);
        }

        /// <summary>
        /// Returns true if the device is premium
        /// </summary>
        /// <param name="device_id">The device_id that should be checked. Only controllers can be premium. Default is this device.</param>
        public bool IsPremium(int device_id = -1) {
            if (!IsAirConsoleUnityPluginReady()) {
                throw new NotReadyException();
            }

            if (device_id == -1) {
                device_id = GetDeviceId();
            }

            if (GetDevice(device_id) != null) {
                try {
                    if (GetDevice(device_id)["premium"] != null) {
                        return (bool)GetDevice(device_id)["premium"];
                    }

                    return false;
                } catch (Exception) {
                    return false;
                }
            }

            if (Settings.debug.warning) {
                AirConsoleLogger.LogWarning(() => "AirConsole: IsPremium: device_id " + device_id + " not found");
            }

            return false;
        }

        /// <summary>
        /// Returns all device Ids that are premium.
        /// </summary>
        public List<int> GetPremiumDeviceIds() {
            if (!IsAirConsoleUnityPluginReady()) {
                throw new NotReadyException();
            }

            List<int> result = new();

            List<int> allControllers = GetControllerDeviceIds();
            for (int i = 0; i < allControllers.Count; ++i) {
                if (IsPremium(allControllers[i])) {
                    result.Add(allControllers[i]);
                }
            }

            return result;
        }

        /// <summary>
        /// Sets the immersive state of the AirConsole game based on the provided options.
        /// </summary>
        /// <param name="payload">
        /// A JObject that may include the following property:
        /// <list type="bullet">
        ///     <item>
        ///         <description>
        ///             <c>light</c>: An optional object that contains:
        ///             <list type="bullet">
        ///                 <item><description><c>r</c>: An integer (or ubyte) value in the range [0, 255].</description></item>
        ///                 <item><description><c>g</c>: An integer (or ubyte) value in the range [0, 255].</description></item>
        ///                 <item><description><c>b</c>: An integer (or ubyte) value in the range [0, 255].</description></item>
        ///             </list>
        ///         </description>
        ///     </item>
        /// </list>
        /// </param>
        /// <exception cref="NotReadyException">Thrown if the AirConsole Unity Plugin is not ready.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown if the <c>light</c> object is provided but any of its fields (<c>r</c>, <c>g</c>, or <c>b</c>) are missing.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if any of the light values are outside the valid range [0, 255].
        /// </exception>
        public void SetImmersiveState(JObject payload) {
            if (!IsAirConsoleUnityPluginReady()) {
                throw new NotReadyException();
            }

            if (!payload.HasValues) {
                return;
            }

            JToken light = payload["light"];
            if (light != null) {
                if (light["r"] == null || light["g"] == null || light["b"] == null) {
                    throw new ArgumentException("The 'light' object must contain fields 'r', 'g', and 'b'.");
                }

                int r = (int)light["r"];
                int g = (int)light["g"];
                int b = (int)light["b"];

                if (r < 0 || r > 255 || g < 0 || g > 255 || b < 0 || b > 255) {
                    throw new ArgumentOutOfRangeException("light values must be in the range [0, 255]");
                }
            }


            JObject msg = new() {
                { "action", "setImmersiveState" },
                { "state", payload }
            };
            wsListener.Message(msg);
        }

        // ReSharper enable MemberCanBePrivate.Global UnusedMember.Global
        #endregion

        #region unity functions
        protected void Awake() {
            if (instance != this) {
                Destroy(gameObject);
            }

            // Always set default object name
            // Critical for unity webgl communication
            gameObject.name = "AirConsole";

            if (IsAndroidRuntime) {
                AirConsoleLogger.Log(() =>
                    $"Launching build {Application.version} in Unity v{Application.unityVersion}");

                defaultScreenHeight = Screen.height;
                _pluginManager = new PluginManager(this);
                _pluginManager.OnUpdateVolume += HandleOnMaxVolumeChanged;
                _pluginManager.OnAudioFocusChange += HandleNativeAudioFocusChanged;
            }
        }

        protected void Start() {
            if (Application.isEditor) {
                _runtimeConfigurator = new EditorRuntimeConfigurator();
            } else if (IsAndroidRuntime) {
                _runtimeConfigurator = new AndroidRuntimeConfigurator();
            } else {
                _runtimeConfigurator = new WebGLRuntimeConfigurator();
            }

            if (IsAndroidOrEditor) {
                InitWebView();

                SceneManager.sceneLoaded += OnAndroidSceneLoaded;
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
            } else {
                InitWebSockets();
            }
        }

        private void InitWebSockets() {
            if (IsAndroidOrEditor) {
                wsListener = new WebsocketListener(webViewObject);
                wsListener.onLaunchApp += OnLaunchApp;
                wsListener.onUnityWebviewResize += OnUnityWebviewResize;
                wsListener.onUnityWebviewPlatformReady += OnUnityWebviewPlatformReady;
                wsListener.OnUpdateContentProvider += OnUpdateContentProvider;
                wsListener.OnPlatformReady += HandlePlatformReady;
            } else {
                wsListener = new WebsocketListener();
            }

            wsListener.OnSetSafeArea += OnSetSafeArea;
            wsListener.onReady += OnReady;
            wsListener.onClose += OnClose;
            wsListener.onMessage += OnMessage;
            wsListener.onDeviceStateChange += OnDeviceStateChange;
            wsListener.onConnect += OnConnect;
            wsListener.onDisconnect += OnDisconnect;
            wsListener.onCustomDeviceStateChange += OnCustomDeviceStateChange;
            wsListener.onDeviceProfileChange += OnDeviceProfileChange;
            wsListener.onAdShow += OnAdShow;
            wsListener.onAdComplete += OnAdComplete;
            wsListener.onGameEnd += OnGameEnd;
            wsListener.onHighScores += OnHighScores;
            wsListener.onHighScoreStored += OnHighScoreStored;
            wsListener.onPersistentDataStored += OnPersistentDataStored;
            wsListener.onPersistentDataLoaded += OnPersistentDataLoaded;
            wsListener.onPremium += OnPremium;
            wsListener.onPause += OnPause;
            wsListener.onResume += OnResume;

            if (Application.isEditor) {
                // Start websocket and connection
                wsServer = new WebSocketServer(Settings.webSocketPort);
                wsServer.AddWebSocketService(Settings.WEBSOCKET_PATH, () => wsListener);
                wsServer.Start();

                if (Settings.debug.info) {
                    AirConsoleLogger.Log(() => "AirConsole: Dev-Server started!");
                }
            } else {
                if (Application.platform == RuntimePlatform.WebGLPlayer) {
                    // Call external javascript init function
                    Application.ExternalCall("onGameReady", autoScaleCanvas);
                }
            }
        }

        private void OnSetSafeArea(JObject msg) {
            SetSafeArea(msg);
        }

        private void HandlePlatformReady(JObject msg) {
            AirConsoleLogger.LogDevelopment(() => $"HandlePlatformReady: {msg}");

            _pluginManager?.ReportPlatformReady();
        }

        internal void SetSafeArea(JObject msg) {
            JObject safeAreaObj = msg.SelectToken("safeArea")?.Value<JObject>();
            if (safeAreaObj == null) {
                throw new UnityException(
                    $"OnSetSafeArea called without safeArea property in the message: {msg.ToString()}");
            }

            eventQueue.Enqueue(delegate {
                _lastSafeAreaParameters = safeAreaObj;
                RefreshSafeArea(_lastSafeAreaParameters);
            });
        }

        private void RefreshSafeArea(JObject safeAreaObj) {
            float heightValue = GetFloatFromMessage(safeAreaObj, "height", 1);
            float y = Screen.height * (1 - GetFloatFromMessage(safeAreaObj, "top", 0) - heightValue);
            float x = Screen.width * GetFloatFromMessage(safeAreaObj, "left", 0);
            float height = Screen.height * heightValue;
            float width = Screen.width * GetFloatFromMessage(safeAreaObj, "width", 1);

            Rect safeArea = new() {
                y = y,
                height = height,
                x = x,
                width = width
            };
            SafeArea = safeArea;

            if (androidUIResizeMode is AndroidUIResizeMode.ResizeCamera
                    or AndroidUIResizeMode.ResizeCameraAndReferenceResolution
                && Camera.main) {
                AirConsoleLogger.LogDevelopment(() =>
                    $"Original pixelRect {Camera.main.pixelRect}, new pixelRect {safeArea}");

                Camera.main.pixelRect = safeArea;
            }

            _safeAreaWasSet = true;
            _webViewManager.ActivateSafeArea();

            AirConsoleLogger.LogDevelopment(() =>
                $"Safe Area is {safeArea} from message {safeAreaObj}. Camera pixelRect is {safeArea} of {Screen.width}x{Screen.height}");
            OnSafeAreaChanged?.Invoke(SafeArea);
        }

        protected void Update() {
            ProcessEvents();

            _runtimeConfigurator?.RefreshConfiguration();

            if (IsAndroidRuntime) {
                // Back button on TV remotes
                if (Input.GetKeyDown(KeyCode.Escape)) {
                    Application.Quit();
                }
            }
        }

        protected void LateUpdate() => ProcessEvents();

        protected void FixedUpdate() => ProcessEvents();

        private void ProcessEvents() {
            while (eventQueue.Count > 0) {
                eventQueue.Dequeue().Invoke();
            }
        }

        private void OnApplicationQuit() {
            StopWebsocketServer();
        }

        private void OnDisable() {
            StopWebsocketServer();
        }

        private void OnDestroy() {
            UnityDestroy?.Invoke();
        }

        private void OnApplicationPause(bool pauseStatus) {
            if (pauseStatus) {
                UnityPause?.Invoke();
            } else {
                UnityResume?.Invoke();
            }
        }

        public static T ACFindObjectOfType<T>() where T : UnityEngine.Object {
#if !UNITY_6000_0_OR_NEWER
            return FindObjectOfType<T>();
#else
            return FindFirstObjectByType<T>();
#endif
        }
        #endregion

        #region internal functions
        private void OnDeviceStateChange(JObject msg) {
            if (msg["device_id"] == null) {
                return;
            }

            try {
                int deviceId = (int)msg["device_id"];
                JToken deviceData = msg["device_data"];

                // Queue all _devices modifications to run on the Unity main thread to avoid race conditions
                eventQueue.Enqueue(delegate() {
                    AllocateDeviceSlots(deviceId);
                    if (deviceData != null && deviceData.HasValues) {
                        _devices[deviceId] = deviceData;
                    } else {
                        _devices[deviceId] = null;
                    }

                    if (onDeviceStateChange != null) {
                        onDeviceStateChange(deviceId, GetDevice(_device_id));
                    }

                    if (Settings.debug.info) {
                        AirConsoleLogger.Log(() => $"AirConsole: saved devicestate of {deviceId}");
                    }
                });
            } catch (Exception e) {
                if (Settings.debug.error) {
                    AirConsoleLogger.LogError(() => e.Message);
                }
            }
        }

        private void OnConnect(JObject msg) {
            if (msg["device_id"] == null) {
                return;
            }

            try {
                int deviceId = (int)msg["device_id"];

                if (onConnect != null) {
                    eventQueue.Enqueue(delegate() {
                        if (onConnect != null) {
                            onConnect(deviceId);
                        }
                    });
                }

                if (Settings.debug.info) {
                    AirConsoleLogger.Log(() => $"AirConsole: onConnect {deviceId}");
                }
            } catch (Exception e) {
                if (Settings.debug.error) {
                    AirConsoleLogger.LogError(() => e.Message);
                }
            }
        }

        private void OnDisconnect(JObject msg) {
            if (msg["device_id"] == null) {
                return;
            }

            try {
                int deviceId = (int)msg["device_id"];

                if (onDisconnect != null) {
                    eventQueue.Enqueue(delegate() {
                        if (onDisconnect != null) {
                            onDisconnect(deviceId);
                        }
                    });
                }

                if (Settings.debug.info) {
                    AirConsoleLogger.Log(() => $"AirConsole: onDisconnect {deviceId}");
                }
            } catch (Exception e) {
                if (Settings.debug.error) {
                    AirConsoleLogger.LogError(() => e.Message);
                }
            }
        }

        private void OnCustomDeviceStateChange(JObject msg) {
            if (msg["device_id"] == null) {
                return;
            }

            try {
                int deviceId = (int)msg["device_id"];

                if (onCustomDeviceStateChange != null) {
                    eventQueue.Enqueue(delegate() {
                        if (onCustomDeviceStateChange != null) {
                            onCustomDeviceStateChange(deviceId, GetCustomDeviceState(deviceId));
                        }
                    });
                }

                if (Settings.debug.info) {
                    AirConsoleLogger.Log(() => $"AirConsole: onCustomDeviceStateChange {deviceId}");
                }
            } catch (Exception e) {
                if (Settings.debug.error) {
                    AirConsoleLogger.LogError(() => e.Message);
                }
            }
        }

        private void OnMessage(JObject msg) {
            if (onMessage != null) {
                eventQueue.Enqueue(delegate() {
                    if (onMessage != null) {
                        onMessage((int)msg["from"], (JToken)msg["data"]);
                    }
                });
            }
        }

        private void RequestAudioFocus() {
            if (!_canHaveAudioFocus) {
                AirConsoleLogger.Log(() => "AirConsole: Not requesting audio focus because the app is not allowed.");
                return;
            }

            if (IsAndroidRuntime) {
                HasAudioFocus = _pluginManager.RequestAudioFocus();
            } else {
                HasAudioFocus = true;
            }

            if (!HasAudioFocus) {
                AirConsoleLogger.LogError(() =>
                    "AirConsole: Failed to obtain audio focus. Audio may not work as expected. Should pause platform");
            }
        }

        private void AbandonAudioFocus() {
            HasAudioFocus = false;
            if (IsAndroidRuntime) {
                _pluginManager.AbandonAudioFocus();
            }
        }

        private bool _firstReady = true;

        // TODO(QAB-14400, QAB-14401): This does not yet work correctly - when going to web, we lose audio focus and due to that
        //  we do not regain it when coming back from web in OnReady. We need to distinguish between the two paths
        private void OnReady(JObject msg) {
            _ignoreAudioFocusLoss = false;
            if (_firstReady) {
                _firstReady = false;
                _canHaveAudioFocus = true;
            }

            RequestAudioFocus();
            if (Application.platform == RuntimePlatform.Android) {
                // Android based games must respect the volume change requests so we can correctly handle Android AudioFocus behavior as
                //  demanded by Automotive on one side and Android 33+ on the other side.
                if (OnGameAudioFocusChanged == null || OnGameAudioFocusChanged.GetInvocationList().Length == 0) {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                    throw new Exception("No listeners registered to OnGameAudioFocusChanged. Editor playback stopped.");
#else
                    throw new Exception("No listeners registered to OnGameAudioFocusChanged.");
#endif
                }

                if (!Application.isEditor) {
                    webViewObject.AbandonUnityAudioFocus();

                    // TODO(PRO-1637): Implement the necessary pieces to properly handle AudioFocus changes on Android in conjunction with
                    //  WebView and the AirConsole Unity Plugin.
                }
            } else {
                if (OnGameAudioFocusChanged != null && OnGameAudioFocusChanged.GetInvocationList().Length > 0) {
                    AirConsoleLogger.Log(() =>
                        "Registration to event OnGameAudioFocusChanged identified. This will only be called when running on Android devices.");
                }
            }

            // Queue all state modifications to run on the Unity main thread to avoid race conditions
            eventQueue.Enqueue(delegate() {
                if (webViewLoadingCanvas) {
                    Destroy(webViewLoadingCanvas.gameObject);
                }

                // parse server_time_offset
                _server_time_offset = (int)msg["server_time_offset"];

                // parse device_id
                _device_id = (int)msg["device_id"];

                // parse location
                _location = (string)msg["location"];

                if (msg["translations"] != null) {
                    _translations = new Dictionary<string, string>();
                    JObject translationObject = (JObject)msg["translations"];
                    if (translationObject != null) {
                        foreach (KeyValuePair<string, JToken> keyValue in translationObject) {
                            string value = (string)keyValue.Value;
                            value = value.Replace("\\n", "\n");
                            value = value.Replace("&lt;", "<");
                            value = value.Replace("&gt;", ">");
                            _translations.Add(keyValue.Key, value);
                        }
                    }
                }


                // load devices
                _devices.Clear();
                foreach (JToken data in (JToken)msg["devices"]) {
                    JToken assign = data;
                    if (data != null && !data.HasValues) {
                        assign = null;
                    }

                    _devices.Add(assign);
                }

                _receivedReady = true;

                if (onReady != null) {
                    onReady((string)msg["code"]);
                }
            });
        }

        private void OnDeviceProfileChange(JObject msg) {
            if (msg["device_id"] == null) {
                return;
            }

            try {
                int deviceId = (int)msg["device_id"];

                if (onDeviceProfileChange != null) {
                    eventQueue.Enqueue(delegate() {
                        if (onDeviceProfileChange != null) {
                            onDeviceProfileChange(deviceId);
                        }
                    });
                }

                if (Settings.debug.info) {
                    AirConsoleLogger.Log(() => $"AirConsole: onDeviceProfileChange {deviceId}");
                }
            } catch (Exception e) {
                if (Settings.debug.error) {
                    AirConsoleLogger.LogError(() => e.Message);
                }
            }
        }

        private void OnAdShow(JObject msg) {
            _webViewManager.RequestStateTransition(WebViewManager.WebViewState.FullScreen);

            try {
                if (onAdShow != null) {
                    eventQueue.Enqueue(delegate() {
                        if (onAdShow != null) {
                            onAdShow();
                        }
                    });
                }

                if (Settings.debug.info) {
                    AirConsoleLogger.Log(() => "AirConsole: onAdShow");
                }
            } catch (Exception e) {
                if (Settings.debug.error) {
                    AirConsoleLogger.LogError(() => e.Message);
                }
            }
        }

        private void OnAdComplete(JObject msg) {
            _webViewManager.RequestStateTransition(WebViewManager.WebViewState.TopBar);

            try {
                bool adWasShown = (bool)msg["ad_was_shown"];

                if (onAdComplete != null) {
                    eventQueue.Enqueue(delegate() {
                        if (onAdComplete != null) {
                            onAdComplete(adWasShown);
                        }
                    });
                }

                if (Settings.debug.info) {
                    AirConsoleLogger.Log(() => "AirConsole: onAdComplete");
                }
            } catch (Exception e) {
                if (Settings.debug.error) {
                    AirConsoleLogger.LogError(() => e.Message);
                }
            }
        }

        /// <summary>
        /// Resets the caches.
        /// </summary>
        /// <param name="taskToQueueAfterClear">Optional task to queue after the eventQueue gets cleared.</param>
        private void ResetCaches(Action taskToQueueAfterClear) {
            AirConsoleLogger.LogDevelopment(() => "Resetting AirConsole caches");

            // Clear device and player data
            _devices.Clear();
            _players.Clear();

            // Reset state variables
            _device_id = 0;
            _server_time_offset = 0;
            _location = null;
            _translations = null;
            _receivedReady = false;

            // Reset safe area
            _safeAreaWasSet = false;
            _lastSafeAreaParameters = null;
            SafeArea = new Rect(0, 0, Screen.width, Screen.height);

            // Clear event queue
            eventQueue.Clear();
            if (taskToQueueAfterClear != null) {
                eventQueue.Enqueue(taskToQueueAfterClear);
            }

            AirConsoleLogger.LogDevelopment(() => "AirConsole caches reset complete");
        }

        private void UnsubscribeWebSocketEvents() {
            // Unsubscribe all event handlers to prevent stale events
            wsListener.OnSetSafeArea -= OnSetSafeArea;
            wsListener.onReady -= OnReady;
            wsListener.onClose -= OnClose;
            wsListener.onMessage -= OnMessage;
            wsListener.onDeviceStateChange -= OnDeviceStateChange;
            wsListener.onConnect -= OnConnect;
            wsListener.onDisconnect -= OnDisconnect;
            wsListener.onCustomDeviceStateChange -= OnCustomDeviceStateChange;
            wsListener.onDeviceProfileChange -= OnDeviceProfileChange;
            wsListener.onAdShow -= OnAdShow;
            wsListener.onAdComplete -= OnAdComplete;
            wsListener.onGameEnd -= OnGameEnd;
            wsListener.onHighScores -= OnHighScores;
            wsListener.onHighScoreStored -= OnHighScoreStored;
            wsListener.onPersistentDataStored -= OnPersistentDataStored;
            wsListener.onPersistentDataLoaded -= OnPersistentDataLoaded;
            wsListener.onPremium -= OnPremium;
            wsListener.onPause -= OnPause;
            wsListener.onResume -= OnResume;

            if (IsAndroidOrEditor) {
                wsListener.onLaunchApp -= OnLaunchApp;
                wsListener.onUnityWebviewResize -= OnUnityWebviewResize;
                wsListener.onUnityWebviewPlatformReady -= OnUnityWebviewPlatformReady;
                wsListener.OnUpdateContentProvider -= OnUpdateContentProvider;
                wsListener.OnPlatformReady -= HandlePlatformReady;
            }
        }

        private void CleanupWebSocketListener() {
            AirConsoleLogger.LogDevelopment(() => "Cleaning up WebSocket listener");

            if (wsListener != null) {
                // Unsubscribe all event handlers to prevent stale events
                UnsubscribeWebSocketEvents();
                wsListener = null;
            }

            // Stop websocket server if in editor
            StopWebsocketServer();

            AirConsoleLogger.LogDevelopment(() => "WebSocket listener cleanup complete");
        }

        private void ReloadWebView() {
            if (!_receivedReady) {
                AirConsoleLogger.LogDevelopment(() => "Skipping reload. We have not yet left the Player Lobby.");
                return;
            }

            if (string.IsNullOrEmpty(_webViewOriginalUrl) || string.IsNullOrEmpty(_webViewConnectionUrl)) {
                List<string> missingComponents = new();
                if (string.IsNullOrEmpty(_webViewOriginalUrl)) {
                    missingComponents.Add("original URL");
                }

                if (string.IsNullOrEmpty(_webViewConnectionUrl)) {
                    missingComponents.Add("connection URL");
                }

                string missing = string.Join(" and ", missingComponents);
                AirConsoleLogger.LogDevelopment(() => $"Cannot recreate webview - missing {missing}");
                return;
            }

            if (webViewObject) {
                AirConsoleLogger.LogDevelopment(() => $"Reloading webview with URL: {_webViewOriginalUrl}");
                LoadAndroidWebviewUrl(_webViewOriginalUrl);
            } else {
                AirConsoleLogger.LogError(() => "Reloading webview failed, no webViewObject found.");
            }
        }

        private void OnGameEnd(JObject msg) {
            _ignoreAudioFocusLoss = true;
            AbandonAudioFocus();
            _webViewManager.RequestStateTransition(WebViewManager.WebViewState.FullScreen);

            try {
                if (onGameEnd != null) {
                    eventQueue.Enqueue(delegate() {
                        if (onGameEnd != null) {
                            onGameEnd();
                        }
                    });
                }

                if (Settings.debug.info) {
                    AirConsoleLogger.Log(() => "AirConsole: onGameEnd");
                }

                // Reset all caches and recreate webview on the main thread
                eventQueue.Enqueue(delegate {
                    // We want to chain RecreateWebView to ensure it happens independent of
                    //  the eventQueue getting cleared and related side effects.
                    ResetCaches(ReloadWebView);
                });
            } catch (Exception e) {
                if (Settings.debug.error) {
                    AirConsoleLogger.LogError(() => e.Message);
                }
            }
        }

        private void OnHighScores(JObject msg) {
            try {
                JToken highscores = msg["highscores"];

                if (onHighScores != null) {
                    eventQueue.Enqueue(delegate() {
                        if (onHighScores != null) {
                            onHighScores(highscores);
                        }
                    });
                }

                if (Settings.debug.info) {
                    AirConsoleLogger.Log(() => "AirConsole: onHighScores");
                }
            } catch (Exception e) {
                if (Settings.debug.error) {
                    AirConsoleLogger.LogError(() => e.Message);
                }
            }
        }

        private void OnHighScoreStored(JObject msg) {
            try {
                JToken highscore = msg["highscore"];

                if (highscore != null && !highscore.HasValues) {
                    highscore = null;
                }

                if (onHighScoreStored != null) {
                    eventQueue.Enqueue(delegate() {
                        if (onHighScoreStored != null) {
                            onHighScoreStored(highscore);
                        }
                    });
                }

                if (Settings.debug.info) {
                    AirConsoleLogger.Log(() => "AirConsole: onHighScoreStored");
                }
            } catch (Exception e) {
                if (Settings.debug.error) {
                    AirConsoleLogger.LogError(() => e.Message);
                }
            }
        }

        private void OnPersistentDataStored(JObject msg) {
            try {
                string uid = (string)msg["uid"];

                if (onPersistentDataStored != null) {
                    eventQueue.Enqueue(delegate() {
                        if (onPersistentDataStored != null) {
                            onPersistentDataStored(uid);
                        }
                    });
                }

                if (Settings.debug.info) {
                    AirConsoleLogger.Log(() => "AirConsole: OnPersistentDataStored");
                }
            } catch (Exception e) {
                if (Settings.debug.error) {
                    AirConsoleLogger.LogError(() => e.Message);
                }
            }
        }

        private void OnPersistentDataLoaded(JObject msg) {
            try {
                JToken data = msg["data"];

                if (onPersistentDataLoaded != null) {
                    eventQueue.Enqueue(delegate() {
                        if (onPersistentDataLoaded != null) {
                            onPersistentDataLoaded(data);
                        }
                    });
                }

                if (Settings.debug.info) {
                    AirConsoleLogger.Log(() => "AirConsole: OnPersistentDataLoaded");
                }
            } catch (Exception e) {
                if (Settings.debug.error) {
                    AirConsoleLogger.LogError(() => e.Message);
                }
            }
        }

        private void OnPremium(JObject msg) {
            try {
                int device_id = (int)msg["device_id"];

                if (onPremium != null) {
                    eventQueue.Enqueue(delegate() {
                        if (onPremium != null) {
                            onPremium(device_id);
                        }
                    });
                }

                if (Settings.debug.info) {
                    AirConsoleLogger.Log(() => "AirConsole: onPremium");
                }
            } catch (Exception e) {
                if (Settings.debug.error) {
                    AirConsoleLogger.LogError(() => e.Message);
                }
            }
        }

        private void OnPause(JObject msg) {
            AbandonAudioFocus();

            try {
                if (onPause != null) {
                    eventQueue.Enqueue(delegate() {
                        if (onPause != null) {
                            onPause();
                        }
                    });
                }

                if (Settings.debug.info) {
                    AirConsoleLogger.Log(() => "AirConsole: onPause");
                }
            } catch (Exception e) {
                if (Settings.debug.error) {
                    AirConsoleLogger.LogError(() => e.Message);
                }
            }
        }

        private void OnResume(JObject msg) {
            // When we resume, we can have audio focus again.
            _canHaveAudioFocus = true;
            RequestAudioFocus();
            try {
                if (onResume != null) {
                    eventQueue.Enqueue(delegate() {
                        if (onResume != null) {
                            onResume();
                        }
                    });
                }

                if (Settings.debug.info) {
                    AirConsoleLogger.Log(() => "AirConsole: onResume");
                }
            } catch (Exception e) {
                if (Settings.debug.error) {
                    AirConsoleLogger.LogError(() => e.Message);
                }
            }
        }

        /// <summary>
        /// Provides access to the device data of all devices in the game.
        /// Use Devices[AirConsole.SCREEN]?["environment"] to access the environment information of the screen.
        /// </summary>
        public ReadOnlyCollection<JToken> Devices {
            get => _devices.AsReadOnly();
        }

        [Obsolete("GetActivePlayerDeviceIds has been replaced with ActivePlayerDeviceIds", true)]
        public ReadOnlyCollection<int> GetActivePlayerDeviceIds {
            get => _players.AsReadOnly();
        }

        /// <summary>
        /// Returns an array of device_ids of the active players previously set by the
        /// screen by calling setActivePlayers. The first device_id in the array is the
        /// first player, the second device_id in the array is the second player, ...
        /// </summary>
        public ReadOnlyCollection<int> ActivePlayerDeviceIds {
            get => _players.AsReadOnly();
        }

        /// <summary>
        /// Maximum Audio Volume allowed at this time.<br />
        /// A value of 0 indicates, that the game should be muted right now and all audio output should be paused.<br />
        /// A value of 1 indicates, that the game can play audio at full volume.<br />
        /// Values in between should be used to set the volume of the audio output.<br />
        /// This value can change at any time and you are expected to listen to OnMaximumVolumeChanged and adjust accordingly immediately.
        /// </summary>
        /// <value>1</value>
        public float MaximumAudioVolume { get; private set; } = 1;

        /// <summary>
        /// Does the game currently have audio focus?<br />
        /// If false, the game should not play any audio at all.<br />
        /// If true, the game can play audio, respecting the MaximumAudioVolume property.<br />
        /// This value can change at any time and you are expected to listen to OnGameAudioFocusChanged and adjust accordingly immediately.
        /// </summary>
        public bool HasAudioFocus { get; private set; } = true;

        /// <summary>
        /// True, if this is the Android platform running on the device, not the editor.
        /// </summary>
        internal static bool IsAndroidRuntime {
            get => Application.platform == RuntimePlatform.Android;
        }

        /// <summary>
        /// True, if this is the android platform running on the device or the Unity editor.
        /// </summary>
        internal static bool IsAndroidOrEditor {
            get => Application.platform == RuntimePlatform.Android || Application.isEditor;
        }

        /// <summary>
        /// The currently valid safe area in camera coordinates. Valid pixelRect for cameras to render in.
        /// </summary>
        /// <remarks>Can be directly assigned to the camera.pixelRect</remarks>
        public Rect SafeArea { get; private set; } = new(0, 0, Screen.width, Screen.height);

        private WebSocketServer wsServer;
        private WebsocketListener wsListener;

        private WebViewObject webViewObject;
        private Canvas webViewLoadingCanvas;
        private UnityEngine.UI.Image webViewLoadingImage;
        private UnityEngine.UI.Image webViewLoadingBG;
        private int webViewHeight;
        private int defaultScreenHeight;
        private List<UnityEngine.UI.CanvasScaler> fixedCanvasScalers = new();

        private Action _reloadWebviewHandler;
        private PluginManager _pluginManager;

        private bool _receivedReady = false;
        private List<JToken> _devices = new();
        private int _device_id;
        private int _server_time_offset;
        private string _location;
        private Dictionary<string, string> _translations;
        private readonly List<int> _players = new();
        private readonly Queue<Action> eventQueue = new();
        private bool _safeAreaWasSet;
        private JObject _lastSafeAreaParameters;
        private WebViewManager _webViewManager;
        private bool _logPlatformMessages;
        private string _webViewConnectionUrl;
        private string _webViewOriginalUrl;

        private IRuntimeConfigurator _runtimeConfigurator;

        private static AirConsole _instance;

        private void StopWebsocketServer() {
            if (wsServer == null) {
                return;
            }

            // Unregister event handlers before stopping to prevent race conditions
            UnsubscribeWebSocketEvents();

            wsServer.Stop();
            wsServer = null;
        }

        private void OnClose() {
            // Queue the clear operation to run on the Unity main thread to avoid race conditions
            eventQueue.Enqueue(delegate() { _devices.Clear(); });
        }

        public static string GetUrl(StartMode mode) {
            bool isHttps = !Application.isEditor
                           || (!string.IsNullOrEmpty(instance.LocalIpOverride)
                               && instance.LocalIpOverride.StartsWith("https://"));
            string url = isHttps ? Settings.AIRCONSOLE_DEV_URL_HTTPS : Settings.AIRCONSOLE_DEV_URL_HTTP;
            if (mode == StartMode.VirtualControllers || mode == StartMode.DebugVirtualControllers) {
                url += "simulator/";
            } else if (!isHttps) {
                url += "?http=1";
            }

            if (Application.isEditor) {
                bool hasDevGameId = instance.devGameId.Length > 0;
                bool hasDevLanguage = instance.devLanguage.Length > 0;
                if (hasDevGameId) {
                    url += "?dev-game-id=" + instance.devGameId;

                    if (hasDevLanguage) {
                        url += $"&language={instance.devLanguage}";
                    }
                }
            }

            url += "#";

            if (mode == StartMode.Debug || mode == StartMode.DebugVirtualControllers) {
                url += "debug:";
            }

            return url;
        }

        public void ProcessJS(string data) {
            if (_logPlatformMessages) {
                AirConsoleLogger.LogError(() => $"PlatformMessage: {data}");
            }

            wsListener.ProcessMessage(data);
#if UNITY_WEBGL && !UNITY_EDITOR
            // On WebGL we need to process events immediately to ensure pause / resume works correctly.
            ProcessEvents();
#endif
        }

        private JToken GetDevice(int deviceId) {
            if (deviceId < _devices.Count && deviceId >= 0) {
                return _devices[deviceId];
            }

            return null;
        }

        private string GetGameUrl(string url) {
            if (url == null) {
                return null;
            }

            url = url.Split('#')[0];
            url = url.Split('?')[0];
            if (url.EndsWith("screen.html")) {
                url = url.Substring(0, url.Length - 11);
            } else if (url.EndsWith("controller.html")) {
                url = url.Substring(0, url.Length - 15);
            }

            if (url.StartsWith("https://")) {
                url = "http://" + url.Substring(8);
            }

            return url;
        }

        private void AllocateDeviceSlots(int i) {
            while (i >= _devices.Count) {
                _devices.Add(null);
            }
        }

        private int GetScaledWebViewHeight() => (int)((float)webViewHeight * Screen.height / defaultScreenHeight);

        private void OnConnectUrlReceived(string connectionUrl) {
            _pluginManager.OnConnectionUrlReceived -= OnConnectUrlReceived;
            eventQueue.Enqueue(delegate {
                CreateAndroidWebview(connectionUrl);
            });
        }

        private string ComputeUrlVersion(string version) {
            string[] split = version.Split('.');
            return $"{split[0]}.{split[1]}{split[2]}";
        }

        private void InitWebView() {
            AirConsoleLogger.LogDevelopment(() => $"InitWebView: {androidGameVersion}");

            if (!string.IsNullOrEmpty(androidGameVersion)) {
                PrepareWebviewOverlay();
                if (Application.isEditor) {
                    string connectionUrl
                        = $"client?id=androidunity-{ComputeUrlVersion(Settings.VERSION)}&runtimePlatform=android";
                    CreateAndroidWebview(connectionUrl);
                } else if (IsAndroidRuntime) {
                    AirConsoleLogger.LogDevelopment(() =>
                        $"IsTvDevice: {_pluginManager.IsTV()}, IsAutomotiveDevice: {_pluginManager.IsAutomotive()}, IsNormalDevice: {_pluginManager.IsNormalDevice()}");

                    if (_pluginManager.IsInitialized) {
                        string connectionUrl = _pluginManager.ConnectionUrl;
                        AirConsoleLogger.LogDevelopment(() =>
                            $"InitWebView: DataProviderInitialized, use connection url {connectionUrl}");

                        CreateAndroidWebview(connectionUrl);
                    } else {
                        AirConsoleLogger.LogDevelopment(() =>
                            $"InitWebView: DataProvider not initialized, register for OnConnectUrlReceived");

                        _pluginManager.OnConnectionUrlReceived += OnConnectUrlReceived;
                    }
                }
            } else {
                AirConsoleLogger.LogDevelopment(() => "InitWebView: No androidGameVersion set");

                if (Settings.debug.error) {
                    AirConsoleLogger.LogError(() =>
                        "AirConsole: for Android builds you need to provide the Game Version Identifier on the AirConsole object in the scene.");
                }
            }
        }

        [Conditional("AIRCONSOLE_ANDROID_RUNTIME")]
        private void PrepareWebviewOverlay() {
            webViewLoadingCanvas = new GameObject("WebViewLoadingCanvas").AddComponent<Canvas>();
            webViewLoadingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            webViewLoadingBG = new GameObject("WebViewLoadingBG").AddComponent<UnityEngine.UI.Image>();
            webViewLoadingBG.color = Color.black;
            webViewLoadingBG.transform.SetParent(webViewLoadingCanvas.transform, true);
            webViewLoadingBG.rectTransform.localPosition = new Vector3(0, 0, 0);
            webViewLoadingBG.rectTransform.sizeDelta = new Vector2(Screen.width, Screen.height);

            webViewLoadingImage = new GameObject("WebViewLoadingImage").AddComponent<UnityEngine.UI.Image>();
            webViewLoadingImage.transform.SetParent(webViewLoadingCanvas.transform, true);
            webViewLoadingImage.sprite = webViewLoadingSprite;
            webViewLoadingImage.rectTransform.localPosition = new Vector3(0, 0, 0);
            if (_pluginManager != null && _pluginManager.IsAutomotive()) {
                webViewLoadingImage.rectTransform.sizeDelta = new Vector2(Screen.width, Screen.height);
            } else {
                webViewLoadingImage.rectTransform.sizeDelta = new Vector2(Screen.width / 2, Screen.height / 2);
            }

            webViewLoadingImage.preserveAspect = true;
            if (!webViewLoadingSprite) {
                webViewLoadingImage.sprite = Resources.Load("androidtv-loadingscreen", typeof(Sprite)) as Sprite;
            }
        }

        private void CreateAndroidWebview(string connectionUrl) {
            AirConsoleLogger.LogDevelopment(() => $"CreateAndroidWebview with connection url {connectionUrl}");
            if (!webViewObject) {
                _webViewConnectionUrl = connectionUrl;

                webViewObject = new GameObject("WebViewObject").AddComponent<WebViewObject>();
                if (Application.isPlaying) {
                    DontDestroyOnLoad(webViewObject.gameObject);
                }

                webViewObject.SetOnAudioFocusChanged(HandleWebViewAudioFocusChanged);

                webViewObject.Init(ProcessJS,
                    err => AirConsoleLogger.LogDevelopment(() => $"AirConsole WebView error: {err}"),
                    httpError => AirConsoleLogger.LogDevelopment(() => $"AirConsole WebView HttpError: {httpError}"),
                    loadedUrl => AirConsoleLogger.LogDevelopment(() => $"AirConsole WebView Loaded URL {loadedUrl}"),
                    started => AirConsoleLogger.LogDevelopment(() => $"AirConsole WebView started: {started}"),
                    hooked => AirConsoleLogger.LogDevelopment(() => $"AirConsole WebView hooked: {hooked}"),
                    cookies => AirConsoleLogger.LogDevelopment(() => $"AirConsole WebView cookies: {cookies}"),
                    true, false);

                if (IsAndroidRuntime && _pluginManager != null) {
                    _pluginManager.OnReloadWebview += () => webViewObject.Reload();
                    _pluginManager.InitializeOfflineCheck();
                }

                string url;
                if (IsAndroidRuntime) {
                    string urlOverride = AndroidIntentUtils.GetIntentExtraString("base_url", string.Empty);
                    url = !string.IsNullOrEmpty(urlOverride) ? urlOverride : Settings.AIRCONSOLE_BASE_URL;
                    AirConsoleLogger.LogDevelopment(() => $"BaseURL Override: {urlOverride}");
                } else {
                    url = Settings.AIRCONSOLE_BASE_URL;
                }

                url += connectionUrl;
                if (IsAndroidRuntime) {
                    url += $"&bundle-version={GetAndroidBundleVersionCode()}";
                }

                androidGameVersion = AndroidIntentUtils.GetIntentExtraString("game_version", androidGameVersion);

                url += "&game-id=" + Application.identifier;
                url += "&game-version=" + androidGameVersion;
                url += "&unity-version=" + Application.unityVersion;
                bool nativeSizingSupported = ResolveNativeGameSizingSupport(nativeGameSizingSupported);
                url += nativeSizingSupported ? "&supportsNativeGameSizing=true" : "&supportsNativeGameSizing=false";

                defaultScreenHeight = Screen.height;
                _webViewOriginalUrl = url;
                _webViewManager = new WebViewManager(webViewObject, defaultScreenHeight);

                LoadAndroidWebviewUrl(url);

                _logPlatformMessages = AndroidIntentUtils.GetIntentExtraBool("log_platform_messages", false);
                InitWebSockets();
            }
        }

        private void LoadAndroidWebviewUrl(string url) {
            webViewObject.SetVisibility(!Application.isEditor);
            AirConsoleLogger.LogDevelopment(() => $"Initial URL: {url}");
            webViewObject.LoadURL(url);

            if (IsAndroidRuntime) {
                if (_pluginManager != null) {
                    if (_reloadWebviewHandler != null) {
                        _pluginManager.OnReloadWebview -= _reloadWebviewHandler;
                    }

                    _reloadWebviewHandler = () => webViewObject.LoadURL(url);
                    _pluginManager.OnReloadWebview += _reloadWebviewHandler;
                    _pluginManager.InitializeOfflineCheck();
                }

                bool isWebviewDebuggable = AndroidIntentUtils.GetIntentExtraBool("webview_debuggable", false);
                webViewObject.EnableWebviewDebugging(isWebviewDebuggable);
            }
        }

        private static bool ResolveNativeGameSizingSupport(bool fallback) {
            AirconsoleRuntimeSettings settings
                = Resources.Load<AirconsoleRuntimeSettings>(AirconsoleRuntimeSettings.ResourceName);
            return settings ? settings.NativeGameSizingSupported : fallback;
        }

        private static int GetAndroidBundleVersionCode() {
            AndroidJavaObject ca = UnityAndroidObjectProvider.GetUnityActivity();
            AndroidJavaObject packageManager = ca.Call<AndroidJavaObject>("getPackageManager");
            AndroidJavaObject pInfo
                = packageManager.Call<AndroidJavaObject>("getPackageInfo", Application.identifier, 0);

            return pInfo.Get<int>("versionCode");
        }

        private void OnLaunchApp(JObject msg) {
            string gameId = (string)msg["game_id"];
            string gameVersion = (string)msg["game_version"];
            AirConsoleLogger.LogDevelopment(() => $"OnLaunchApp for {msg} -> {gameId} -> {gameVersion}");

            if (gameId != Application.identifier || gameVersion != instance.androidGameVersion) {
                // Marshal to main thread since this is called from WebSocket background thread
                eventQueue.Enqueue(() => HandleLaunchAppTransition(msg, gameId, gameVersion));
            }
        }

        private void HandleLaunchAppTransition(JObject msg, string gameId, string gameVersion) {
            bool quitAfterLaunchIntent = false; // Flag used to force old pre v2.5 way of quitting

            if (msg["quit_after_launch_intent"] != null) {
                quitAfterLaunchIntent = msg.SelectToken("quit_after_launch_intent").Value<bool>();
            }

            // Quit the Unity Player first and give it the time to close all the threads (Default)
            if (!quitAfterLaunchIntent) {
                Application.Quit();
                if (_pluginManager == null || !_pluginManager.IsAutomotive()) {
                    int waitTime = _pluginManager != null && _pluginManager.IsAutomotive() ? 500 : 2000;
                    AirConsoleLogger.LogDevelopment(() => $"Quit and wait for {waitTime} milliseconds");
                    Thread.Sleep(waitTime);
                } else {
                    AirConsoleLogger.LogDevelopment(() => $"Quit immediately");
                }
            }

            if (IsAndroidRuntime) {
                LaunchNativeAirConsoleStore(msg, gameId, gameVersion);
            }

            // Quitting after launch intent was the pre v2.5 way
            if (quitAfterLaunchIntent) {
                AirConsoleLogger.LogDevelopment(() => $"Quit after launch intent");
                Application.Quit();
                return;
            }

            Thread.Sleep(_pluginManager != null && _pluginManager.IsAutomotive() ? 500 : 2000);
            FinishActivity();
        }

        private static void LaunchNativeAirConsoleStore(JObject msg, string gameId, string gameVersion) {
            // Start the main AirConsole App
            AndroidJavaObject ca = UnityAndroidObjectProvider.GetUnityActivity();
            AndroidJavaObject packageManager = ca.Call<AndroidJavaObject>("getPackageManager");
            AndroidJavaObject launchIntent = null;
            try {
                launchIntent = packageManager.Call<AndroidJavaObject>("getLeanbackLaunchIntentForPackage", gameId);
            } catch (Exception) {
                AirConsoleLogger.Log(() => "getLeanbackLaunchIntentForPackage for " + gameId + " failed");
            }

            if (launchIntent == null) {
                try {
                    launchIntent = packageManager.Call<AndroidJavaObject>("getLaunchIntentForPackage", gameId);
                } catch (Exception) {
                    AirConsoleLogger.Log(() => "getLaunchIntentForPackage for " + gameId + " failed");
                }
            }

            AirConsoleLogger.LogDevelopment(() =>
                $"OnLaunchApp for {msg}, launch intent: {launchIntent != null}, gameId: {gameId}, Application.identifier: {Application.identifier} gameVersion: {gameVersion}");
            if (launchIntent != null && gameId != Application.identifier) {
                ca.Call("startActivity", launchIntent);
            } else {
                Application.OpenURL("market://details?id=" + gameId);
            }

            packageManager.Dispose();
            launchIntent.Dispose();
        }

        private void FinishActivity() {
            if (!IsAndroidRuntime) {
                return;
            }

            AndroidJavaObject activity = UnityAndroidObjectProvider.GetUnityActivity();
            activity.Call("finish");
        }

        private void OnUnityWebviewResize(JObject msg) {
            AirConsoleLogger.LogDevelopment(() => $"OnUnityWebviewResize w/ msg {msg}");
            if (_devices.Count > 0) {
                AirConsoleLogger.Log(() => $"screen device data: {_devices[0]}");
            }

            int h = Screen.height;

            if (msg["top_bar_height"] != null) {
                h = (int)msg["top_bar_height"] * 2; // TODO(android-native): This should use the screen dpi scaling factor
                webViewHeight = h;
                _webViewManager.SetWebViewHeight(h);
            }

            if (webViewHeight > 0) {
                _webViewManager.RequestStateTransition(WebViewManager.WebViewState.TopBar);
            } else {
                _webViewManager.RequestStateTransition(WebViewManager.WebViewState.Hidden);
            }

            if (Camera.main
                && androidUIResizeMode is AndroidUIResizeMode.ResizeCamera
                    or AndroidUIResizeMode.ResizeCameraAndReferenceResolution) {
                Camera.main.pixelRect = GetCameraPixelRect();
            }
        }

        private Rect GetCameraPixelRect() {
            if (IsAndroidOrEditor) {
                if (_safeAreaWasSet) {
                    return SafeArea;
                }

                return new Rect(0, 0, Screen.width, Screen.height - GetScaledWebViewHeight());
            }

            return Camera.main ? Camera.main.pixelRect : new Rect(0, 0, Screen.width, Screen.height);
        }

        private void OnUnityWebviewPlatformReady(JObject msg) {
            AirConsoleLogger.LogDevelopment(() => $"OnUnityWebviewPlatformReady {msg}");
            _webViewManager.RequestStateTransition(WebViewManager.WebViewState.FullScreen);
        }

        private void OnAndroidSceneLoaded(Scene scene, LoadSceneMode sceneMode) {
            if (instance != this || !Camera.main) {
                return;
            }

            if (androidUIResizeMode is AndroidUIResizeMode.ResizeCamera
                or AndroidUIResizeMode.ResizeCameraAndReferenceResolution) {
                Camera.main.pixelRect = GetCameraPixelRect();
            }

            if (!IsAutomotive()) {
                AdaptUGuiLayout();
            }
        }

        private void AdaptUGuiLayout() {
            if (androidUIResizeMode != AndroidUIResizeMode.ResizeCameraAndReferenceResolution) {
                return;
            }

            UnityEngine.UI.CanvasScaler[] allCanvasScalers = FindObjectsOfType<UnityEngine.UI.CanvasScaler>();

            for (int i = 0; i < allCanvasScalers.Length; ++i) {
                if (fixedCanvasScalers.Contains(allCanvasScalers[i])) {
                    continue;
                }

                allCanvasScalers[i].referenceResolution =
                    new Vector2(allCanvasScalers[i].referenceResolution.x,
                        allCanvasScalers[i].referenceResolution.y
                        / (allCanvasScalers[i].referenceResolution.y - GetScaledWebViewHeight())
                        * allCanvasScalers[i].referenceResolution.y);
                fixedCanvasScalers.Add(allCanvasScalers[i]);
            }
        }

        #region Audio Focus and Volume

        private bool _nativeGainedAudioFocus;
        private bool _canIgnoreNativeAudioLoss = true;
        
        private void HandleOnMaxVolumeChanged(float newMaximumVolume) {
            MaximumAudioVolume = Mathf.Clamp01(_canHaveAudioFocus ? newMaximumVolume : 0f);
            AirConsoleLogger.LogDevelopment(() =>
                $"HandleOnMaxVolumeChanged({newMaximumVolume}) -> {MaximumAudioVolume}. No action taken.");


        }

        // TODO(QAB-14400, QAB-14401, QAB-14403): Handle Audio Focus change not yet fully implemented.
        // Needs testing on various devices and scenarios and requires a more complete state machine.
        // TODO(QAB-14400, QAB-14401, QAB-14403): Is this actually correct regarding _ignoreAudioFocusLoss which is true from onGameEnd -> onReady?
        // Plugin Manager Audio Focus change handler
        private void HandleAudioFocusChange(bool canHaveAudioFocus, bool shallMuteWebview) {
            AirConsoleLogger.LogDevelopment(() => $"HandleAudioFocusChanged({canHaveAudioFocus})");

            if (!canHaveAudioFocus && _ignoreAudioFocusLoss) {
                AirConsoleLogger.LogDevelopment(() => "Ignoring audio focus loss until the game is ready.");
                return;
            }

            // TODO(PRO-1637): Implement a more complete state machine to handle audio focus changes correctly.
            _canHaveAudioFocus = canHaveAudioFocus;
            if (!canHaveAudioFocus) {
                AirConsoleLogger.LogError(() => "Can have audio focus lost.");
            }

            MaximumAudioVolume = canHaveAudioFocus ? 1f : 0f;
            eventQueue.Enqueue(() => OnGameAudioFocusChanged?.Invoke(canHaveAudioFocus, MaximumAudioVolume));
        }

        // Webview Audio Focus change handler
        private void HandleWebViewAudioFocusChanged(string command) {
            HandlePlatformAudioFocusChanged(command);
        }

        private void HandleNativeAudioFocusChanged(string command) {
            HandlePlatformAudioFocusChanged(command);
        }

        private void HandlePlatformAudioFocusChanged(string command) {
            AirConsoleLogger.LogDevelopment(() => $"HandlePlatformAudioFocusChanged('{command}')");
            if (string.IsNullOrEmpty(command)) {
                AirConsoleLogger.LogError(() => "Audio focus command was null or empty.");
                return;
            }

            if (command.StartsWith("WEBVIEW_AUDIOFOCUS_", StringComparison.Ordinal)) {
                HandleWebViewAudioFocusEvent(command);
                return;
            }

            if (command.StartsWith("NATIVE_AUDIOFOCUS_", StringComparison.Ordinal)) {
                HandleNativeAudioFocusEvent(command);
                return;
            }

            AirConsoleLogger.LogError(() => $"Unknown audio focus command: {command}");
        }

        private void HandleWebViewAudioFocusEvent(string command) {
            switch (command) {
                case "WEBVIEW_AUDIOFOCUS_START":
                    break;
                case "WEBVIEW_AUDIOFOCUS_STOP":
                    break;
                case "WEBVIEW_AUDIOFOCUS_GAIN":
                case "WEBVIEW_AUDIOFOCUS_GAIN_TRANSIENT_EXCLUSIVE":
                case "WEBVIEW_AUDIOFOCUS_GAIN_TRANSIENT_MAY_DUCK":
                case "WEBVIEW_AUDIOFOCUS_GAIN_TRANSIENT":
                    AirConsoleLogger.Log(() =>
                        $"{command}: Can ignore native loss={_canIgnoreNativeAudioLoss}");
                    HasAudioFocus = true;
                    _ignoreAudioFocusLoss = true;
                    break;
                case "WEBVIEW_AUDIOFOCUS_LOSS":
                    AirConsoleLogger.Log(() =>
                        $"{command}: Can ignore native loss={_canIgnoreNativeAudioLoss}");

                    if (_nativeGainedAudioFocus) {
                        return;
                    }

                    _ignoreAudioFocusLoss = false;
                    HandleAudioFocusChange(false, true);
                    break;

                case "WEBVIEW_AUDIOFOCUS_LOSS_TRANSIENT":
                case "WEBVIEW_AUDIOFOCUS_LOSS_CAN_DUCK":
                    break;

                // This is fired when we ask the webview to abandon audio focus.
                case "WEBVIEW_AUDIOFOCUS_ABANDON":
                    break;

                default:
                    AirConsoleLogger.LogError(() => $"Unknown audio focus command from webview: {command}");
                    break;
            }
        }

        private void HandleNativeAudioFocusEvent(string command) {
            switch (command) {
                case "NATIVE_AUDIOFOCUS_GAIN":
                case "NATIVE_AUDIOFOCUS_GAIN_TRANSIENT":
                case "NATIVE_AUDIOFOCUS_GAIN_TRANSIENT_EXCLUSIVE":
                case "NATIVE_AUDIOFOCUS_GAIN_TRANSIENT_MAY_DUCK":
                    AirConsoleLogger.Log(() =>
                        $"{command}: Can ignore native loss={_canIgnoreNativeAudioLoss}");
                    HasAudioFocus = true;
                    _ignoreAudioFocusLoss = false;
                    _nativeGainedAudioFocus = true;

                    HandleAudioFocusChange(true, true);

                    break;

                case "NATIVE_AUDIOFOCUS_LOSS":
                    AirConsoleLogger.Log(() =>
                        $"{command}: Can ignore native loss={_canIgnoreNativeAudioLoss}");

                    if (_canIgnoreNativeAudioLoss) {
                        _canIgnoreNativeAudioLoss = false;
                        return;
                    }

                    _nativeGainedAudioFocus = false;
                    _ignoreAudioFocusLoss = false;
                    HandleAudioFocusChange(false, true);

                    break;
                case "NATIVE_AUDIOFOCUS_LOSS_TRANSIENT":
                case "NATIVE_AUDIOFOCUS_LOSS_CAN_DUCK":
                    break;
                case "NATIVE_AUDIOFOCUS_ABANDON":
                    _nativeGainedAudioFocus = false;
                    _ignoreAudioFocusLoss = false;
                    HandleAudioFocusChange(true, false);
                    break;

                case "NATIVE_AUDIOFOCUS_NONE":
                    break;
                default:
                    AirConsoleLogger.LogError(() => $"Unknown audio focus command from native plugin: {command}");
                    break;
            }
        }
        #endregion Audio Focus and Volume

        /// <summary>
        /// Called when there is an update for the content provider.
        /// </summary>
        /// <param name="messsage">The message received.</param>
        private void OnUpdateContentProvider(JObject messsage) {
            string connectCode = (string)messsage["connectCode"];
            string uid = (string)messsage["uid"];

            if (!string.IsNullOrEmpty(connectCode) && !string.IsNullOrEmpty(uid)) {
                _pluginManager?.WriteClientIdentification(connectCode, uid);
            }
        }

        // ReSharper disable once UnusedMember.Global
        /// <summary>
        /// Checks if the current device is an automotive device.
        /// </summary>
        /// <returns>True if the device is an automotive device, otherwise false.</returns>
        public bool IsAutomotive() => _pluginManager?.IsAutomotive() ?? false;

        // ReSharper disable once UnusedMember.Global
        /// <summary>
        /// Checks if the current device is a TV device.
        /// </summary>
        /// <returns>True if the device is a TV device, otherwise false.</returns>
        public bool IsTV() => _pluginManager?.IsTV() ?? false;

        private static float GetFloatFromMessage(JObject msg, string name, int defaultValue) =>
            !string.IsNullOrEmpty((string)msg[name])
                ? (float)msg[name]
                : defaultValue;
        #endregion

        #region AirConsole Internal
        /// <summary>
        /// Sends a message to the platform.
        /// </summary>
        /// <param name="msg">The message to send.</param>
        internal void SendPlatformMessage(JObject msg) {
            if (!IsAirConsoleUnityPluginReady()) {
                return;
            }

            wsListener.Message(msg);
        }
        #endregion AirConsole Internal

#endif
    }
}
