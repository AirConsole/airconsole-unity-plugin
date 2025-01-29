using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace NDream.AirConsole.Android.Plugin {
    public class DataProviderPlugin {
        private AndroidJavaObject dataProviderHelper;

        public bool DataProviderInitialized { get; private set; }
        public string ConnectionUrl { get; private set; }
        public event Action<string> OnConnectionUrlReceived;
        
        // ReSharper disable once UnusedMember.Local -- Used by DataProviderPlugin() in Android NonEditor only
        private bool CheckLibrary() {
            // Get the current Android activity context
            AndroidJavaClass unityPlayer = new("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            // Create an instance of your Java plugin class
            dataProviderHelper = new("com.airconsole.unityandroidlibrary.DataProviderService", context);

            return dataProviderHelper != null;
        }

        public DataProviderPlugin() {
            
#if UNITY_ANDROID
#if !UNITY_EDITOR
            if (!CheckLibrary()) {
                Debug.Log("DataProviderPlugin is not initialized");
                return;
            }
            // Get the current Android activity context
            AndroidJavaClass unityPlayer = new("com.unity3d.player.UnityPlayer");
            AndroidJavaObject context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            UnityPluginStringCallback callback = new UnityPluginStringCallback(url => {
                    DataProviderInitialized = true;
                    ConnectionUrl = url;
                    OnConnectionUrlReceived?.Invoke(url);
                    Debug.Log($"Received URL: {url}");
                },
                error => { Debug.LogError($"Initialization of DataProvider failed with {error}"); }); 

            // Create an instance of your Java plugin class
            dataProviderHelper = new("com.airconsole.unityandroidlibrary.DataProviderService", context);
            dataProviderHelper.Call("init", Settings.AIRCONSOLE_BASE_URL, callback);
#endif
#else
            throw new NotSupportedException("DataProviderPlugin is only supported on Android and Android in Unity");
#endif
        }

        // /// <summary>
        // /// Returns the connection base url
        // /// </summary>
        // /// <returns>The connection base url to append to the backend service base url</returns>
//         public String GetConnectionBaseUrl() {
//             // return "client?id=bmw-idc-23&runtimePlatform=android&unityPluginVersion=2.6.0";
//             // return "client?id=androidunity-4.0&runtimePlatform=android&unityPluginVersion=2.6.0";
//             // but should be
//             //      return "client?id=androidunity-2.60&runtimePlatform=android&unityPluginVersion=2.6.0";
// #if UNITY_ANDROID
// #if !UNITY_EDITOR
//             if (dataProviderHelper != null) {
//                 return $"client?{dataProviderHelper.Call<string>("getConnectionUrl")}&unityPluginVersion={ComputeUrlVersion(Settings.VERSION)}";
//             } else {
//               Debug.Log("DataProviderPlugin is not initialized");
//             }
// #else
//             string urlVersion = ComputeUrlVersion(Settings.VERSION);
//             return $"client?id=androidunity-{urlVersion}&runtimePlatform=android&unityPluginVersion={urlVersion}";
// #endif
// #endif
//             throw new NotSupportedException("DataProviderPlugin is only supported on Android and Android in Unity");
//         }

        // [Obsolete("Use GetConnectionUrl", true)]
//         public void GetCarInformation(Action<CarInformation> callback) {
//             if (callback == null) throw new ArgumentException("callback");
//
// #if !UNITY_EDITOR
// #if UNITY_ANDROID 
//             if (dataProviderHelper == null) throw new UnityException("DataProviderPlugin is not initialized");
//            
//             UnityPluginStringCallback callback = new UnityPluginStringCallback(callback); 
//             dataProviderHelper.Call("getCarInfo", callback);
//             return;
// #endif
//             throw new NotSupportedException("DataProviderPlugin is only supported on Android and Android in Unity");
// #else
//             callback(new CarInformation {
//                 AuthToken = new Dictionary<string, string>(),
//                 Complete = Random.Range(0, 1) > 0.5f,
//                 HomeCountry = string.Empty,
//                 Model = string.Empty,
//                 SoftwareVersion = string.Empty
//             });
// #endif
//         }

//         public void GetConnectionUrl(Action<string> successCallback, Action<string> failureCallback) {
//             if (successCallback == null) throw new ArgumentException("successCallback");
//             if (failureCallback == null) throw new ArgumentException("failureCallback");
//
//             Action<string> innerSuccessCallback = url => {
//                 successCallback($"client?{url}&unityPluginVersion={ComputeUrlVersion(Settings.VERSION)}");
//             };
//             
// #if !UNITY_EDITOR
// #if UNITY_ANDROID 
//             if (dataProviderHelper == null) throw new UnityException("DataProviderPlugin is not initialized");
//            
//             UnityPluginStringCallback callback = new UnityPluginStringCallback(innerSuccessCallback,failureCallback); 
//             dataProviderHelper.Call("getConnectionUrl", callback);
//             return;
// #endif
//             throw new NotSupportedException("DataProviderPlugin is only supported on Android and Android in Unity");
// #else
//             successCallback(new CarInformation {
//                 AuthToken = new Dictionary<string, string>(),
//                 Complete = Random.Range(0, 1) > 0.5f,
//                 HomeCountry = string.Empty,
//                 Model = string.Empty,
//                 SoftwareVersion = string.Empty
//             });
// #endif 
//         }

        // private static string ComputeUrlVersion(string version) {
        //     string[] split = version.Split('.');
        //     return $"{split[0]}.{split[1]}{split[2]}";
        // }
    }
}