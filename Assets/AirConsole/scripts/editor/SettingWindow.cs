#if !DISABLE_AIRCONSOLE
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
using UnityEngine.Rendering;
using AndroidSdkVersions = UnityEditor.AndroidSdkVersions;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace NDream.AirConsole.Editor {
	public class SettingWindow : EditorWindow {

		GUIStyle styleBlack = new GUIStyle ();
		private GUIStyle styleRedBold = new GUIStyle();
		bool groupEnabled = false;
		static Texture2D bg;
		static Texture logo;
		static Texture logoSmall;
		static GUIContent titleInfo;
		
		Color darkRed = new Color(0.7f, 0.0f, 0.0f);

		private bool debugFoldout;
		private bool androidFoldout;
		private bool webglFoldout;

		public void OnEnable () {
			// ReSharper disable once Unity.UnknownResource
			bg = (Texture2D)Resources.Load ("AirConsoleBg");
			// ReSharper disable once Unity.UnknownResource
			logo = (Texture)Resources.Load ("AirConsoleLogoText");
			// ReSharper disable once Unity.UnknownResource
			logoSmall = (Texture)Resources.Load ("AirConsoleLogoSmall");
			titleInfo = new GUIContent ("AirConsole", logoSmall, "AirConsole Settings");

			// setup style for airconsole logo
			styleBlack.normal.background = bg;
			styleBlack.normal.textColor = Color.white;
			styleBlack.margin.top = 5;
			styleBlack.padding.right = 5;

			styleRedBold.normal.textColor = darkRed;
			styleRedBold.wordWrap = true;
			styleRedBold.fontStyle = FontStyle.Bold;
			styleRedBold.margin.top = 5;
			styleRedBold.padding.right = 5;
			styleRedBold.padding.left = 5;
			
			ApplyAndroidRequiredSettings();
			// SettingWindow.RemoveDisallowedUnityPackages();
		}

		[MenuItem("Window/AirConsole/Settings")]
		static void Init () {
			SettingWindow window = (SettingWindow)EditorWindow.GetWindow (typeof(SettingWindow));
			window.titleContent = titleInfo;
			window.Show ();
		}

		void OnGUI () {
			// show logo & version
			EditorGUILayout.BeginHorizontal (styleBlack, GUILayout.Height (30));
			GUILayout.Label (logo, GUILayout.Width (128), GUILayout.Height (30));
			GUILayout.FlexibleSpace ();
			GUILayout.Label ("v" + Settings.VERSION, styleBlack);
			EditorGUILayout.EndHorizontal ();

			GUILayout.Label ("AirConsole Settings", EditorStyles.boldLabel);

			Settings.webSocketPort = EditorGUILayout.IntField ("Websocket Port", Settings.webSocketPort, GUILayout.MaxWidth (200));
			EditorPrefs.SetInt ("webSocketPort", Settings.webSocketPort);

			Settings.webServerPort = EditorGUILayout.IntField ("Webserver Port", Settings.webServerPort, GUILayout.MaxWidth (200));
			EditorPrefs.SetInt ("webServerPort", Settings.webServerPort);

			EditorGUILayout.LabelField ("Webserver is running", Extentions.webserver.IsRunning ().ToString ());

			GUILayout.BeginHorizontal ();

			GUILayout.Space (150);
			if (GUILayout.Button ("Stop", GUILayout.MaxWidth (60))) {
				Extentions.webserver.Stop ();
			}
			if (GUILayout.Button ("Restart", GUILayout.MaxWidth (60))) {
				Extentions.webserver.Restart ();
			}

			GUILayout.EndHorizontal ();
			
			EditorGUILayout.Space();
			debugFoldout = EditorGUILayout.Foldout (debugFoldout, "Debug Configuration", true);
			if(debugFoldout) DrawDebugFoldout();
			
			EditorGUILayout.Space();
			androidFoldout = EditorGUILayout.Foldout (androidFoldout, "Android Configuration", true);
			if(androidFoldout) DrawAndroidFoldout();
			
			EditorGUILayout.Space();
			webglFoldout = EditorGUILayout.Foldout (webglFoldout, "WebGL Configuration", true);
			if(webglFoldout) DrawWebGLFoldout();
			
			DrawFooter();

			SettingWindow.ApplyDefaultWebGLSettings();
			SettingWindow.ApplyAndroidRequiredSettings();
		}

		private void DrawFooter()
		{
			EditorGUILayout.BeginHorizontal(styleBlack);

			GUILayout.FlexibleSpace();
			if(GUILayout.Button("Reset Settings", GUILayout.MaxWidth(110)))
			{
				Extentions.ResetDefaultValues();
			}

			GUILayout.EndHorizontal();
		}

		private void DrawDebugFoldout()
		{
			groupEnabled = EditorGUILayout.BeginToggleGroup ("Debug Settings", groupEnabled);

			Settings.debug.info = EditorGUILayout.Toggle ("Info", Settings.debug.info);
			EditorPrefs.SetBool ("debugInfo", Settings.debug.info);

			Settings.debug.warning = EditorGUILayout.Toggle ("Warning", Settings.debug.warning);
			EditorPrefs.SetBool ("debugWarning", Settings.debug.warning);

			Settings.debug.error = EditorGUILayout.Toggle ("Error", Settings.debug.error);
			EditorPrefs.SetBool ("debugError", Settings.debug.error);

			EditorGUILayout.EndToggleGroup ();
		}

		private void DrawAndroidFoldout()
		{
			EditorGUILayout.BeginVertical();
			GUILayout.Label ("Required Settings", EditorStyles.boldLabel);
			
			bool requiresGradle = !Application.unityVersion.Contains("202") || Application.unityVersion.Contains("2020");
			// launcher gradle
			EditorGUILayout.BeginHorizontal();
			GUI.enabled = requiresGradle;// && UnityEditor.pu;
			GUILayout.Toggle(true, "Enable Custom Launcher Gradle Template");
			GUI.enabled = true;
			EditorGUILayout.EndHorizontal();
			if(!requiresGradle)
			{
				GUILayout.Label("With Unity 2021 and higher, AirConsoles custom gradle configuration is no longer required.");
				GUILayout.Label("Only enable this if you have your own custom launcher gradle template", styleRedBold);
			}
			
			// main gradle
			EditorGUILayout.BeginHorizontal();
			GUI.enabled = requiresGradle;
			// Debug.LogWarning($"Main: {EditorUserBuildSettings.GetPlatformSettings("Android", "BuildSubtarget")}; Launcher: {EditorUserBuildSettings.GetPlatformSettings("buildLauncherAppBundle", "true")}");
			// Debug.LogWarning($"Main2: {EditorUserBuildSettings.GetPlatformSettings("Editor","AndroidCustomMainGradleTemplate")}; Launcher: {EditorUserBuildSettings.GetPlatformSettings("AndroidUseCustomMainGradleTemplate", "true")}");
			// Debug.LogWarning($"Custom Main Template enabled: {EditorUserBuildSettings.GetPlatformSettings("Android", "buildAppBundle")} or1 {EditorUserBuildSettings.GetPlatformSettings("Android", "m_CustomLauncherGradleTemplate")}");
			GUILayout.Toggle(true, "Enable Custom Main Gradle Template");
			GUI.enabled = true;
			EditorGUILayout.EndHorizontal();
			if(!requiresGradle)
			{
				GUILayout.Label("With Unity 2021 and higher, AirConsoles custom gradle configuration is no longer required.");
				GUILayout.Label("Only enable this if you have your own custom main gradle template", styleRedBold);
			}
			
			EditorGUILayout.Space(20);
			
			GUILayout.Label ("Recommended Settings", EditorStyles.boldLabel);
			
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Android Blit Type:");
			PlayerSettings.Android.blitType = (AndroidBlitType)EditorGUILayout.EnumPopup(PlayerSettings.Android.blitType);
			EditorGUILayout.EndHorizontal();
			if(PlayerSettings.Android.blitType != AndroidBlitType.Auto && PlayerSettings.Android.blitType!= AndroidBlitType.Never)
			{
				GUILayout.Label("Unless absolutely required after tests on AndroidTV hardware it is recommended to configure Blit Type to be \"Auto\" or \"Never\"", styleRedBold);
			}
			
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Incremental GC:");
			PlayerSettings.gcIncremental = EditorGUILayout.Toggle(PlayerSettings.gcIncremental);
			EditorGUILayout.EndHorizontal();
			if(!PlayerSettings.gcIncremental)
			{
				GUILayout.Label("Unless your tests on AndroidTV show a negative impact it is recommended to enable the incremental GC", styleRedBold);
			}
			
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Code Stripping Level:");
			ManagedStrippingLevel currentLevel = PlayerSettings.GetManagedStrippingLevel(BuildTargetGroup.Android);
			ManagedStrippingLevel newLevel = (ManagedStrippingLevel)EditorGUILayout.EnumPopup(currentLevel);
			if(currentLevel != newLevel)
			{
				PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, newLevel);
			}
			EditorGUILayout.EndHorizontal();
			if(newLevel != ManagedStrippingLevel.High)
			{
				GUILayout.Label("Unless your tests on AndroidTV show a negative impact it is recommended to always use High Managed Stripping.", styleRedBold);
				GUILayout.Label("If you face issues, check your Link.xml file to not strip required functionality.", styleRedBold);
			}
			
			if(!PlayerSettings.GetMobileMTRendering(BuildTargetGroup.Android))
			{
				if(!EditorUtility.DisplayDialog("Verification",
                                                "You do currently not have Multithreaded Rendering enabled.\nPlease confirm that this is required as it decreases performance", "Yes", "No"))
                {
                    Debug.Log("Enable Multithreaded Rendering");
                    PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android,true);
                }
                else
                {
                    Debug.LogWarning("User declined to enable Mixed Reality Rendering");
                }
			}

			if(!PlayerSettingsHelper.GetIsLowLightmapEncodingQualityForPlatformGroup(BuildTargetGroup.Android))
			{
				if(!EditorUtility.DisplayDialog("Verification",
                                                "You do currently not have Low Lightmap Encoding Quality enabled for Android.\nPlease confirm that you do not use Lightmaps in ES2 otherwise you will face issues", "Yes", "No"))
                {
                    Debug.Log("Enable Low Lightmap Encoding Quality");
                    PlayerSettingsHelper.SetLowLightmapEncodingQualityForPlatformGroup(BuildTargetGroup.Android);
                }
				else
				{
					Debug.LogWarning("User declined to set Lightmap Encoding to low quality for ES2");
				}
			}

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("API Compatibility Level:");
			ApiCompatibilityLevel currentApiLevel = PlayerSettings.GetApiCompatibilityLevel(BuildTargetGroup.Android);
			ApiCompatibilityLevel newApiLevel = (ApiCompatibilityLevel)EditorGUILayout.EnumPopup(currentApiLevel);
			if(currentApiLevel != newApiLevel)
			{
				Debug.Log($"Set API Compatibility Level to {newApiLevel}");
				PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Android, newApiLevel);
			}
			EditorGUILayout.EndHorizontal();
			if(newApiLevel != ApiCompatibilityLevel.NET_Standard_2_0)
			{
				GUILayout.Label("Unless your tests on AndroidTV show a negative impact it is recommended to use .NET Standard as it increases performance and decreases the size.", styleRedBold);
			}
			
			EditorGUILayout.EndVertical();
		}

		private void DrawWebGLFoldout()
		{
			EditorGUILayout.BeginVertical();
			GUILayout.Label ("Required Settings", EditorStyles.boldLabel);
			
			
			EditorGUILayout.Space(20);
			GUILayout.Label ("Recommended Settings", EditorStyles.boldLabel);
			
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Incremental GC:");
			PlayerSettings.gcIncremental = EditorGUILayout.Toggle(PlayerSettings.gcIncremental);
			EditorGUILayout.EndHorizontal();
			if(!PlayerSettings.gcIncremental)
			{
				GUILayout.Label("Unless your tests on WebGL show a negative impact it is recommended to enable the incremental GC", styleRedBold);
			}
			
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Code Stripping Level:");
			ManagedStrippingLevel currentStrippingLevel = PlayerSettings.GetManagedStrippingLevel(BuildTargetGroup.WebGL);
			ManagedStrippingLevel newStrippingLevel = (ManagedStrippingLevel)EditorGUILayout.EnumPopup(currentStrippingLevel);
			if(currentStrippingLevel != newStrippingLevel)
			{
				Debug.Log($"Update managed stripping level to {newStrippingLevel}");
				PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.WebGL, newStrippingLevel);
			}
			EditorGUILayout.EndHorizontal();
			if(newStrippingLevel != ManagedStrippingLevel.High)
			{
				GUILayout.Label("Unless your tests on WebGL show a negative impact it is recommended to always use High Managed Stripping.", styleRedBold);
				GUILayout.Label("If you face issues, check your Link.xml file to not strip required functionality.", styleRedBold);
			}

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("API Compatibility Level:");
			ApiCompatibilityLevel currentApiLevel = PlayerSettings.GetApiCompatibilityLevel(BuildTargetGroup.WebGL);
			ApiCompatibilityLevel newApiLevel = (ApiCompatibilityLevel)EditorGUILayout.EnumPopup(currentApiLevel);
			if(currentApiLevel != newApiLevel)
			{
				Debug.Log($"Set API Compatibility Level to {newApiLevel}");
				PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.WebGL, newApiLevel);
			}
			EditorGUILayout.EndHorizontal();
			if(newApiLevel != ApiCompatibilityLevel.NET_Standard_2_0)
			{
				GUILayout.Label("Unless your tests on WebGL show a negative impact it is recommended to use .NET Standard as it increases performance and decreases the size.", styleRedBold);
			}
			
			EditorGUILayout.EndVertical();
		}
		
		internal static void ApplyAndroidRequiredSettings()
		{
			// TODO: required or recommended?
			PlayerSettings.Android.optimizedFramePacing = true;

			if(PlayerSettings.Android.androidTargetDevices != AndroidTargetDevices.PhonesTabletsAndTVDevicesOnly)
			{
				Debug.Log("Configure Android target devices to AndroidTV and Phones/Tablets only, removing ChromeOS");
				PlayerSettings.Android.androidTargetDevices = AndroidTargetDevices.PhonesTabletsAndTVDevicesOnly;
			}
			
			if(PlayerSettings.Android.fullscreenMode != FullScreenMode.FullScreenWindow)
			{
				Debug.Log("Enable Android Fullscreen Mode");
				PlayerSettings.Android.fullscreenMode = FullScreenMode.FullScreenWindow;
			}

			if(!PlayerSettings.Android.startInFullscreen)
			{
				Debug.Log("Enable Android Start in Fullscreen");
                PlayerSettings.Android.startInFullscreen = true;
			}

			if(PlayerSettings.defaultInterfaceOrientation != UIOrientation.AutoRotation)
			{
				Debug.Log("Enable Android Default Orientation");
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
			}
			
			PlayerSettings.allowedAutorotateToLandscapeLeft = true;
			PlayerSettings.allowedAutorotateToLandscapeRight = true;
			PlayerSettings.allowedAutorotateToPortrait = false;
			PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
			
			if(!PlayerSettings.Android.androidTVCompatibility)
			{
				Debug.Log("Enable Android TV Compatibility flag");
				PlayerSettings.Android.androidTVCompatibility = true;
			}

			if(PlayerSettingsHelper.GetAndroidGamepadSupportLevel() != AndroidGamepadSupportLevel.SupportsDPad)
			{
				Debug.Log("Reduce Android Gamepad Support to only DPad");
                PlayerSettingsHelper.SetAndroidGamepadSupportLevel(AndroidGamepadSupportLevel.SupportsDPad);
			}

			if(!PlayerSettings.Android.androidIsGame)
			{
				Debug.Log("Enable Android is Game PlayerSettings flag");
				PlayerSettings.Android.androidIsGame = true;
			}

			if(!PlayerSettings.Android.forceInternetPermission)
			{
				Debug.Log("Enable Android Force Internet Permission flag");
                PlayerSettings.Android.forceInternetPermission = true;
			}

			if(PlayerSettings.Android.chromeosInputEmulation)
			{
				Debug.Log("Disable Android ChromeOS Input Emulation flag");
                PlayerSettings.Android.chromeosInputEmulation = false;
			}
			
			if((int)PlayerSettings.Android.minSdkVersion > 22)
			{
	
                Debug.LogWarning($"Set Android API Level to 22");
                PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)22;
          
			}
			
			if((int)PlayerSettings.Android.targetSdkVersion < 33 && PlayerSettings.Android.targetSdkVersion != AndroidSdkVersions.AndroidApiLevelAuto)
			{
				Debug.LogWarning($"Set Target Android SDK Version to API Level 33");
                PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)33;
			}

			GraphicsDeviceType[] androidGraphicsAPIs = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
			bool isOpenGles2Enabled = androidGraphicsAPIs.Contains(GraphicsDeviceType.OpenGLES2) && !PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android);
			if(!isOpenGles2Enabled)
			{
				Debug.Log("Enable OpenES2 in Android GraphicsAPIs and disable DefaultGraphicsAPI");
				PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, androidGraphicsAPIs.Append(GraphicsDeviceType.OpenGLES2).ToArray());
				PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
			}

			if(!PlayerSettings.stripEngineCode)
			{
				Debug.Log("Enable Engine Code stripping");
                PlayerSettings.stripEngineCode = true;
			}

			if(PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) != ScriptingImplementation.IL2CPP)
			{
				Debug.Log("Enable IL2CPP Scripting Backend");
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
			}
			if((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARMv7) == 0)
			{
				Debug.Log("Enable ARMv7 Architecture");
                PlayerSettings.Android.targetArchitectures |= AndroidArchitecture.ARMv7;
			}
			if((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) == 0)
			{
				Debug.Log("Enable ARM64 Architecture");
				PlayerSettings.Android.targetArchitectures |= AndroidArchitecture.ARM64;
			}
			
			PlayerSettings.Android.targetArchitectures &= ~AndroidArchitecture.X86 & ~AndroidArchitecture.X86_64;
		}

		internal static void CheckUnityVersionForBuildSupport()
		{
			if(!Application.unityVersion.Contains("202") && 
                EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android && 
                !EditorUserBuildSettings.exportAsGoogleAndroidProject)
			{
				if(EditorUtility.DisplayDialog("IMPORTANT",
				                               "Unity 2019 requires Android Studio to use Android SDK 30 features and Gradle 5.6. We will update the Build Settings now to export an Android Studio Project",
				                               "I understand, please build an Android Studio project",
				                               "I prefer to update the project to Unity 2020.3 or newer!"))
				{
					EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
				}
			}
		}
		
		internal static bool AndroidBuildNotAllowed;
		// static RemoveRequest Request;
		static ListRequest Request;
		static List<string> packages = new List<string>();
		
		[InitializeOnLoadMethod]
		internal static void RemoveDisallowedUnityPackages()
		{
			// TODO: Analytics should be allowed but not PII, independent of game dev wishes?
			// packages.Add("com.unity.analytics");
			// TODO: ID legacy ads
			// packages.Add("");
			// // // TODO: ID ads
			packages.Add("com.unity.ads.ios-support");
			// // TODO: ID new ads
			packages.Add("com.unity.ads");
			// // TODO: ID legacy iap?
			// packages.Add("");
			// TODO: ID iap
			packages.Add("com.unity.purchasing");  
			// TODO: don't forget to also remove BillingMode.json and its meta.
			// Request = Client.Remove(packages.Dequeue());
			Request = Client.List(true, true);
			EditorApplication.update -= Progress;
			EditorApplication.update += Progress;
			// EditorApplication.LockReloadAssemblies();
		}
		static void Progress()
		{
			if (Request.IsCompleted)
			{
				switch (Request.Status)
				{
					case StatusCode.Success:
					{
						// foreach (PackageInfo packageInfo in SettingWindow.Request.Result)
						// {
						// 	Debug.Log(packageInfo.packageId);
						// }
						string notAllowedPackage = string.Empty;
						foreach(PackageInfo packageInfo in Request.Result)
						{
							foreach (string package in SettingWindow.packages)
							{
								if(packageInfo.packageId.Contains(package))
								{
									AndroidBuildNotAllowed = true;
									notAllowedPackage = package;
									Debug.LogError($"Not allowed package for AndroidTV builds found: {notAllowedPackage}");
									break;
								}
							}

							if(SettingWindow.AndroidBuildNotAllowed) break;
						}
						
						if(AndroidBuildNotAllowed && EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
						{
							EditorUtility.DisplayDialog("Error", $"We found package {notAllowedPackage}. This is not allowed for AndroidTV releases of AirConsole", "I understand and will remove it.");
						}
						// Debug.Log("Removed: " + Request.PackageIdOrName);
						// if(packages.Count > 0)
						// {
						// 	Request = Client.Remove(packages.Dequeue());
						// }
						// else
						// {
						// 	EditorApplication.update -= Progress;
						// }
						break;
					}
					case StatusCode.Failure:
					{
						Debug.LogError(Request.Error.message);
						break;
					}
				}
				EditorApplication.update -= Progress;
				// EditorApplication.UnlockReloadAssemblies();
			}
		}

		[MenuItem("Window/AirConsole/Recommendations/Android")]
		internal static void QueryAndApplyRecommendedAndroidSettings()
		{
			if(PlayerSettings.GetManagedStrippingLevel(BuildTargetGroup.Android) != ManagedStrippingLevel.High)
			{
				if(!EditorUtility.DisplayDialog("Verification",
				                                "You do currently not have HIGH Code Stripping Level active. Please confirm that this is required", "Yes", "No"))
				{
					Debug.Log("Set Managed Stripping Level to High");
					PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.High);
				}
				else
				{
					Debug.LogWarning("User declined to set Managed Stripping Level to High");
				}
			}
			
			if(!PlayerSettings.GetMobileMTRendering(BuildTargetGroup.Android))
			{
				if(!EditorUtility.DisplayDialog("Verification",
                                                "You do currently not have Multithreaded Rendering enabled.\nPlease confirm that this is required as it decreases performance", "Yes", "No"))
                {
                    Debug.Log("Enable Multithreaded Rendering for Android");
                    PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android,true);
                }
                else
                {
                    Debug.LogWarning("User declined to enable Multithreaded Rendering on Android");
                }
			}

			if(!PlayerSettingsHelper.GetIsLowLightmapEncodingQualityForPlatformGroup(BuildTargetGroup.Android))
			{
				if(!EditorUtility.DisplayDialog("Verification",
                                                "You do currently not have Low Lightmap Encoding Quality enabled for Android.\nPlease confirm that you do not use Lightmaps in ES2 otherwise you will face issues", "Yes", "No"))
                {
                    Debug.Log("Enable Low Lightmap Encoding Quality");
                    PlayerSettingsHelper.SetLowLightmapEncodingQualityForPlatformGroup(BuildTargetGroup.Android);
                }
				else
				{
					Debug.LogWarning("User declined to set Lightmap Encoding to low quality for ES2");
				}
			}

			if(PlayerSettings.GetApiCompatibilityLevel(BuildTargetGroup.Android) != ApiCompatibilityLevel.NET_Standard_2_0)
			{
				if(!EditorUtility.DisplayDialog("Verification",
                                                "You do currently not have API Compatibility Level set to.NET Standard 2.0.\nPlease confirm that this is required as it decreases performance and increases the size", "Yes", "No"))
                {
                    Debug.Log("Set API Compatibility Level to.NET Standard 2.0");
                    PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Android, ApiCompatibilityLevel.NET_Standard_2_0);
                }
                else
                {
                    Debug.LogWarning("User declined to set API Compatibility Level to.NET Standard 2.0");
                }
			}
		}
		
		private static void ApplyDefaultWebGLSettings()
		{
			if(PlayerSettings.WebGL.template.Equals("APPLICATION:Default") || PlayerSettings.WebGL.template.Equals("APPLICATION:Minimal"))
			{
				string originalTemplate = PlayerSettings.WebGL.template;
				PlayerSettings.WebGL.template = Application.unityVersion.Substring(0, 3) == "202" ? "APPLICATION:AirConsole-2020" : "APPLICATION:AirConsole";
				EditorUtility.DisplayDialog("Information", $"Updated WebGL Template from {originalTemplate.Replace("APPLICATION", "")} to {PlayerSettings.WebGL.template.Replace("APPLICATION:","")}", "OK");
			}
		}
		
	}
	
	internal static class PlayerSettingsHelper
	{
		public static void SetLowLightmapEncodingQualityForPlatformGroup(BuildTargetGroup platformGroup)
		{
			// Get the internal static method.
			MethodInfo method = typeof(PlayerSettings).GetMethod("SetLightmapEncodingQualityForPlatformGroup", BindingFlags.Static | BindingFlags.NonPublic);

			// Get the internal enum type.
			Type enumType = typeof(PlayerSettings).Assembly.GetType("UnityEditor.LightmapEncodingQuality");
			
			// Get the field value of the given enum value.
			int qualityLevelValue = (int)enumType.GetField("Low").GetValue(null);
			
			// Invoke the method with the given parameters.
			method.Invoke(null, new object[] { platformGroup, qualityLevelValue });
		}
		
		public static bool GetIsLowLightmapEncodingQualityForPlatformGroup(BuildTargetGroup platformGroup)
		{
			// Get the internal static method.
			MethodInfo method = typeof(PlayerSettings).GetMethod("GetLightmapEncodingQualityForPlatformGroup", BindingFlags.Static | BindingFlags.NonPublic);

			// Invoke the method with the given parameters.
			object qualityLevel = method.Invoke(null, new object[] { platformGroup });

			return qualityLevel.ToString().ToLower() == "low";
		}
		
		public static AndroidGamepadSupportLevel GetAndroidGamepadSupportLevel()
		{
			PropertyInfo property = typeof(PlayerSettings.Android).GetProperty("androidGamepadSupportLevel", BindingFlags.Static | BindingFlags.NonPublic);
			int gamepadSupportLevel = (int)property.GetValue(null);
			return (AndroidGamepadSupportLevel)gamepadSupportLevel;
		}
		
		public static void SetAndroidGamepadSupportLevel(AndroidGamepadSupportLevel gamepadSupportLevel)
		{
			PropertyInfo property = typeof(PlayerSettings.Android).GetProperty("androidGamepadSupportLevel", BindingFlags.Static | BindingFlags.NonPublic);
			property.SetValue(null, (int)gamepadSupportLevel);
		}
	}
}
#endif
