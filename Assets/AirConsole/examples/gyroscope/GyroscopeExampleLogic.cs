using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NDream.AirConsole;
using Newtonsoft.Json.Linq;

public class GyroscopeExampleLogic : MonoBehaviour {

	public Material material1;
	public Material material2;
	private bool materialToggle;

	public GameObject playerCube;

	void Awake () {
		AirConsole.instance.onMessage += OnMessage;		
	}

	void OnMessage (int from, JToken data){
		//Debug.Log ("message from device " + from + ", data: " + data); 

		switch (data ["action"].ToString ()) {
		case "motion":

			if (data ["motion_data"] != null) {

				if (data ["motion_data"] ["x"].ToString() != "") {

					Vector3 abgAngles = new Vector3 (-(float)data ["motion_data"] ["beta"], -(float)data ["motion_data"] ["alpha"], -(float)data ["motion_data"] ["gamma"]);
					playerCube.transform.eulerAngles = abgAngles;
				}
			}

			break;
		case "shake":
			//the cube changes color on shake
			if (materialToggle) {
				playerCube.GetComponent<Renderer> ().materials = new Material[]{ material1 };
				materialToggle = false;
			} else {
				playerCube.GetComponent<Renderer> ().materials = new Material[]{ material2 };
				materialToggle = true;
			}
			break;
		default:
			Debug.Log (data);
			break;
		}
	}

	void OnDestroy () {
		if (AirConsole.instance != null) {
			AirConsole.instance.onMessage -= OnMessage;		
		}
	}
}
