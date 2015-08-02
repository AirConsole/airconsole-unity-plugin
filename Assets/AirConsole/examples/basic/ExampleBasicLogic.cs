using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NDream.AirConsole;
using Newtonsoft.Json.Linq;

public class ExampleBasicLogic : MonoBehaviour {

    public GameObject cube;
    private bool isReady = false;

	void Start () {
        // register events
        AirConsole.instance.onReady += OnReady;
        AirConsole.instance.onMessage += OnMessage;
	}

    void OnReady(string code) {
        Debug.Log("ExampleBasic: air console is ready!");
        isReady = true;
    }

    void OnMessage(int from, JToken data) {
        Debug.Log("ExampleBasic: Incoming message from device " + from + ": " + data);

        // move the cube to left
        if ((string)data == "left") {
            this.cube.transform.Translate(Vector3.left);
        }

        // move the cube to right
        if ((string)data == "right") {
            this.cube.transform.Translate(Vector3.right);
        }
    }

    void OnGUI() {

        if (isReady == false) {
            GUILayout.Label("connection not ready yet..");
            GUILayout.Label("waiting for screen.html to connect");
            return;
        }

        if(GUILayout.Button("send message to controller 1")){
            AirConsole.instance.Message(1, "hey controller 1!");
        }

        if (GUILayout.Button("broadcast message to all")) {
            AirConsole.instance.Broadcast("hey guys!");
        }

        if (GUILayout.Button("get device_id")) {
            Debug.Log(AirConsole.instance.device_id);
        }

        if (GUILayout.Button("get nickname of controller 1")) {
            Debug.Log(AirConsole.instance.GetNickname(1));
        }

        if (GUILayout.Button("get profile picture url of controller 1")) {
            Debug.Log(AirConsole.instance.GetProfilePicture(1));
        }

        if (GUILayout.Button("get all custom data from controller 1")) {

            JToken data = AirConsole.instance.GetCustomDeviceState(1);

            if (data != null) {

                // check if data has multiple properties
                if (data.HasValues) {

                    // go through all properties
                    foreach (var prop in ((JObject)data).Properties()) {
                        Debug.Log("key:" + prop.Name + " / value:" + prop.Value);
                    }

                } else {
                    Debug.Log(data);
                }
            }
        }

        if (GUILayout.Button("get custom data parameter health from controller 1")) {

            if (AirConsole.instance.GetCustomDeviceState(1) != null) {

                JToken data = AirConsole.instance.GetCustomDeviceState(1);
                Debug.Log("value int 'health':" + (int)data["health"]);
            }
        }

        if (GUILayout.Button("set some custom screen data")) {

            var customData = new { 
                level = 1,
                started  = true,
            };

            AirConsole.instance.SetCustomDeviceState(customData);
        }

        if (GUILayout.Button("get custom data from screen")) {

            if (AirConsole.instance.GetCustomDeviceState(0) != null) {

                // only show json string of entries
                foreach (JToken key in AirConsole.instance.GetCustomDeviceState(0).Children()) {
                    Debug.Log(key);
                }
            }
        }

        if (GUILayout.Button("get amount of connected devices")) {
            Debug.Log(AirConsole.instance.devices.Count);
        }

        if (GUILayout.Button("get server time")) {
            Debug.Log(AirConsole.instance.GetServerTime());
        }

        if (GUILayout.Button("get server time offset")) {
            Debug.Log(AirConsole.instance.server_time_offset);
        }

        if (GUILayout.Button("show Default UI")) {
            AirConsole.instance.ShowDefaultUI(true);
        }

        if (GUILayout.Button("hide Default UI")) {
            AirConsole.instance.ShowDefaultUI(false);
        }

        if (GUILayout.Button("navigateHome")) {
            AirConsole.instance.NavigateHome();
        }
    }

    void OnDestroy() {

        // unregister events
        if (AirConsole.instance != null) {
            AirConsole.instance.onReady -= OnReady;
            AirConsole.instance.onMessage -= OnMessage;
        }
    }
}
