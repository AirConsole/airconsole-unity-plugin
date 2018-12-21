using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NDream.AirConsole;
using Newtonsoft.Json.Linq;

public class Player_Swipe : MonoBehaviour {

	public Material mat1;
	public Material mat2;
	private bool materialToggle;

	private float distance = 3f;
	private float step = 0.2f;

	private Coroutine movementCoroutine;

	void Awake (){
		AirConsole.instance.onMessage += OnMessage;
	}

	void OnMessage(int from, JToken message){
		//We check if the message I receive has an "action" parameter and if it's a swipe
		if (message ["action"] != null) {
		if (message ["action"].ToString () == "swipe") {
				//We log the whole vector to see its values 
				Debug.Log ("swipe: " + message ["vector"]);

				//if there is already movement going on, we cancel it
				if (movementCoroutine != null){
					StopCoroutine (movementCoroutine);
				}

				//we convert the x and y values we received to float values and make a new direction vector to pass to our movement function
				movementCoroutine = StartCoroutine(MoveSphere (new Vector3 ((float)message ["vector"]["x"], -(float)message ["vector"]["y"], 0) ));
			}
		}
	}

	//we make the movement a Coroutine so the sphere moves over time instead of instantly
	private IEnumerator MoveSphere (Vector3 direction){

		//calculate the target position
		Vector3 targetPosition = transform.position + direction * distance;

		//while the sphere has not reached its target position, move it closer
		while (Vector3.Distance(targetPosition, transform.position) > 0.1f){
			transform.position = Vector3.MoveTowards (transform.position, targetPosition, step);
			yield return new WaitForFixedUpdate ();
		}
	}

	private void OnDestroy(){
		//unregister events
		if (AirConsole.instance != null){
			AirConsole.instance.onMessage -= OnMessage;
		}
	}
}
