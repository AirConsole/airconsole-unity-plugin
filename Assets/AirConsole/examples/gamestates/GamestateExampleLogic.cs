using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using NDream.AirConsole;

public class GamestateExampleLogic : MonoBehaviour {

	Button[] gameStateButtons; 

	private string[] colorNames = new string[]{"red", "blue", "green", "yellow", "orange", "purple", "pink"};
	private int colorIndex;

	private bool gameStateInitialized;

	void Awake(){
		//register all the events I need 
		AirConsole.instance.onReady += OnReady;
		AirConsole.instance.onMessage += OnMessage;

		//no device state can be set until AirConsole is ready, so I disable the buttons until then
		gameStateButtons = FindObjectsOfType<Button>();
		for (int i = 0; i < gameStateButtons.Length; ++i) {
			gameStateButtons [i].interactable = false;
		}
	}

	void OnReady(string code){
		//Initialize Game State
		JObject newGameState = new JObject();
		newGameState.Add ("view", new JObject ());
		newGameState.Add ("playerColors", new JObject ());

		AirConsole.instance.SetCustomDeviceState(newGameState);

		//now that AirConsole is ready, the buttons can be enabled 
		for (int i = 0; i < gameStateButtons.Length; ++i) {
			gameStateButtons [i].interactable = true;
		}
	}

	void OnMessage (int deviceId, JToken message){
		Debug.Log ("received message from device " + deviceId + ". content: " + message);
	}

	public void AssignPlayerColors(){

		if (!AirConsole.instance.IsAirConsoleUnityPluginReady ()) {
			Debug.LogWarning ("can't assign player colors until plugin is ready");
			return;
		}

		//make a copy of connected controller IDs so it can't change while I loop through it
		List<int> controllerIDs = AirConsole.instance.GetControllerDeviceIds ();

		//loop through connected devices
		for (int i = 0; i < controllerIDs.Count; ++i){
			//ideally, you'd write all the data into the game state first and then set it only once. 
			//I'm doing it this way for simplicity, but updating the device state too often can mean your updates get delayed because of rate limiting
			//the more devices are connected, the more this becomes a problem
			AirConsole.instance.SetCustomDeviceStateProperty("playerColors", UpdatePlayerColorData(AirConsole.instance.GetCustomDeviceState (0), controllerIDs[i], colorNames[colorIndex]));
			//the controller listens for the onCustomDeviceStateChanged event. See the  controller-gamestates.html file for how this is handled there. 

			//different color for the next player
			colorIndex++;
			if (colorIndex == colorNames.Length) {
				colorIndex = 0;
			}
		}
	}

	public void SetView(string viewName){
		//I don't need to replace the entire game state, I can just set the view property
		AirConsole.instance.SetCustomDeviceStateProperty("view", viewName);

		//the controller listens for the onCustomDeviceStateChanged event. See the  controller-gamestates.html file for how this is handled there. 
	}

	public void LogCurrentScreenState(){
		Debug.Log ("screen CustomDeviceState: " + AirConsole.instance.GetCustomDeviceState (0));
	}

	void OnDestroy() {

		//Unregister events
		if (AirConsole.instance != null) {
			AirConsole.instance.onReady -= OnReady;
			AirConsole.instance.onMessage -= OnMessage;
		}
	}

	public static JToken UpdatePlayerColorData(JToken oldGameState, int deviceId, string colorName){

		//take out the existing playerColorData and store it as a JObject so I can modify it
		JObject playerColorData = oldGameState ["playerColors"] as JObject;

		//check if the playerColorData object within the game state already has data for this device
		if (playerColorData.HasValues && playerColorData[deviceId.ToString()] != null){
			//there is already color data for this device, replace it
			playerColorData[deviceId.ToString()] = colorName;
		} else {
			playerColorData.Add(deviceId.ToString(), colorName);
			//there is no color data for this device yet, create it new
		}

		//logging and returning the updated playerColorData
		Debug.Log ("AssignPlayerColor for device " + deviceId + " returning new playerColorData: " + playerColorData);
		return playerColorData;
	}
}
