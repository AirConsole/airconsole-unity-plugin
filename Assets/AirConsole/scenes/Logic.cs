using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using AirConsole;
using Newtonsoft.Json.Linq;

public class Logic : MonoBehaviour {

    public AirController controller;

	// Use this for initialization
	void Start () {

        controller.onReady += OnAirConsoleReady;
        controller.onMessage += OnAirConsoleMessage;
	
	}

    void OnAirConsoleReady() {

        Debug.Log("LOGIC: air console is ready!");
    }

    void OnAirConsoleMessage(JObject msg) {

        Debug.Log("Incoming message from device " + msg["from"] + ": " + msg["data"]);
    }

    void OnGUI() {

        if(GUILayout.Button("send message to controller 1")){
            controller.Message(1, "heey controller 1");
        }

        if (GUILayout.Button("broadcast message to all")) {
            controller.Broadcast("heey guys");
        }


        if (GUILayout.Button("get all device properties from controller 1")) {

            foreach (JToken key in controller.GetDevice(1).Children()) {
                Debug.Log(key);
            }
        }

        if (GUILayout.Button("get custom data from controller 1")) {

            if (controller.GetCustomDeviceState(1) == null) {
                Debug.Log("no custom data on controller 1");
                return;
            }

            JToken data = controller.GetCustomDeviceState(1);
            Debug.Log("value string 'style':" + data["style"]);
            Debug.Log("value int 'health':" + (int)data["health"]);
        }

        if (GUILayout.Button("get amount of connected devices")) {
            Debug.Log(controller.GetConnectedDeices());
        }


        if (GUILayout.Button("get server time")) {
            Debug.Log(controller.GetServerTime());
        }

        if (GUILayout.Button("get server time offset")) {
            Debug.Log(controller.GetServerTimeOffset());
        }
    }
}
