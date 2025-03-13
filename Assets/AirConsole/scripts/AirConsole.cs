using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System;
using WebSocketSharp;
using WebSocketSharp.Server;
using Newtonsoft.Json.Linq;
using UnityEngine.SceneManagement;
#if UNITY_WEBGL
using System.Runtime.InteropServices;
#endif

namespace NDream.AirConsole {
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

    public delegate void OnDeviceStateChange(int device_id, JToken user_data);

    public delegate void OnConnect(int device_id);

    public delegate void OnDisconnect(int device_id);

    public delegate void OnCustomDeviceStateChange(int device_id, JToken custom_device_data);

    public delegate void OnDeviceProfileChange(int device_id);

    public delegate void OnAdShow();

    public delegate void OnAdComplete(bool ad_was_shown);

    public delegate void OnGameEnd();

    public delegate void OnHighScores(JToken highscores);

    public delegate void OnHighScoreStored(JToken highscore);

    public delegate void OnPersistentDataStored(string uid);

    public delegate void OnPersistentDataLoaded(JToken data);

    public delegate void OnPremium(int device_id);

    public delegate void OnPause();

    public delegate void OnResume();

    public class AirConsole : MonoBehaviour {
#if !DISABLE_AIRCONSOLE

        #region airconsole api

        /// <summary>
        /// Device ID of the screen
        /// </summary>
        public const int SCREEN = 0;

        /// <summary>
        /// Gets the Version string to provide it to remote addressable path.
        /// </summary>
        /// <remarks>This is designed to be used with Remote Addressable Configuration as {NDream.AirConsole.AirConsole.Version} path fragment</remarks>
        public static string Version {
#if UNITY_ANDROID
            get { return instance.androidTvGameVersion; }
#else
            get { return string.Empty; }
#endif
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
        /// This event also also fires onConnect for all devices that already are
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
        /// This is function is also called every time OnConnect, OnDisconnect or OnCustomDeviceStateChange is called. It's like their root function.
        /// </summary>
        /// <param name="device_id">the device ID that changed its DeviceState.</param>
        /// <param name="data"> the data of that device. If undefined, the device has left.</param>
        public event OnDeviceStateChange onDeviceStateChange;

        /// <summary>
        /// Gets called when a device has connected and loaded the game.
        /// </summary>
        /// <param name="device_id">the device ID that loaded the game.</param>
        public event OnConnect onConnect;

        /// <summary>
        /// Gets called when a device has left the game.
        /// </summary>
        /// <param name="device_id">the device ID that left the game.</param>
        public event OnDisconnect onDisconnect;

        /// <summary>
        /// Gets called when a device updates it's custom DeviceState by calling SetCustomDeviceState or SetCustomDeviceStateProperty.
        /// Make sure you understand the power of device states: http://developers.airconsole.com/#/guides/device_ids_and_states
        /// </summary>
        /// <param name="device_id">the device ID that changed its customDeviceState.</param>
        /// <param name="cutsom_data">The custom DeviceState data value.</param>
        public event OnCustomDeviceStateChange onCustomDeviceStateChange;

        /// <summary>
        /// Gets called when a device updates it's profile pic, nickname or email.
        /// </summary>
        /// <param name="device_id">The device_id that changed its profile.</param>
        public event OnDeviceProfileChange onDeviceProfileChange;

        /// <summary>
        /// Gets called if a fullscreen advertisement is shown on this screen.
        /// In case this event gets called, please mute all sounds.
        /// </summary>
        public event OnAdShow onAdShow;

        /// <summary>
        /// Gets called when an advertisement is finished or no advertisement was shown.
        /// </summary>
        /// <param name="ad_was_shown">True if an ad was shown and onAdShow was called.</param>
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
        /// <param name="device_id">The device id of the premium device.</param>
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
        /// Determines whether the AirConsole Unity Plugin is ready. Use onReady event instead if possible.
        /// </summary>
        /// <returns><c>true</c> if the AirConsole Unity Plugin is ready; otherwise, <c>false</c>.</returns>
        public bool IsAirConsoleUnityPluginReady() {
            return wsListener != null && wsListener.IsReady();
        }

        /// <summary>
        /// Sends a message to another device.
        /// </summary>
        /// <param name="to">The device ID to send the message to.</param>
        /// <param name="data">The data to send.</param>
        public void Message(int to, object data) {
            if (!IsAirConsoleUnityPluginReady()) {
                throw new NotReadyException();
            }

            JObject msg = new JObject();
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

            JObject msg = new JObject();
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
        /// <param name="data">The maximum number of controllers that should
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

            JObject msg = new JObject();
            msg.Add("action", "setActivePlayers");
            msg.Add("max_players", max_players);

            wsListener.Message(msg);
        }

        /// <summary>
        /// Returns an array of device_ids of the active players previously set by the
        /// screen by calling setActivePlayers. The first device_id in the array is the
        /// first player, the second device_id in the array is the second player, ...
        /// </summary>
        public ReadOnlyCollection<int> GetActivePlayerDeviceIds => _players.AsReadOnly();

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
        public int ConvertDeviceIdToPlayerNumber(int device_id) {
            return _players.IndexOf(device_id);
        }


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
                    Debug.LogWarning("AirConsole: GetNickname: device_id " + device_id + " not found");
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
                    Debug.LogWarning("AirConsole: GetLanguage: device_id " + device_id);
                }

                return null;
            }
        }


        /// <summary>
        /// Gets a translation for the users current language See http://developers.airconsole.com/#!/guides/translations
        /// </summary>
        /// <param name="id">The id of the translation string.</param>
        /// <param name="id">Values that should be used for replacement in the translated string. E.g. if a translated string is "Hi %name%" and values is {"name": "Tom"} then this will be replaced to "Hi Tom".</param>
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
                    Debug.LogWarning("Translation not found: " + id);
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
        public string GetProfilePicture(string uid, int size = 64) {
            return Settings.AIRCONSOLE_PROFILE_PICTURE_URL + uid + "&size=" + size;
        }

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
                    return Settings.AIRCONSOLE_PROFILE_PICTURE_URL + (string)GetDevice(device_id)["uid"] + "&size=" + size;
                } catch (Exception) {
                    if (Settings.debug.warning) {
                        Debug.LogWarning("AirConsole: GetProfilePicture: can't find profile picture of device_id:" + device_id);
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

            JObject msg = new JObject();
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

            JObject msg = new JObject();
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

            JObject msg = new JObject();
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
        /// <param name="data">The property name.</param>
        /// <param name="data">The property value.</param>
        public void SetCustomDeviceStateProperty(string key, object value) {
            if (!IsAirConsoleUnityPluginReady()) {
                throw new NotReadyException();
            }

            JObject msg = new JObject();
            msg.Add("action", "setCustomDeviceStateProperty");
            msg.Add("key", JToken.FromObject(key));
            msg.Add("value", JToken.FromObject(value));

            AllocateDeviceSlots(0);
            if (GetDevice(0) == null) {
                _devices[0] = new JObject();
            }

            JToken custom = _devices[0]["custom"];
            if (custom == null) {
                JObject new_custom = new JObject();
                _devices[0]["custom"] = JToken.FromObject(new_custom);
            }


            _devices[0]["custom"][key] = msg["value"];

            wsListener.Message(msg);
        }

        /// <summary>
        /// Requests that AirConsole shows a multiscreen advertisment.
        /// onAdShow is called on all connected devices if an advertisement
        /// is shown (in this event please mute all sounds).
        /// onAdComplete is called on all connected devices when the
        /// advertisement is complete or no advertisement was shown.
        /// </summary>
        public void ShowAd() {
            if (!IsAirConsoleUnityPluginReady()) {
                throw new NotReadyException();
            }

            JObject msg = new JObject();
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

            List<int> result = new List<int>();
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

            if (GetDevice(device_id) != null) {
                try {
                    if (GetDevice(device_id)["auth"] != null) {
                        return (bool)GetDevice(device_id)["auth"];
                    }
                } catch (Exception) {
                    return false;
                }

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
        public void RequestHighScores(string level_name, string level_version, List<string> uids = null, List<string> ranks = null,
            int total = -1, int top = -1) {
            if (!IsAirConsoleUnityPluginReady()) {
                throw new NotReadyException();
            }

            JObject msg = new JObject();
            msg.Add("action", "requestHighScores");
            msg.Add("level_name", level_name);
            msg.Add("level_version", level_version);

            JArray uidsJArray = null;

            if (uids != null) {
                uidsJArray = new JArray();
                foreach (string uid in uids) {
                    uidsJArray.Add(uid);
                }

                msg.Add("uids", uidsJArray);
            }

            JArray ranksJArray = null;

            if (ranks != null) {
                ranksJArray = new JArray();
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
        public void StoreHighScore(string level_name, string level_version, float score, string uid, JObject data = null,
            string score_string = null) {
            List<string> uids = new List<string>();
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
        public void StoreHighScore(string level_name, string level_version, float score, List<string> uids, JObject data = null,
            string score_string = null) {
            if (!IsAirConsoleUnityPluginReady()) {
                throw new NotReadyException();
            }

            JObject msg = new JObject();
            msg.Add("action", "storeHighScore");
            msg.Add("level_name", level_name);
            msg.Add("level_version", level_version);
            msg.Add("score", score);


            JArray uidJArray = new JArray();
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
        public class NotReadyException : SystemException {
            public NotReadyException() : base() { }
        }

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
                throw new ArgumentNullException("uids");
            }

            if (uids.Count < 1) {
                throw new ArgumentException("uids must contain at least one uid");
            }

            JObject msg = new JObject();
            msg.Add("action", "requestPersistentData");

            if (uids != null) {
                JArray uidJArray = new JArray();
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
                throw new ArgumentException("uid must not be null or empty");
            }

            JObject msg = new JObject();
            msg.Add("action", "storePersistentData");
            msg.Add("key", key);
            msg.Add("value", value);

            if (uid != null) {
                msg.Add("uid", uid);
            }

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
                    } else {
                        return false;
                    }
                } catch (Exception) {
                    return false;
                }
            } else {
                if (Settings.debug.warning) {
                    Debug.LogWarning("AirConsole: IsPremium: device_id " + device_id + " not found");
                }

                return false;
            }
        }

        /// <summary>
        /// Returns all device Ids that are premium.
        /// </summary>
        public List<int> GetPremiumDeviceIds() {
            if (!IsAirConsoleUnityPluginReady()) {
                throw new NotReadyException();
            }

            List<int> result = new List<int>();

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
            
            
            JObject msg = new JObject {
                { "action", "setImmersiveState" },
                { "state", payload }
            };
            wsListener.Message(msg);
        }
        #endregion

#endif

        #region airconsole unity config

        [Tooltip("The controller html file for your game")]
        public UnityEngine.Object controllerHtml;

        [Tooltip("Automatically scale the game canvas")]
        public bool autoScaleCanvas = true;

        [Header("Android Settings")]
        [Tooltip(
            "The uploaded web version on the AirConsole Developer Console where your game retrieves its controller data. See details: https://developers.airconsole.com/#!/guides/unity-androidtv")]
        public string androidTvGameVersion;

        [Tooltip("Resize mode to allow space for AirConsole Default UI. See https://developers.airconsole.com/#!/guides/unity-androidtv")]
        public AndroidUIResizeMode androidUIResizeMode;

        [Tooltip("Loading Sprite to be displayed at the start of the game.")]
        public Sprite webViewLoadingSprite;

        [Header("Development Settings")]
        [Tooltip("Start your game normally, with virtual controllers or in debug mode.")]
        public StartMode browserStartMode;

        [Tooltip("Game Id to use for persistentData, HighScore and Translation functionalities")]
        public string devGameId;

        [Tooltip("Language used in the simulator during play mode.")]
        public string devLanguage;

        [Tooltip("Used as local IP instead of your public IP in Unity Editor. Use this to use the controller together with ngrok")]
        public string LocalIpOverride;

        #endregion

#if !DISABLE_AIRCONSOLE

        #region unity functions

        private void Awake() {
            if (instance != this) {
                Destroy(gameObject);
            }

            // always set default object name
            // important for unity webgl communication
            gameObject.name = "AirConsole";
#if UNITY_ANDROID
            defaultScreenHeight = Screen.height;
#endif
        }

        private void Start() {
            // application has to run in background
#if UNITY_ANDROID && !UNITY_EDITOR
            Application.runInBackground = false;
#else
            Application.runInBackground = true;
#endif

            // register all incoming events
#if UNITY_ANDROID
            InitWebView();
            wsListener = new WebsocketListener(webViewObject);
            wsListener.onLaunchApp += OnLaunchApp;
            wsListener.onUnityWebviewResize += OnUnityWebviewResize;
            wsListener.onUnityWebviewPlatformReady += OnUnityWebviewPlatformReady;

            SceneManager.sceneLoaded += OnSceneLoaded;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
#else
            wsListener = new WebsocketListener();
#endif
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


            // check if game is running in webgl build
            if (Application.platform != RuntimePlatform.WebGLPlayer && Application.platform != RuntimePlatform.Android) {
                // start websocket connection
                wsServer = new WebSocketServer(Settings.webSocketPort);
                wsServer.AddWebSocketService<WebsocketListener>(Settings.WEBSOCKET_PATH, () => wsListener);
                wsServer.Start();

                if (Settings.debug.info) {
                    Debug.Log("AirConsole: Dev-Server started!");
                }
            } else {
                if (Application.platform == RuntimePlatform.WebGLPlayer) {
                    // call external javascript init function
                    Application.ExternalCall("onGameReady", autoScaleCanvas);
                }
            }
        }

        private void Update() {
            ProcessEvents();

#if UNITY_ANDROID
            //back button on TV remotes
            if (Input.GetKeyDown(KeyCode.Escape)) {
                Application.Quit();
            }
#endif
        }

        private void ProcessEvents() {
            // dispatch event queue on main unity thread
            while (eventQueue.Count > 0) {
                eventQueue.Dequeue().Invoke();
            }
        }

        private void OnApplicationQuit() {
            Debug.Log("OnApplicationQuit");
            StopWebsocketServer();
        }

        private void OnDisable() {
            StopWebsocketServer();
        }
        
        public static T ACFindObjectOfType<T>() where T : UnityEngine.Object
        {
#if !UNITY_6000_0_OR_NEWER
            return UnityEngine.Object.FindObjectOfType<T>();
#else
            return UnityEngine.Object.FindFirstObjectByType<T>();
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
                AllocateDeviceSlots(deviceId);
                JToken deviceData = (JToken)msg["device_data"];
                if (deviceData != null && deviceData.HasValues) {
                    _devices[deviceId] = deviceData;
                } else {
                    _devices[deviceId] = null;
                }

                if (onDeviceStateChange != null) {
                    eventQueue.Enqueue(delegate() {
                        if (onDeviceStateChange != null) {
                            onDeviceStateChange(deviceId, GetDevice(_device_id));
                        }
                    });
                }

                if (Settings.debug.info) {
                    Debug.Log("AirConsole: saved devicestate of " + deviceId);
                }
            } catch (Exception e) {
                if (Settings.debug.error) {
                    Debug.LogError(e.Message);
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
                    Debug.Log("AirConsole: onConnect " + deviceId);
                }
            } catch (Exception e) {
                if (Settings.debug.error) {
                    Debug.LogError(e.Message);
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
                    Debug.Log("AirConsole: onDisconnect " + deviceId);
                }
            } catch (Exception e) {
                if (Settings.debug.error) {
                    Debug.LogError(e.Message);
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
                    Debug.Log("AirConsole: onCustomDeviceStateChange " + deviceId);
                }
            } catch (Exception e) {
                if (Settings.debug.error) {
                    Debug.LogError(e.Message);
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

        private void OnReady(JObject msg) {
#if UNITY_ANDROID && !UNITY_EDITOR
			if (webViewLoadingCanvas != null){
				GameObject.Destroy (webViewLoadingCanvas.gameObject);
			}
#endif

            // parse server_time_offset
            _server_time_offset = (int)msg["server_time_offset"];

            // parse device_id
            _device_id = (int)msg["device_id"];

            // parse location
            _location = (string)msg["location"];

            if (msg["translations"] != null) {
                _translations = new Dictionary<string, string>();


                foreach (KeyValuePair<string, JToken> keyValue in (JObject)msg["translations"]) {
                    _translations.Add(keyValue.Key, (string)keyValue.Value);
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

            if (onReady != null) {
                eventQueue.Enqueue(delegate() {
                    if (onReady != null) {
                        onReady((string)msg["code"]);
                    }
                });
            }
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
                    Debug.Log("AirConsole: onDeviceProfileChange " + deviceId);
                }
            } catch (Exception e) {
                if (Settings.debug.error) {
                    Debug.LogError(e.Message);
                }
            }
        }

        private void OnAdShow(JObject msg) {
#if UNITY_ANDROID && !UNITY_EDITOR
            webViewObject.SetMargins(0, 0, 0, 0);
#endif
            try {
                if (onAdShow != null) {
                    eventQueue.Enqueue(delegate() {
                        if (onAdShow != null) {
                            onAdShow();
                        }
                    });
                }

                if (Settings.debug.info) {
                    Debug.Log("AirConsole: onAdShow");
                }
            } catch (Exception e) {
                if (Settings.debug.error) {
                    Debug.LogError(e.Message);
                }
            }
        }

        private void OnAdComplete(JObject msg) {
#if UNITY_ANDROID && !UNITY_EDITOR
		webViewObject.SetMargins(0, 0, 0, defaultScreenHeight - webViewHeight);
#endif
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
                    Debug.Log("AirConsole: onAdComplete");
                }
            } catch (Exception e) {
                if (Settings.debug.error) {
                    Debug.LogError(e.Message);
                }
            }
        }

        private void OnGameEnd(JObject msg) {
#if UNITY_ANDROID
            webViewObject.SetMargins(0, 0, 0, 0);
#endif
            try {
                if (onGameEnd != null) {
                    eventQueue.Enqueue(delegate() {
                        if (onGameEnd != null) {
                            onGameEnd();
                        }
                    });
                }

                if (Settings.debug.info) {
                    Debug.Log("AirConsole: onGameEnd");
                }
            } catch (Exception e) {
                if (Settings.debug.error) {
                    Debug.LogError(e.Message);
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
                    Debug.Log("AirConsole: onHighScores");
                }
            } catch (Exception e) {
                if (Settings.debug.error) {
                    Debug.LogError(e.Message);
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
                    Debug.Log("AirConsole: onHighScoreStored");
                }
            } catch (Exception e) {
                if (Settings.debug.error) {
                    Debug.LogError(e.Message);
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
                    Debug.Log("AirConsole: OnPersistentDataStored");
                }
            } catch (Exception e) {
                if (Settings.debug.error) {
                    Debug.LogError(e.Message);
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
                    Debug.Log("AirConsole: OnPersistentDataLoaded");
                }
            } catch (Exception e) {
                if (Settings.debug.error) {
                    Debug.LogError(e.Message);
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
                    Debug.Log("AirConsole: onPremium");
                }
            } catch (Exception e) {
                if (Settings.debug.error) {
                    Debug.LogError(e.Message);
                }
            }
        }

        private void OnPause(JObject msg) {
            try {
                if (onPause != null) {
                    eventQueue.Enqueue(delegate () {
                        if (onPause != null)
                        {
                            onPause();
                        }
                    });
                }

                if (Settings.debug.info) {
                    Debug.Log("AirConsole: onPause");
                }
            } catch (Exception e) {
                if (Settings.debug.error) {
                    Debug.LogError(e.Message);
                }
            }
        }

        private void OnResume(JObject msg) {
            try {
                if (onResume != null) {
                    eventQueue.Enqueue(delegate () {
                        if (onResume != null)
                        {
                            onResume();
                        }
                    });
                }

                if (Settings.debug.info) {
                    Debug.Log("AirConsole: onResume");
                }
            } catch (Exception e) {
                if (Settings.debug.error) {
                    Debug.LogError(e.Message);
                }
            }
        }

        // TODO(2.6.0): Remove this property
        [Obsolete("Please use GetServerTime(). This method will be removed in version 2.6.0.", true)]
        public int server_time_offset => _server_time_offset;

        // TODO(2.6.0): Remove this property
        [Obsolete("device_id is deprecated, please use GetDeviceId() instead.\nThis method will be removed in version 2.6.0.", true)]
        public int device_id => GetDeviceId();

        // TODO(2.6.0): Remove this property
        [Obsolete("Please use getter .Devices instead.\nThis property will be removed in version 2.6.0.", true)]
        public ReadOnlyCollection<JToken> devices => _devices.AsReadOnly();

        /// <summary>
        /// Provides access to the device data of all devices in the game.
        /// Use Devices[AirConsole.SCREEN]?["environment"] to access the environment information of the screen.
        /// </summary>
        public ReadOnlyCollection<JToken> Devices => _devices.AsReadOnly();

        // private vars
        private WebSocketServer wsServer;
        private WebsocketListener wsListener;
#if UNITY_ANDROID
        private WebViewObject webViewObject;
        private Canvas webViewLoadingCanvas;
        private UnityEngine.UI.Image webViewLoadingImage;
        private UnityEngine.UI.Image webViewLoadingBG;
        private int webViewHeight;
        private int defaultScreenHeight;
        private List<UnityEngine.UI.CanvasScaler> fixedCanvasScalers = new List<UnityEngine.UI.CanvasScaler>();
#endif
        private List<JToken> _devices = new List<JToken>();
        private int _device_id;
        private int _server_time_offset;
        private string _location;
        private Dictionary<string, string> _translations;
        private List<int> _players = new List<int>();
        private readonly Queue<Action> eventQueue = new Queue<Action>();

        // unity singleton handling
        private static AirConsole _instance;

        private void StopWebsocketServer() {
            if (wsServer != null) {
                wsServer.Stop();
            }
        }

        private void OnClose() {
            _devices.Clear();
        }

        public static string GetUrl(StartMode mode) {
#if UNITY_EDITOR
            bool isHttps = !Application.isEditor
                           || (!string.IsNullOrEmpty(instance.LocalIpOverride) && instance.LocalIpOverride.StartsWith("https://"));
#else
			bool isHttps = true;
#endif
            string url = isHttps ? Settings.AIRCONSOLE_DEV_URL_HTTPS : Settings.AIRCONSOLE_DEV_URL_HTTP;
            if (mode == StartMode.VirtualControllers || mode == StartMode.DebugVirtualControllers) {
                url += "simulator/";
            } else if (!isHttps) {
                url += "?http=1";
            }

#if UNITY_EDITOR
            var hasDevGameId = instance.devGameId.Length > 0;
            var hasDevLanguage = instance.devLanguage.Length > 0;
            if (hasDevGameId)
            {
                url += "?dev-game-id=" + AirConsole.instance.devGameId;

                if (hasDevLanguage)
                {
                    url += $"&language={instance.devLanguage}";
                }
            }
#endif

            url += "#";

            if (mode == StartMode.Debug || mode == StartMode.DebugVirtualControllers) {
                url += "debug:";
            }

            return url;
        }

        public void ProcessJS(string data) {
            wsListener.ProcessMessage(data);
#if UNITY_WEBGL && !UNITY_EDITOR
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


#if UNITY_ANDROID
        private int GetScaledWebViewHeight() {
            return (int)((float)webViewHeight * Screen.height / defaultScreenHeight);
        }

        private string ComputeUrlVersion(string version) {
            var split = version.Split('.');
            return $"{split[0]}.{split[1]}{split[2]}";
        }

        private void InitWebView() {
            if (androidTvGameVersion != null && androidTvGameVersion != "") {
                if (webViewObject == null) {
                    webViewObject = new GameObject("WebViewObject").AddComponent<WebViewObject>();
                    DontDestroyOnLoad(webViewObject.gameObject);
                    webViewObject.Init((msg) => ProcessJS(msg));

                    string url = Settings.AIRCONSOLE_BASE_URL;
                    url += "client?id=androidunity-" + ComputeUrlVersion(Settings.VERSION);

#if !UNITY_EDITOR
                    // Get bundle version ("Bundle Version Code" in Unity)
                    AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    AndroidJavaObject ca = up.GetStatic<AndroidJavaObject>("currentActivity");
                    AndroidJavaObject packageManager = ca.Call<AndroidJavaObject>("getPackageManager");
                    AndroidJavaObject pInfo = packageManager.Call<AndroidJavaObject>("getPackageInfo", Application.identifier, 0);

                    url += "&bundle-version=" + pInfo.Get<int>("versionCode");
#endif

                    url += "&game-id=" + Application.identifier;
                    url += "&game-version=" + androidTvGameVersion;
                    url += "&unity-version=" + Application.unityVersion;

                    webViewObject.SetMargins(0, 0, 0, defaultScreenHeight);
                    webViewObject.SetVisibility(true);
                    webViewObject.LoadURL(url);

                    //Display loading Screen
                    webViewLoadingCanvas = new GameObject("WebViewLoadingCanvas").AddComponent<Canvas>();


#if !UNITY_EDITOR
					webViewLoadingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
					webViewLoadingBG = (new GameObject("WebViewLoadingBG")).AddComponent<UnityEngine.UI.Image>();
					webViewLoadingImage = (new GameObject("WebViewLoadingImage")).AddComponent<UnityEngine.UI.Image>();
					webViewLoadingBG.transform.SetParent(webViewLoadingCanvas.transform, true);
					webViewLoadingImage.transform.SetParent(webViewLoadingCanvas.transform, true);
					webViewLoadingImage.sprite = webViewLoadingSprite;
					webViewLoadingBG.color = Color.black;
					webViewLoadingImage.rectTransform.localPosition = new Vector3 (0, 0, 0);
					webViewLoadingBG.rectTransform.localPosition = new Vector3 (0, 0, 0);
					webViewLoadingImage.rectTransform.sizeDelta = new Vector2 (Screen.width / 2, Screen.height / 2);
					webViewLoadingBG.rectTransform.sizeDelta = new Vector2 (Screen.width, Screen.height);
					webViewLoadingImage.preserveAspect = true;

					if (webViewLoadingSprite == null){
						webViewLoadingImage.sprite = Resources.Load("androidtv-loadingscreen", typeof(Sprite)) as Sprite;
					}
#endif
                }
            } else {
                if (Settings.debug.error) {
                    Debug.LogError(
                        "AirConsole: for Android builds you need to provide the Game Version Identifier on the AirConsole object in the scene.");
                }
            }
        }

        private void OnLaunchApp(JObject msg) {
            Debug.Log("onLaunchApp");
            string gameId = (string)msg["game_id"];
            string gameVersion = (string)msg["game_version"];

            if (gameId != Application.identifier || gameVersion != instance.androidTvGameVersion) {
                bool quitAfterLaunchIntent = false; // Flag used to force old pre v2.5 way of quitting

                if (msg["quit_after_launch_intent"] != null) {
                    quitAfterLaunchIntent = msg.SelectToken("quit_after_launch_intent").Value<bool>();
                }

                // Quit the Unity Player first and give it the time to close all the threads (Default)
                if (!quitAfterLaunchIntent) {
                    Application.Quit();
                    System.Threading.Thread.Sleep(2000);
                }

                // Start the main AirConsole App
                AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject ca = up.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject packageManager = ca.Call<AndroidJavaObject>("getPackageManager");
                AndroidJavaObject launchIntent = null;
                try {
                    launchIntent = packageManager.Call<AndroidJavaObject>("getLeanbackLaunchIntentForPackage", gameId);
                } catch (Exception) {
                    Debug.Log("getLeanbackLaunchIntentForPackage for " + gameId + " failed");
                }

                if (launchIntent == null) {
                    try {
                        launchIntent = packageManager.Call<AndroidJavaObject>("getLaunchIntentForPackage", gameId);
                    } catch (Exception) {
                        Debug.Log("getLaunchIntentForPackage for " + gameId + " failed");
                    }
                }

                if (launchIntent != null && gameId != Application.identifier) {
                    ca.Call("startActivity", launchIntent);
                } else {
                    Application.OpenURL("market://details?id=" + gameId);
                }

                up.Dispose();
                ca.Dispose();
                packageManager.Dispose();
                launchIntent.Dispose();

                // Quitting after launch intent was the pre v2.5 way
                if (quitAfterLaunchIntent) {
                    Application.Quit();
                }
            }
        }

        private void OnUnityWebviewResize(JObject msg) {
            Debug.Log("OnUnityWebviewResize");
            if (_devices.Count > 0) {
                Debug.Log("screen device data: " + _devices[0].ToString());
            }

            int h = Screen.height;

            if (msg["top_bar_height"] != null) {
                h = (int)msg["top_bar_height"] * 2;
                webViewHeight = h;
            }

            webViewObject.SetMargins(0, 0, 0, defaultScreenHeight - webViewHeight);
            if (androidUIResizeMode == AndroidUIResizeMode.ResizeCamera
                || androidUIResizeMode == AndroidUIResizeMode.ResizeCameraAndReferenceResolution) {
                Camera.main.pixelRect = new Rect(0, 0, Screen.width, Screen.height - GetScaledWebViewHeight());
            }
        }

        private void OnUnityWebviewPlatformReady(JObject msg) {
            webViewObject.SetMargins(0, 0, 0, 0);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode) {
            if (instance != this) {
                return;
            }

            if (androidUIResizeMode == AndroidUIResizeMode.ResizeCamera
                || androidUIResizeMode == AndroidUIResizeMode.ResizeCameraAndReferenceResolution) {
                Camera.main.pixelRect = new Rect(0, 0, Screen.width, Screen.height - GetScaledWebViewHeight());
            }

            if (androidUIResizeMode == AndroidUIResizeMode.ResizeCameraAndReferenceResolution) {
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
        }
#endif

        #endregion

#endif
    }
}
