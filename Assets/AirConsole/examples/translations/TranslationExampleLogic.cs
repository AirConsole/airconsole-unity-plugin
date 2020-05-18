using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using NDream.AirConsole;
using Newtonsoft.Json.Linq;

public class TranslationExampleLogic : MonoBehaviour {

	public Text translationExample;
	public Text[] bulkTranslationExamples;

	void Awake () {
		AirConsole.instance.onMessage += OnMessage;
		AirConsole.instance.onReady += OnReady;


	}

	void OnReady(string code) {
		//Translations are requested in OnReady so that the translated strings can be stored locally and retrieved quickly afterwards
		InitializeTranslations();
	}

	void OnMessage (int from, JToken data){
		//Debug.Log ("message from device " + from + ", data: " + data); 
		
	}


	void InitializeTranslations()
	{

		//simple translations can be called in bulk by passing an array of Unity Text components, or Text Meshes
		AirConsole.instance.TranslateUIElements(bulkTranslationExamples);

		//if a translatable text includes a replaceable value, you have to translate it individually and pass along the values
		//in this case, we are translating a greeting message that includes the master controller's nickname
		//'welcome_nickname' has an English language value of "Welcome %nickname%, glad you're here!"
		translationExample.text = AirConsole.instance.GetTranslation("welcome_nickname", new Dictionary<string, string> { { "nickname", AirConsole.instance.GetNickname(AirConsole.instance.GetMasterControllerDeviceId()) } });

		/* Note: to really understand how translations work, we recommend creating your own project in the AirConsole Developer Console.
         * in this example, we show how to retrieve translations, but you will not be able to view or edit translatable phrases for the
         * example project. Documentation: https://developers.airconsole.com/#!/guides/translations
         * */
	}


	void OnDestroy () {
		if (AirConsole.instance != null) {
			AirConsole.instance.onMessage -= OnMessage;
			AirConsole.instance.onReady -= OnReady;		
		}
	}
}