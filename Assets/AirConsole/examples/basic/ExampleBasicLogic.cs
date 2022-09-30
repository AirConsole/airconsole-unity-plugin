using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using NDream.AirConsole;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;

public class ExampleBasicLogic : MonoBehaviour {

	public GameObject logo;
	public Renderer profilePicturePlaneRenderer;
	public Text logWindow;
	private bool turnLeft;
	private bool turnRight;

	private float highScore = 0;
	public Text highScoreDisplay;

#if !DISABLE_AIRCONSOLE
	void Awake () {
		// register events
		AirConsole.instance.onReady += OnReady;
		AirConsole.instance.onMessage += OnMessage;
		AirConsole.instance.onConnect += OnConnect;
		AirConsole.instance.onDisconnect += OnDisconnect;
		AirConsole.instance.onDeviceStateChange += OnDeviceStateChange;
		AirConsole.instance.onCustomDeviceStateChange += OnCustomDeviceStateChange;
		AirConsole.instance.onDeviceProfileChange += OnDeviceProfileChange;
		AirConsole.instance.onAdShow += OnAdShow;
		AirConsole.instance.onAdComplete += OnAdComplete;
		AirConsole.instance.onGameEnd += OnGameEnd;
		AirConsole.instance.onHighScores += OnHighScores;
		AirConsole.instance.onHighScoreStored += OnHighScoreStored;
		AirConsole.instance.onPersistentDataStored += OnPersistentDataStored;
		AirConsole.instance.onPersistentDataLoaded += OnPersistentDataLoaded;
		AirConsole.instance.onPremium += OnPremium;
		logWindow.text = "Connecting... \n \n";
	}

	void OnReady (string code) {
		//Log to on-screen Console
		logWindow.text = "ExampleBasic: AirConsole is ready! \n \n";

		//Mark Buttons as Interactable as soon as AirConsole is ready
		Button[] allButtons = (Button[])GameObject.FindObjectsOfType ((typeof(Button)));
		foreach (Button button in allButtons) {
			button.interactable = true;
		}
	}

	void OnMessage (int from, JToken data) {
		//Log to on-screen Console
		logWindow.text = logWindow.text.Insert (0, "Incoming message from device: " + from + ": " + data.ToString () + " \n \n");

		// Rotate the AirConsole Logo to the right
		if ((string)data == "left") {
			turnLeft = true;
			turnRight = false;
		}

		// Rotate the AirConsole Logo to the right
		if ((string)data == "right") {
			turnLeft = false;
			turnRight = true;
		}

		// Stop rotating the AirConsole Logo
		//'stop' is sent when a button on the controller is released
		if ((string)data == "stop") {
			turnLeft = false;
			turnRight = false;
		}

		//Show an Ad
		if ((string)data == "show_ad") {
			AirConsole.instance.ShowAd ();
		}
	}

	void OnConnect (int device_id) {
		//Log to on-screen Console
		logWindow.text = logWindow.text.Insert (0, "Device: " + device_id + " connected. \n \n");
	}

	void OnDisconnect (int device_id) {
		//Log to on-screen Console
		logWindow.text = logWindow.text.Insert (0, "Device: " + device_id + " disconnected. \n \n");
	}

	void OnDeviceStateChange (int device_id, JToken data) {
		//Log to on-screen Console
		logWindow.text = logWindow.text.Insert (0, "Device State Change on device: " + device_id + ", data: " + data + "\n \n");
	}

	void OnCustomDeviceStateChange (int device_id, JToken custom_data) {
		//Log to on-screen Console
		logWindow.text = logWindow.text.Insert (0, "Custom Device State Change on device: " + device_id + ", data: " + custom_data + "\n \n");
	}

	void OnDeviceProfileChange (int device_id) {
		//Log to on-screen Console
		logWindow.text = logWindow.text.Insert (0, "Device " + device_id + " made changes to its profile. \n \n");
	}

	void OnAdShow () {
		//Log to on-screen Console
		logWindow.text = logWindow.text.Insert (0, "On Ad Show \n \n");
	}

	void OnAdComplete (bool adWasShown) {
		//Log to on-screen Console
		logWindow.text = logWindow.text.Insert (0, "Ad Complete. Ad was shown: " + adWasShown + "\n \n");
	}

	void OnGameEnd () {
		Debug.Log ("OnGameEnd is called");
		Camera.main.enabled = false;
		Time.timeScale = 0.0f;
	}

	void OnHighScores (JToken highscores) {
		//Log to on-screen Console
		logWindow.text = logWindow.text.Insert (0, "On High Scores " + highscores + " \n \n");
		//logWindow.text = logWindow.text.Insert (0, "Converted Highscores: " + HighScoreHelper.ConvertHighScoresToTables(highscores).ToString() + " \n \n");
	}

	void OnHighScoreStored (JToken highscore) {
		//Log to on-screen Console
		if (highscore == null) {
			logWindow.text = logWindow.text.Insert (0, "On High Scores Stored (null)\n \n");
		} else {
			logWindow.text = logWindow.text.Insert (0, "On High Scores Stored " + highscore + "\n \n");
		}
	}

	void OnPersistentDataStored (string uid) {
		//Log to on-screen Console
		logWindow.text = logWindow.text.Insert (0, "Stored persistentData for uid " + uid + " \n \n");
	}

	void OnPersistentDataLoaded (JToken data) {
		//Log to on-screen Console
		logWindow.text = logWindow.text.Insert (0, "Loaded persistentData: " + data + " \n \n");
	}

	void OnPremium(int device_id){
		//Log to on-screen Console
		logWindow.text = logWindow.text.Insert (0, "On Premium (device " + device_id + ") \n \n");
	}

	void Update () {
		//If any controller is pressing a 'Rotate' button, rotate the AirConsole Logo in the scene
		if (turnLeft) {
			this.logo.transform.Rotate (0, 0, 2);

		} else if (turnRight) {
			this.logo.transform.Rotate (0, 0, -2);
		}
	}

	public void SendMessageToController1 () {
		//Say Hi to the first controller in the GetControllerDeviceIds List.

		//We cannot assume that the first controller's device ID is '1', because device 1
		//might have left and now the first controller in the list has a different ID.
		//Never hardcode device IDs!
		int idOfFirstController = AirConsole.instance.GetControllerDeviceIds () [0];

		AirConsole.instance.Message (idOfFirstController, "Hey there, first controller!");

		//Log to on-screen Console
		logWindow.text = logWindow.text.Insert (0, "Sent a message to first Controller \n \n");
	}

	public void BroadcastMessageToAllDevices () {
		AirConsole.instance.Broadcast ("Hey everyone!");
		logWindow.text = logWindow.text.Insert (0, "Broadcast a message. \n \n");
	}

	public void DisplayDeviceID () {
		//Get the device id of this device
		int device_id = AirConsole.instance.GetDeviceId ();

		//Log to on-screen Console
		logWindow.text = logWindow.text.Insert (0, "This device's id: " + device_id + "\n \n");
	}

	public void DisplayNicknameOfFirstController () {
		//We cannot assume that the first controller's device ID is '1', because device 1
		//might have left and now the first controller in the list has a different ID.
		//Never hardcode device IDs!
		int idOfFirstController = AirConsole.instance.GetControllerDeviceIds () [0];

		//To get the controller's name right, we get their nickname by using the device id we just saved
		string nicknameOfFirstController = AirConsole.instance.GetNickname (idOfFirstController);

		//Log to on-screen Console
		logWindow.text = logWindow.text.Insert (0, "The first controller's nickname is: " + nicknameOfFirstController + "\n \n");

	}

	private IEnumerator DisplayUrlPicture (string uri) {
		// Download the picture URL as a texture
		var www = UnityWebRequestTexture.GetTexture(uri);
		yield return www.SendWebRequest();
		Texture pictureText = DownloadHandlerTexture.GetContent(www);

		// Assign texture
		profilePicturePlaneRenderer.material.mainTexture = pictureText;
		Color color = Color.white;
		color.a = 1;
		profilePicturePlaneRenderer.material.color = color;

		yield return new WaitForSeconds (3.0f);

		color.a = 0;
		profilePicturePlaneRenderer.material.color = color;
	}

	public void DisplayProfilePictureOfFirstController () {
		//We cannot assume that the first controller's device ID is '1', because device 1
		//might have left and now the first controller in the list has a different ID.
		//Never hardcode device IDs!
		int idOfFirstController = AirConsole.instance.GetControllerDeviceIds () [0];

		string urlOfProfilePic = AirConsole.instance.GetProfilePicture (idOfFirstController, 512);

		//Log url to on-screen Console
		logWindow.text = logWindow.text.Insert (0, "URL of Profile Picture of first Controller: " + urlOfProfilePic + "\n \n");
		StartCoroutine (DisplayUrlPicture (urlOfProfilePic));
	}

	public void DisplayAllCustomDataOfFirstController () {
		//We cannot assume that the first controller's device ID is '1', because device 1
		//might have left and now the first controller in the list has a different ID.
		//Never hardcode device IDs!
		int idOfFirstController = AirConsole.instance.GetControllerDeviceIds () [0];

		//Get the Custom Device State of the first Controller
		JToken data = AirConsole.instance.GetCustomDeviceState (idOfFirstController);

		if (data != null) {

			// Check if data has multiple properties
			if (data.HasValues) {

				// go through all properties
				foreach (var prop in ((JObject)data).Properties()) {
					logWindow.text = logWindow.text.Insert (0, "Custom Data on first Controller - Key:  " + prop.Name + " / Value:" + prop.Value + "\n \n");
				}

			} else {
				//If there's only one property, log it to on-screen Console
				logWindow.text = logWindow.text.Insert (0, "Custom Data on first Controller: " + data + "\n \n");
			}
		} else {
			logWindow.text = logWindow.text.Insert (0, "No Custom Data on first Controller \n \n");
		}
	}

	public void DisplayCustomPropertyHealthOnFirstController () {
		//We cannot assume that the first controller's device ID is '1', because device 1
		//might have left and now the first controller in the list has a different ID.
		//Never hardcode device IDs!
		int idOfFirstController = AirConsole.instance.GetControllerDeviceIds () [0];

		//Get the Custom Device State of the first Controller
		JToken data = AirConsole.instance.GetCustomDeviceState (idOfFirstController);

		//If it exists, get the data's health property and cast it as int
		if (data != null && data ["health"] != null) {
			int healthOfFirstController = (int)data ["health"];
			logWindow.text = logWindow.text.Insert (0, "value 'health':" + healthOfFirstController + "\n \n");
		} else {
			logWindow.text = logWindow.text.Insert (0, "No 'health' property set on first Controller \n \n");
		}
	}

	public void SetSomeCustomDataOnScreen () {
		//create some data
		var customData = new {
			players = AirConsole.instance.GetControllerDeviceIds ().Count,
			started = false,
		};

		//Set that Data as this device's Custom Device State (this device is the Screen)
		AirConsole.instance.SetCustomDeviceState (customData);

		//Log url to on-screen Console
		logWindow.text = logWindow.text.Insert (0, "Set new Custom data on Screen: " + customData + " \n \n");
	}

	public void SetLevelPropertyInCustomScreenData () {
		//Set a property 'level' in this devie's custom data (this device is the Screen)
		AirConsole.instance.SetCustomDeviceStateProperty ("level", 1);
	}

	public void DisplayAllCustomDataFromScreen () {
		//The screen always has device id 0. That is the only device id you're allowed to hardcode.
		if (AirConsole.instance.GetCustomDeviceState (0) != null) {

			logWindow.text = logWindow.text.Insert (0, " \n");

			// Show json string of entries
			foreach (JToken key in AirConsole.instance.GetCustomDeviceState(0).Children()) {
				logWindow.text = logWindow.text.Insert (0, "Custom Data on Screen: " + key + " \n");
			}
		}
	}

	public void DisplayNumberOfConnectedControllers () {
		//This does not count devices that have been connected and then left,
		//only devices that are still active
		int numberOfActiveControllers = AirConsole.instance.GetControllerDeviceIds ().Count;
		logWindow.text = logWindow.text.Insert (0, "Number of Active Controllers: " + numberOfActiveControllers + "\n \n");
	}

	public void SetActivePlayers () {
		//Set the currently connected devices as the active players (assigning them a player number)
		AirConsole.instance.SetActivePlayers ();

		string activePlayerIds = "";
		foreach (int id in AirConsole.instance.GetActivePlayerDeviceIds) {
			activePlayerIds += id + "\n";
		}

		//Log to on-screen Console
		logWindow.text = logWindow.text.Insert (0, "Active Players were set to:\n" + activePlayerIds + " \n \n");
	}

	public void DisplayDeviceIDOfPlayerOne () {

		int device_id = AirConsole.instance.ConvertPlayerNumberToDeviceId (0);

		//Log to on-screen Console
		if (device_id != -1) {
			logWindow.text = logWindow.text.Insert (0, "Player #1 has device ID: " + device_id + " \n \n");
		} else {
			logWindow.text = logWindow.text.Insert (0, "There is no active player # 1 - Set Active Players first!\n \n");
		}
	}

	public void DisplayServerTime () {
		//Get the Server Time
		float time = AirConsole.instance.GetServerTime ();

		//Log to on-screen Console
		logWindow.text = logWindow.text.Insert (0, "Server Time: " + time + "\n \n");
	}

	public void DisplayIfFirstContrllerIsLoggedIn () {
		//Get the Device Id
		int idOfFirstController = AirConsole.instance.GetControllerDeviceIds () [0];

		bool firstPlayerLoginStatus = AirConsole.instance.IsUserLoggedIn (idOfFirstController);

		//Log to on-screen Console
		logWindow.text = logWindow.text.Insert (0, "First Player is logged in: " + firstPlayerLoginStatus + "\n \n");
	}

	public void NavigateHome () {
		//Navigate back to the AirConsole store
		AirConsole.instance.NavigateHome ();

		//Log to on-screen Console
		logWindow.text = logWindow.text.Insert (0, "Navigated back to home screen" + "\n \n");
	}

	public void NavigateToPong () {
		//Navigate to another game
		AirConsole.instance.NavigateTo ("http://games.airconsole.com/pong/");
	}

	public void ShowAd () {
		//Display an Advertisement
		AirConsole.instance.ShowAd ();
		//Log to on-screen Console
		logWindow.text = logWindow.text.Insert (0, "Called ShowAd" + "\n \n");
	}

	public void IncreaseScore () {
		//increase current score and show on ui
		highScore += 1;
		highScoreDisplay.text = "Current Score: " + highScore;
	}

	public void ResetScore () {
		//reset current score and show on ui
		highScore = 0;
		highScoreDisplay.text = "Current Score: " + highScore;
	}

	public void RequestHighScores () {
		List <string> ranks = new List<string> ();
		ranks.Add ("world");
		AirConsole.instance.RequestHighScores ("Basic Example", "v1.0", null, ranks, 5, 3);
	}

	public void StoreHighScore () {
		JObject testData = new JObject();
		testData.Add ("test", "data");
		AirConsole.instance.StoreHighScore ("Basic Example", "v1.0", highScore, AirConsole.instance.GetUID(AirConsole.instance.GetMasterControllerDeviceId()), testData);
	}

	public void StoreTeamHighScore () {
		List<string> connectedUids = new List<string> ();
		List<int> deviceIds = AirConsole.instance.GetControllerDeviceIds();

		for (int i = 0; i < deviceIds.Count; i++) {
			connectedUids.Add (AirConsole.instance.GetUID(deviceIds[i]));
		}
		AirConsole.instance.StoreHighScore ("Basic Example", "v1.0", highScore, connectedUids);
	}

	public void StorePersistentData () {
		//Store test data for the master controller
		JObject testData = new JObject();
		testData.Add ("test", "data");
		AirConsole.instance.StorePersistentData("custom_data", testData, AirConsole.instance.GetUID(AirConsole.instance.GetMasterControllerDeviceId()));
	}

	public void RequestPersistentData () {
		List<string> connectedUids = new List<string> ();
		List<int> deviceIds = AirConsole.instance.GetControllerDeviceIds();

		for (int i = 0; i < deviceIds.Count; i++) {
			connectedUids.Add (AirConsole.instance.GetUID(deviceIds[i]));
		}
		AirConsole.instance.RequestPersistentData (connectedUids);
	}

	public void ShowMasterControllerId(){
		int masterControllerId = AirConsole.instance.GetMasterControllerDeviceId ();

		logWindow.text = logWindow.text.Insert (0, "Device " + masterControllerId + " is Master Controller\n \n");
	}

	public void ShowPremiumDeviceIDs () {

		List<int> premiumDevices = AirConsole.instance.GetPremiumDeviceIds ();

		if (premiumDevices.Count > 0) {
			foreach (int deviceId in premiumDevices){
				logWindow.text = logWindow.text.Insert (0, "Device " + deviceId + " is Premium" + "\n \n");
			}
		} else {
			//Log to on-screen Console
			logWindow.text = logWindow.text.Insert (0, "No premium controllers are connected" + "\n \n");
		}
	}

	void OnDestroy () {

		// unregister events
		if (AirConsole.instance != null) {
			AirConsole.instance.onReady -= OnReady;
			AirConsole.instance.onMessage -= OnMessage;
			AirConsole.instance.onConnect -= OnConnect;
			AirConsole.instance.onDisconnect -= OnDisconnect;
			AirConsole.instance.onDeviceStateChange -= OnDeviceStateChange;
			AirConsole.instance.onCustomDeviceStateChange -= OnCustomDeviceStateChange;
			AirConsole.instance.onAdShow -= OnAdShow;
			AirConsole.instance.onAdComplete -= OnAdComplete;
			AirConsole.instance.onGameEnd -= OnGameEnd;
			AirConsole.instance.onHighScores -= OnHighScores;
			AirConsole.instance.onHighScoreStored -= OnHighScoreStored;
			AirConsole.instance.onPersistentDataStored -= OnPersistentDataStored;
			AirConsole.instance.onPersistentDataLoaded -= OnPersistentDataLoaded;
			AirConsole.instance.onPremium -= OnPremium;
		}
	}
#endif
}

