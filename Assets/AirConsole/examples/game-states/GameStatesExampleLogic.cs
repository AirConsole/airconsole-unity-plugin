#if !DISABLE_AIRCONSOLE
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using NDream.AirConsole;
// ReSharper disable Unity.PerformanceCriticalCodeInvocation

public class GameStatesExampleLogic : MonoBehaviour {

	public GameObject logo;
	public Text gameStateText;
	public Text audioStateText;
	public AudioSource backgroundMusic;

	private Button[] gameStateButtons;

	private static class GameStates {
		public const string Playing = "Playing";
		public const string Paused = "Paused";
	}

	private static class ControllerEvents {
		public const string Hello = "hello";
		public const string ToggleAudio = "toggle-audio";
		public const string PauseGame = "pause-game";
		public const string ResumeGame = "resume-game";
	}

	private readonly string[] colorNames = new string[]{ "red", "blue", "green", "yellow", "orange", "purple", "pink" };
	private int colorIndex;

	// This field should overwrite any game state
	private bool gameIsPausedByAirConsole = false;

	private void Awake() {
		// Register all the events I need
		AirConsole.instance.onReady += OnAirConsoleReady;
		AirConsole.instance.onMessage += OnAirConsoleMessage;
		AirConsole.instance.onPause += OnAirConsolePause;
		AirConsole.instance.onResume += OnAirConsoleResume;

		// No device state can be set until AirConsole is ready, so I disable the buttons until then
		gameStateButtons = FindObjectsOfType<Button>();
		foreach (var t in gameStateButtons) {
			t.interactable = false;
		}
	}

	private void Update() {
		this.logo.transform.Rotate(25 * Time.deltaTime, 5 * Time.deltaTime, 100 * Time.deltaTime);
	}

	private void OnDestroy() {
		if (AirConsole.instance == null) return;

		// Unregister events
		AirConsole.instance.onReady -= OnAirConsoleReady;
		AirConsole.instance.onMessage -= OnAirConsoleMessage;
		AirConsole.instance.onPause -= OnAirConsolePause;
		AirConsole.instance.onResume -= OnAirConsoleResume;
	}

	private void OnAirConsoleReady(string code) {
		// Initialize Game State
		var newGameState = new JObject {
			{ "view", new JObject() },
			{ "playerColors", new JObject() }
		};

		AirConsole.instance.SetCustomDeviceState(newGameState);

		// Now that AirConsole is ready, the buttons can be enabled
		foreach (var t in gameStateButtons) {
			t.interactable = true;
		}

		// The game is initialized in the playing state without being muted
		this.SetGameState(GameStates.Playing);
		this.SetAudioIsPausedState(false);

		// Start background music
		backgroundMusic.Play();
	}

	private void OnAirConsoleMessage(int deviceId, JToken message) {
		Debug.Log("Received message from device " + deviceId + ". content: " + message);

		if (message["action"] == null) return;

		var action = message["action"].ToString();

		if (action == ControllerEvents.PauseGame) {
			if (this.gameIsPausedByAirConsole) {
				Debug.Log("Ignoring pause-game event because the game is paused by AirConsole");
			} else {
				this.PauseGame();
			}
		} else if (action == ControllerEvents.ResumeGame) {
			if (this.gameIsPausedByAirConsole) {
				Debug.Log("Ignoring resume-game event because the game is paused by AirConsole");
			} else {
				this.ResumeGame();
			}
		}
	}

	private void OnAirConsolePause() {
		this.gameIsPausedByAirConsole = true;
		this.PauseGame();
	}

	private void OnAirConsoleResume() {
		this.gameIsPausedByAirConsole = false;
		this.ResumeGame();
	}

	private void PauseAudio() {
		AudioListener.pause = true;
		this.SetAudioIsPausedState(true);
	}

	private void UnPauseAudio() {
		AudioListener.pause = false;
		this.SetAudioIsPausedState(false);
	}

	private void PauseGame() {
		Time.timeScale = 0;
		this.PauseAudio();
		this.SetGameState(GameStates.Paused);
	}

	private void ResumeGame() {
		Time.timeScale = 1;
		this.UnPauseAudio();
		this.SetGameState(GameStates.Playing);
	}

	public void AssignPlayerColors() {
		if (!AirConsole.instance.IsAirConsoleUnityPluginReady()) {
			Debug.LogWarning("can't assign player colors until plugin is ready");
			return;
		}

		// Make a copy of connected controller IDs so it can't change while I loop through it
		var controllerIDs = AirConsole.instance.GetControllerDeviceIds();

		// Loop through connected devices
		foreach (int i in controllerIDs) {
			// Ideally, you'd write all the data into the game state first and then set it only once.
			// I'm doing it this way for simplicity, but updating the device state too often can mean your
			// updates get delayed because of rate limiting the more devices are connected, the more this becomes a problem
			AirConsole.instance.SetCustomDeviceStateProperty("playerColors", UpdatePlayerColorData(AirConsole.instance.GetCustomDeviceState(0), i, colorNames[colorIndex]));
			// The controller listens for the onCustomDeviceStateChanged event.
			// See the  controller-game-states.html file for how this is handled there.

			// Different color for the next player
			colorIndex++;
			if (colorIndex == colorNames.Length) {
				colorIndex = 0;
			}
		}
	}

	private void SetGameState(string state) {
		this.gameStateText.text = state;
		// Set a custom device state property to inform all connected devices
		// of the current game state
		AirConsole.instance.SetCustomDeviceStateProperty("state", state);

		// The controller listens for the onCustomDeviceStateChanged event.
		// Open controller-game-states.html to see how it is handled.
	}

	private void SetAudioIsPausedState(bool audioIsPaused) {
		this.audioStateText.text = audioIsPaused ? "Paused" : "Playing";
	}

	private static JToken GetCurrentScreenState() {
		return AirConsole.instance.GetCustomDeviceState(0);
	}

	private static JToken UpdatePlayerColorData(JToken oldGameState, int deviceId, string colorName) {
		// Take out the existing playerColorData and store it as a JObject so I can modify it
		var playerColorData = oldGameState ["playerColors"] as JObject;

		// Check if the playerColorData object within the game state already has data for this device
		if (playerColorData.HasValues && playerColorData[deviceId.ToString()] != null) {
			// There is already color data for this device, replace it
			playerColorData[deviceId.ToString()] = colorName;
		} else {
			// There is no color data for this device yet, create it new
			playerColorData.Add(deviceId.ToString(), colorName);
		}

		// Logging and returning the updated playerColorData
		Debug.Log ("AssignPlayerColor for device " + deviceId + " returning new playerColorData: " + playerColorData);

		return playerColorData;
	}
}
#endif