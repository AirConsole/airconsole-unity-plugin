using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace NDream.AirConsole.Editor {
    public abstract class UnityVersionCheck {
        [InitializeOnLoadMethod]
        private static void CheckVersions() {
#if !UNITY_2021_3_OR_NEWER && !UNITY_2022_3_OR_NEWER
            EditorUtility.DisplayDialog("Unsupported", $"AirConsole Unity Plugin {Settings.VERSION} requires Unity 2021.3 or 2022.3", "I understand");
            EditorApplication.isPlaying = false;
#endif

#if UNITY_6 || UNITY_6_3_OR_NEWER
            EditorUtility.DisplayDialog("Unity 6 not allowed", $"AirConsole Unity Plugin {Settings.VERSION} does not allow games to be built with Unity 6", "I understand");
            EditorApplication.isPlaying = false;
#endif
        } 
    }
    
    public abstract class CompatibilityCheck : IPreprocessBuildWithReport {
        public int callbackOrder => 0;
        
        public void OnPreprocessBuild(BuildReport report) {
            CheckGeneralPlayerSettings();

            switch (report.summary.platform) {
                case BuildTarget.Android:
                    CheckAndroidPlayerSettings();
                    break;
                
                case BuildTarget.WebGL:
                    break;
                
                default:
                    throw new UnityException($"AirConsole Plugin does not support platform {report.summary.platform}");
            }
        }
        
        [InitializeOnLoadMethod]
        private static void CheckGeneralPlayerSettings() {
            // The internet permission is required for the AirConsole Unity Plugin so we enforce it by default
            PlayerSettings.Android.forceInternetPermission = true;
            
            // TODO(Marc): These should be done in production builds only
            if (PlayerSettings.stripEngineCode == false) {
                Debug.LogError("AirConsole Unity Plugin requires 'Strip Engine Code' to be enabled in Player Settings. Code Stripping has been enabled again.");
                PlayerSettings.stripEngineCode = true;
            }
        }

        [InitializeOnLoadMethod]
        private static void CheckAndroidPlayerSettings() {
            EnforceAndroidPlayerSettings();
        }
        
        
#if UNITY_ANDROID
        private static void EnforceAndroidPlayerSettings() {
            const int requiredAndroidTargetSdk = 34;
            
            if ((int)PlayerSettings.Android.targetSdkVersion < requiredAndroidTargetSdk) {
                Debug.LogError($"AirConsole Unity Plugin requires 'Target SDK Version' to be set to {requiredAndroidTargetSdk} or higher in Player Settings. We are updating the Android settings now.");
            }
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)requiredAndroidTargetSdk;

            if (PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) != ScriptingImplementation.IL2CPP) {
                Debug.LogWarning("AirConsole Unity Plugin requires 'Scripting Backend' to be set to IL2CPP in Player Settings. We are updating the Android settings now.");
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);    
            }
            
            if((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) != AndroidArchitecture.ARM64) {
                Debug.LogWarning("AirConsole Unity Plugin requires 'Target Architectures' to be set to ARM64 in Player Settings. We are updating the Android settings now.");
                PlayerSettings.Android.targetArchitectures |= AndroidArchitecture.ARM64;
            }
            
            if (!PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android) && !PlayerSettings.GetGraphicsAPIs(BuildTarget.Android).Contains(GraphicsDeviceType.Vulkan)) {
                Debug.LogWarning("AirConsole Unity Plugin requires 'Vulkan' to be enabled in Player Settings. Unless AutoGraphics API is active, we prepend it for Android targets.");
                GraphicsDeviceType[] graphicsAPIs = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android).Prepend(GraphicsDeviceType.Vulkan).ToArray();
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, graphicsAPIs);
            }

            // TODO(Marc): These should be done in production builds only
            if (PlayerSettings.GetManagedStrippingLevel(BuildTargetGroup.Android) == ManagedStrippingLevel.Disabled) {
                Debug.LogWarning("AirConsole Unity Plugin requires 'Managed Stripping Level' to be enabled in Player Settings. Switching stripping level to lown.");
                PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.Low);
            }
#endif
        }
    }
}