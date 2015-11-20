using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using NDream.AirConsole;
using Newtonsoft.Json.Linq;

public class ExampleBasicLogic : MonoBehaviour {

    public GameObject logo;

	public Text logWindow;

	private bool turnLeft;
	private bool turnRight;

	void Start () {
        // register events
        AirConsole.instance.onReady += OnReady;
        AirConsole.instance.onMessage += OnMessage;
        AirConsole.instance.onConnect += OnConnect;
		AirConsole.instance.onDisconnect += OnDisconnect;
		AirConsole.instance.onDeviceStateChange += OnDeviceStateChange;
		AirConsole.instance.onCustomDeviceStateChange += OnCustomDeviceStateChange;
		logWindow.text = "Connecting... \n \n";
	}

    void OnReady(string code) {
		//Log to on-screen Console
		logWindow.text = "ExampleBasic: AirConsole is ready! \n \n";

		//Mark Buttons as Interactable as soon as AirConsole is ready
		Button[] allButtons = (Button[])GameObject.FindObjectsOfType ((typeof(Button)));
		foreach (Button button in allButtons) {
			button.interactable = true;
		}
    }

    void OnMessage(int from, JToken data) {
		//Log to on-screen Console
		logWindow.text = logWindow.text.Insert(0, "Incoming message from device: " + from + ": " + data.ToString() + " \n \n");
		
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
    }

	void OnConnect(int device_id){
		//Log to on-screen Console
		logWindow.text = logWindow.text.Insert(0, "Device: " + device_id + " connected. \n \n");
	}

	void OnDisconnect(int device_id){
		//Log to on-screen Console
		logWindow.text = logWindow.text.Insert(0, "Device: " + device_id + " disconnected. \n \n");
	}

	void OnDeviceStateChange(int device_id, JToken data){
		//Log to on-screen Console
		logWindow.text = logWindow.text.Insert(0, "Device State Change on device: " + device_id + ", data: " + data + "\n \n");
	}

	void OnCustomDeviceStateChange(int device_id, JToken custom_data){
		//Log to on-screen Console
		logWindow.text = logWindow.text.Insert(0, "Custom Device State Change on device: " + device_id + ", data: " + custom_data + "\n \n");
	}

	void Update(){
		//If any controller is pressing a 'Rotate' button, rotate the AirConsole Logo in the scene
		if (turnLeft) {
			this.logo.transform.Rotate(0,0,2);
		
		}
		else if (turnRight) {
			this.logo.transform.Rotate(0,0,-2);
		}
	}

    void OnGUI() {

        /*

        if (GUILayout.Button("navigateHome")) {
            AirConsole.instance.NavigateHome();
        }
		if (GUILayout.Button("navigateTo http://games.airconsole.com/pong/")) {
			AirConsole.instance.NavigateTo("http://games.airconsole.com/pong/");
		}*/
    }

	public void SendMessageToController1(){
		//Say Hi to the first controller in the GetControllerDeviceIds List.

		//We cannot assume that the first controller's device ID is '1', because device 1 
		//might have left and now the first controller in the list has a different ID.
		//Never hardcode device IDs!
		int idOfFirstController = AirConsole.instance.GetControllerDeviceIds () [0];

		AirConsole.instance.Message(idOfFirstController, "Hey there, first controller!");

		//Log to on-screen Console
		logWindow.text = logWindow.text.Insert (0, "Sent a message to first Controller \n \n");
	}

	public void BroadcastMessageToAllDevices(){
		AirConsole.instance.Broadcast("Hey everyone!");
		logWindow.text = logWindow.text.Insert (0, "Broadcast a message. \n \n");
	}

	public void GetDeviceID(){
		//Get the device id of this device
		int device_id = AirConsole.instance.GetDeviceId();

		//Log to on-screen Console		
		logWindow.text = logWindow.text.Insert(0, "This device's id: " + device_id + "\n \n");
	}

	public void GetNicknameOfFirstController(){
		//We cannot assume that the first controller's device ID is '1', because device 1 
		//might have left and now the first controller in the list has a different ID.
		//Never hardcode device IDs!		
		int idOfFirstController = AirConsole.instance.GetControllerDeviceIds () [0];

		//To get the controller's name right, we get their nickname by using the device id we just saved
		string nicknameOfFirstController = AirConsole.instance.GetNickname (idOfFirstController);

		//Log to on-screen Console
		logWindow.text = logWindow.text.Insert(0, "The first controller's nickname is: " + nicknameOfFirstController + "\n \n");
		
	}

	public void GetURLOfProfilePictureOfFirstController(){
		//We cannot assume that the first controller's device ID is '1', because device 1 
		//might have left and now the first controller in the list has a different ID.
		//Never hardcode device IDs!		
		int idOfFirstController = AirConsole.instance.GetControllerDeviceIds () [0];
	
		string urlOfProfilePic = AirConsole.instance.GetProfilePicture (idOfFirstController);
		//Log url to on-screen Console
		logWindow.text = logWindow.text.Insert(0, "URL of Profile Picture of first Controller: " + urlOfProfilePic + "\n \n");
	}

	public void GetAllCustomDataOfFirstController(){
		//We cannot assume that the first controller's device ID is '1', because device 1 
		//might have left and now the first controller in the list has a different ID.
		//Never hardcode device IDs!		
		int idOfFirstController = AirConsole.instance.GetControllerDeviceIds () [0];

		//Get the Custom Device State of the first Controller
		JToken data = AirConsole.instance.GetCustomDeviceState(idOfFirstController);
		
		if (data != null) {
			
			// Check if data has multiple properties
			if (data.HasValues) {
				
				// go through all properties
				foreach (var prop in ((JObject)data).Properties()) {
					logWindow.text = logWindow.text.Insert(0, "Custom Data on first Controller - Key:  " + prop.Name + " / Value:" + prop.Value + "\n \n");
				}

			} else {
				//If there's only one property, log it to on-screen Console
				logWindow.text = logWindow.text.Insert(0, "Custom Data on first Controller: " + data + "\n \n");
			}
		} else {
			logWindow.text = logWindow.text.Insert(0, "No Custom Data on first Controller \n \n");
		}
	}

	public void GetCustomPropertyHealthOnFirstController(){
		//We cannot assume that the first controller's device ID is '1', because device 1 
		//might have left and now the first controller in the list has a different ID.
		//Never hardcode device IDs!		
		int idOfFirstController = AirConsole.instance.GetControllerDeviceIds () [0];
		
		//Get the Custom Device State of the first Controller
		JToken data = AirConsole.instance.GetCustomDeviceState(idOfFirstController);

		//If it exists, get the data's health property and cast it as int
		if (data != null && data ["health"] != null) {
			int healthOfFirstController = (int)data ["health"];
			logWindow.text = logWindow.text.Insert (0, "value 'health':" + healthOfFirstController + "\n \n");
		} else {
			logWindow.text = logWindow.text.Insert (0, "No 'health' property set on first Controller \n \n");
		}
	}

	public void SetSomeCustomDataOnScreen(){
		//create some data
		var customData = new { 
			players = AirConsole.instance.GetControllerDeviceIds().Count,
			started = false,
		};

		//Set that Data as this device's Custom Device State (this device is the Screen)
		AirConsole.instance.SetCustomDeviceState(customData);

		//Log url to on-screen Console
		logWindow.text = logWindow.text.Insert (0, "Set new Custom data on Screen: " + customData + " \n \n");
	}

	public void SetLevelPropertyInCustomScreenData(){
		//Set a property 'level' in this devie's custom data (this device is the Screen)
		AirConsole.instance.SetCustomDeviceStateProperty("level", 1);
	}

	public void GetAllCustomDataFromScreen(){
		//The screen always has device id 0. That is the only device id you're allowed to hardcode.
		if (AirConsole.instance.GetCustomDeviceState(0) != null) {

			logWindow.text = logWindow.text.Insert (0, " \n");

			// Show json string of entries
			foreach (JToken key in AirConsole.instance.GetCustomDeviceState(0).Children()) {
				logWindow.text = logWindow.text.Insert (0, "Custom Data on Screen: " + key + " \n");
			}

		}
	}

	public void GetNumberOfConnectedControllers(){
		//This does not count devices that have been connected and then left,
		//only devices that are still active
		int numberOfActiveControllers = AirConsole.instance.GetControllerDeviceIds ().Count;
		logWindow.text = logWindow.text.Insert (0, "Number of Active Controllers: " + numberOfActiveControllers + "\n \n");
	}

	public void GetServerTime(){
		//Get the Server Time
		float time = AirConsole.instance.GetServerTime ();

		//Log to on-screen Console
		logWindow.text = logWindow.text.Insert (0, "Server Time: " + time + "\n \n");
	}

	public void HideDefaultUI(){
		//Hide the Default UI in the Browser Window
		AirConsole.instance.ShowDefaultUI (false);

		//Log to on-screen Console
		logWindow.text = logWindow.text.Insert (0, "Hid Default UI" + "\n \n");
	}

	public void ShowDefaultUI(){
		//Show the Default UI in the Browser Window
		AirConsole.instance.ShowDefaultUI (true);

		//Log to on-screen Console
		logWindow.text = logWindow.text.Insert (0, "Showed Default UI" + "\n \n");
	}

	public void NavigateHome(){
		//Navigate back to the AirConsole store
        AirConsole.instance.NavigateHome();

		//Log to on-screen Console
		logWindow.text = logWindow.text.Insert (0, "Navigated back to home screen" + "\n \n");
	}

	public void NavigateToPong(){
		//Navigate to another game
		AirConsole.instance.NavigateTo("http://games.airconsole.com/pong/");
	}

    void OnDestroy() {

        // unregister events
        if (AirConsole.instance != null) {
            AirConsole.instance.onReady -= OnReady;
            AirConsole.instance.onMessage -= OnMessage;
			AirConsole.instance.onConnect -= OnConnect;
			AirConsole.instance.onDisconnect -= OnDisconnect;
			AirConsole.instance.onDeviceStateChange -= OnDeviceStateChange;
			AirConsole.instance.onCustomDeviceStateChange -= OnCustomDeviceStateChange;
        }
    }
}
