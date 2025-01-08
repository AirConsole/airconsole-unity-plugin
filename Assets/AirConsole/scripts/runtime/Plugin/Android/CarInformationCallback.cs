using System;
using Newtonsoft.Json;
using UnityEngine;

namespace NDream.AirConsole.Android.Plugin {
    public class CarInformationCallback : AndroidJavaProxy {
        private Action<CarInformation> _callback;

        public CarInformationCallback(Action<CarInformation> callback) : base("com.airconsole.unityandroidlibrary.CarInformationCallback") {
            _callback = callback;
        }

        // This method matches the Java interface method
        public void onCallback(string message) {
            Debug.Log("Received message from Java: " + message);
            if (_callback != null) {
                _callback(JsonConvert.DeserializeObject<CarInformation>(message));
            }
            // Handle the callback as needed
        }
    }
}