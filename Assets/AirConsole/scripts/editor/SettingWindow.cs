#if !DISABLE_AIRCONSOLE
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Net;
using System.Text;
using UnityEditor.Build;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
using UnityEngine.Rendering;
using WebSocketSharp.Server;
using AndroidSdkVersions = UnityEditor.AndroidSdkVersions;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace NDream.AirConsole.Editor {
	public class SettingWindow : EditorWindow {

		#if !UNITY_2019_4_OR_NEWER
		// TODO(marc): Are we able to drop support for Unity < 2019?
		[InitializeOnLoadMethod]
		private static void UnsupportedUnityVersion()
		{
			if(EditorUtility.DisplayDialog("Unsupported Unity Version", "AirConsole only supports Unity 2019.4 or newer.", "I understand that AirConsole is being disabled"))
			{
				PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, "DISABLE_AIRCONSOLE;"+PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android));
				PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.WebGL, "DISABLE_AIRCONSOLE;"+PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.WebGL));
			}
		}
		#elif !UNITY_2020_3_OR_NEWER
		private const string AC_OLDVERSION_PREFS = "AirConsole_OldUnityVersion";
		[InitializeOnLoadMethod]
		private static void OldUnityVersion()
		{
			if(EditorPrefs.GetString(AC_OLDVERSION_PREFS, "") != Application.unityVersion && EditorUtility.DisplayDialog("Old Unity Version", "AirConsole recommends\n- 2020.3 LTS\n- 2021.3 LTS\n- 2022.3 LTS", "OK"))
			{
				EditorPrefs.SetString(AC_OLDVERSION_PREFS, Application.unityVersion);
			}
		}
		#endif
		
		[MenuItem("Window/AirConsole/Clear AC Prefs")]
		private static void ClearPrefs() {
#if !UNITY_2020_3_OR_NEWER
            EditorPrefs.DeleteKey(SettingWindow.AC_OLDVERSION_PREFS);
#endif
            EditorPrefs.DeleteKey(SettingWindow.AC_LATEST_VERSION_PREFS);
        }
		
		const string AC_LATEST_VERSION_PREFS = "AC_LATEST_VERSION";
		
		GUIStyle styleBlack = new GUIStyle ();
		GUIStyle updateBanner = new GUIStyle ();
		private Texture2D updateBannerBg;
		private GUIStyle styleRedBold = new GUIStyle();
		private Texture2D bg;
		private Texture logo;
		private Texture logoSmall;
		
		Color darkRed = new Color(0.7f, 0.0f, 0.0f);

		private readonly string AndroidPluginPath = Path.Combine("Assets", "Plugins", "Android");
		private bool androidFoldout;
		private bool webglFoldout;

		public void OnEnable () {
			// ReSharper disable once Unity.UnknownResource
			bg = (Texture2D)Resources.Load ("AirConsoleBg");
			// ReSharper disable once Unity.UnknownResource
			logo = (Texture)Resources.Load ("AirConsoleLogoText");
			// ReSharper disable once Unity.UnknownResource
			logoSmall = (Texture)Resources.Load ("AirConsoleLogoSmall");
			titleContent = new GUIContent ("AirConsole", logoSmall, "AirConsole Settings");

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

			updateBannerBg = MakeTex(1, 1, new Color(0.68f, 0.94f, 0f));
			updateBanner.normal.background = updateBannerBg;
			updateBanner.normal.textColor = Color.black;
			updateBanner.fontStyle = FontStyle.Bold;
			updateBanner.padding = new RectOffset(10, 20, 10, 10);

			ApplyAndroidRequiredSettings();
		}
		
		private Texture2D MakeTex(int width, int height, Color col) {
			Color[] pix = new Color[width * height];
			for (int i = 0; i < pix.Length; i++) {
				pix[i] = col;
			}
   
			Texture2D result = new Texture2D(width, height);
			result.SetPixels(pix);
			result.Apply();
			return result;
		}

		private void OnDisable()
		{
			bg = null;
			logo = null;
			logoSmall = null;
			updateBannerBg = null;
			Resources.UnloadUnusedAssets();
		}

		[MenuItem("Window/AirConsole/Settings")]
		static void Init ()
		{
			if(!EditorPrefs.HasKey(SettingWindow.AC_LATEST_VERSION_PREFS))
			{
				HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create("https://api.github.com/repos/AirConsole/airconsole-unity-plugin/releases");
                request.Method = "GET";
                request.ContentType = "application/json";
         
                request.Accept = "application/vnd.github+json";
                request.Headers.Add("X-GitHub-Api-Version: 2022-11-28");
                request.UserAgent = "AirConsole";
                
                try
                {
                    using(HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        Debug.Log("Publish Response: " + (int)response.StatusCode + ", " + response.StatusDescription);
                        if((int)response.StatusCode == 200)
                        {
	                        string json = "[]";
	                        using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
	                        {
		                        json = reader.ReadToEnd();
	                        }
	                        List<JToken> releases = JsonConvert.DeserializeObject<List<JToken>>(json);
							List<float> versions = releases
							                      ?.Where(t => t.HasValues && !string.IsNullOrEmpty(t["tag_name"].ToString()))
							                      .Select(t => t["tag_name"].ToString().Replace("v",""))
							                      .Select(float.Parse)
							                      .ToList();
							if(versions != null)
							{
								EditorPrefs.SetFloat(SettingWindow.AC_LATEST_VERSION_PREFS, versions.First());
							}
                        }
                    }
                }
                catch(Exception e)
                {
                    Debug.LogError(e.ToString());
                }
			}
			SettingWindow window = (SettingWindow)EditorWindow.GetWindow (typeof(SettingWindow));
			window.Show ();
		}

		void OnGUI () {
			if(!new[] { BuildTarget.Android, BuildTarget.WebGL }.Contains(EditorUserBuildSettings.activeBuildTarget))
			{
				GUILayout.Label("AirConsole only supports Android and WebGL.", styleRedBold);
				return;
			}

			GUI.enabled = !Application.isPlaying;
			
			// show logo & version
			EditorGUILayout.BeginHorizontal (styleBlack, GUILayout.Height (30));
			GUILayout.Label (logo, GUILayout.Width (128), GUILayout.Height (30));
			GUILayout.FlexibleSpace ();
			GUILayout.Label ("v" + Settings.VERSION, styleBlack);
			EditorGUILayout.EndHorizontal ();
			
			DrawUpdatedVersionNotification();
			
			DrawEditorSettings();
			
			EditorGUILayout.Space(20);
			EditorGUILayout.LabelField("Build Settings", EditorStyles.boldLabel);
			DrawGeneralSettings();
			
			EditorGUILayout.Space(20);
			androidFoldout = EditorGUILayout.Foldout (androidFoldout, "Android Configuration", true);
			if(androidFoldout) DrawAndroidFoldout();
			
			EditorGUILayout.Space(20);
			webglFoldout = EditorGUILayout.Foldout (webglFoldout, "WebGL Configuration", true);
			if(webglFoldout) DrawWebGLFoldout();
			EditorGUILayout.Space();
			
			DrawFooter();

			SettingWindow.ApplyDefaultWebGLSettings();
			SettingWindow.ApplyAndroidRequiredSettings();
		}

		private void DrawUpdatedVersionNotification()
		{
			if(EditorPrefs.HasKey(SettingWindow.AC_LATEST_VERSION_PREFS) &&
			   float.TryParse(Settings.VERSION, out float pluginVersion) && 
			   EditorPrefs.GetFloat(AC_LATEST_VERSION_PREFS, 0f) > pluginVersion)
			{
				EditorGUILayout.BeginHorizontal(updateBanner);
				EditorGUILayout.Space(20);
				GUILayout.Label("Newer Version");
				GUILayout.Label($"v{EditorPrefs.GetFloat(AC_LATEST_VERSION_PREFS)}",EditorStyles.boldLabel);
				GUILayout.Label("available");
				GUILayout.FlexibleSpace();
				if(GUILayout.Button("Download now"))
				{
					Application.OpenURL("https://github.com/AirConsole/airconsole-unity-plugin/releases");
				}
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.Space(20);
			}
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

		private void DrawEditorSettings()
		{
			
			GUILayout.Label ("Editor Connection Settings", EditorStyles.boldLabel);

			EditorGUILayout.BeginHorizontal();
			Settings.webSocketPort = EditorGUILayout.IntField ("Websocket Port", Settings.webSocketPort, GUILayout.MaxWidth (200));
			
			if(GUILayout.Button(new GUIContent("Force Reset", "Will stop existing Unity WebSocket Servers on this port"), GUILayout.MaxWidth(100)))
			{
				SettingWindow.ResetWebsocketServer();
			}
			EditorPrefs.SetInt ("webSocketPort", Settings.webSocketPort);
			EditorGUILayout.EndHorizontal();

			int newWebserverPort = EditorGUILayout.IntField ("Webserver Port", Settings.webServerPort, GUILayout.MaxWidth (200));
			if(newWebserverPort != Settings.webServerPort)
			{
				if(Extentions.webserver.IsRunning() && !Extentions.webserver.IsBlocked)
				{
					Extentions.webserver.Stop();
				}
				
				Settings.webServerPort = newWebserverPort;
				EditorPrefs.SetInt ("webServerPort", Settings.webServerPort);
				
				Extentions.webserver.Start();
			}

			if(Extentions.webserver.IsBlocked)
			{
				EditorGUILayout.LabelField ($"Webserver Port {Settings.webServerPort} already in use.\nAirConsole is not able to communicate with the Simulator", styleRedBold);
			}
			else
			{
				EditorGUILayout.LabelField ("Webserver is running", Extentions.webserver.IsRunning () .ToString ());

				GUILayout.BeginHorizontal ();

				GUILayout.Space (150);
				if (GUILayout.Button ("Stop", GUILayout.MaxWidth (60))) {
					Extentions.webserver.Stop ();
				}
				if (GUILayout.Button ("Restart", GUILayout.MaxWidth (60))) {
					Extentions.webserver.Restart ();
				}

				GUILayout.EndHorizontal ();
			}
			
			EditorGUILayout.Space();
			
			bool guiWasEnabled = GUI.enabled;
			GUI.enabled = true;
			GUILayout.Label("Editor Debug Logging Settings", EditorStyles.boldLabel);

			Settings.debug.info = EditorGUILayout.Toggle ("Log Info", Settings.debug.info);
			EditorPrefs.SetBool ("debugInfo", Settings.debug.info);

			Settings.debug.warning = EditorGUILayout.Toggle ("Log Warnings", Settings.debug.warning);
			EditorPrefs.SetBool ("debugWarning", Settings.debug.warning);

			Settings.debug.error = EditorGUILayout.Toggle ("Log Errors", Settings.debug.error);
			EditorPrefs.SetBool ("debugError", Settings.debug.error);
			GUI.enabled = guiWasEnabled;
		}

		private void DrawAndroidFoldout()
		{
			EditorGUILayout.Space(5);
			EditorGUILayout.BeginVertical();
			GUILayout.Label ("Required Settings", EditorStyles.boldLabel);
			
			bool requiresGradleExtension = !Application.unityVersion.Contains("202");

			DrawCustomMainGradleWidget(requiresGradleExtension);
			DrawCustomLauncherGradleWidget(requiresGradleExtension);

			EditorGUILayout.Space(20);
			
			GUILayout.Label ("Recommended Settings", EditorStyles.boldLabel);
			
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Android Blit Type:");
			PlayerSettings.Android.blitType = (AndroidBlitType)EditorGUILayout.EnumPopup(PlayerSettings.Android.blitType);
			EditorGUILayout.EndHorizontal();
			if(PlayerSettings.Android.blitType != AndroidBlitType.Auto && PlayerSettings.Android.blitType!= AndroidBlitType.Never)
			{
				GUILayout.Label("Unless your tests on AndroidTV hardware requires this, we recommended to use \"Never\" or \"Auto\"", styleRedBold);
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
				GUILayout.Label("We recommended to use High Code Stripping for the best game experience.", styleRedBold);
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

		private void DrawCustomLauncherGradleWidget(bool requiresGradleExtension)
		{
			bool launcherTemplateExists = File.Exists(Path.Combine(AndroidPluginPath, "launcherTemplate.gradle"));
			bool disabledLauncherTemplateExists = File.Exists(Path.Combine(AndroidPluginPath, "launcherTemplate.gradle.DISABLED"));
			string launcherTemplatePath = Path.Combine(AndroidPluginPath, "launcherTemplate.gradle");
			string disabledLauncherTemplatePath = Path.Combine(AndroidPluginPath, "launcherTemplate.gradle.DISABLED");
			if(disabledLauncherTemplateExists || launcherTemplateExists)
			{
				GUI.enabled = !Application.isPlaying &&
				              (requiresGradleExtension || launcherTemplateExists);
				
				// The Gradle Logic is based on the implementation in UnityEditor.Modules.PlayerSettingsEditorExtension
				EditorGUILayout.BeginHorizontal();

				bool launcherGradleEnabled = GUILayout.Toggle(launcherTemplateExists,
				                                              new GUIContent("Enable Custom Launcher Gradle Template",
				                                                             "With Unity 2021 and higher, AirConsoles custom gradle configuration is no longer required."));
				if(launcherGradleEnabled)
				{
					if(disabledLauncherTemplateExists)
					{
						File.Move(disabledLauncherTemplatePath, launcherTemplatePath);
						AssetDatabase.Refresh();
					}
				}
				else
				{
					if(launcherTemplateExists)
					{
						if(disabledLauncherTemplateExists)
						{
							try
							{
								File.Delete(disabledLauncherTemplatePath);
							}
							catch (IOException e)
							{
								Debug.LogException(e);
							}
						}
						File.Move(launcherTemplatePath, disabledLauncherTemplatePath);
						AssetDatabase.Refresh();
					}
				}
				EditorGUILayout.EndHorizontal();
				if(!requiresGradleExtension && disabledLauncherTemplateExists && GUI.enabled)
				{
					GUILayout.Label("Only enable this if you have your own custom main gradle template", styleRedBold);
				}
				GUI.enabled = !Application.isPlaying;
			}
		}

		private void DrawCustomMainGradleWidget(bool requiresGradleExtension)
		{
			bool mainTemplateExists = File.Exists(Path.Combine(AndroidPluginPath, "mainTemplate.gradle"));
			bool disabledMainTemplateExists = File.Exists(Path.Combine(AndroidPluginPath, "mainTemplate.gradle.DISABLED"));
			string mainTemplatePath = Path.Combine(AndroidPluginPath, "mainTemplate.gradle");
			string disabledMainTemplatePath = Path.Combine(AndroidPluginPath, "mainTemplate.gradle.DISABLED");
			if(disabledMainTemplateExists || mainTemplateExists)
			{
				GUI.enabled = !Application.isPlaying &&
				              (requiresGradleExtension || mainTemplateExists);
				
				// The Gradle Logic is based on the implementation in UnityEditor.Modules.PlayerSettingsEditorExtension
				EditorGUILayout.BeginHorizontal();
				bool mainGradleEnabled = GUILayout.Toggle(mainTemplateExists,
				                                          new GUIContent("Enable Custom Main Gradle Template",
				                                                         "With Unity 2021 and higher, AirConsoles custom gradle configuration is no longer required."));
				if(mainGradleEnabled)
				{
					if(disabledMainTemplateExists)
					{
						File.Move(disabledMainTemplatePath,mainTemplatePath);
						AssetDatabase.Refresh();
					}
				}
				else
				{
					if(mainTemplateExists)
					{
						if(disabledMainTemplateExists)
						{
							try
							{
								File.Delete(disabledMainTemplatePath);
							}
							catch (IOException e)
							{
								Debug.LogException(e);
							}
						}
						File.Move(mainTemplatePath, disabledMainTemplatePath);
						AssetDatabase.Refresh();
					}
				}
				EditorGUILayout.EndHorizontal();
				if(!requiresGradleExtension && disabledMainTemplateExists && GUI.enabled)
				{
					GUILayout.Label("Only enable this if you have your own custom launcher gradle template", styleRedBold);
				}
				GUI.enabled = !Application.isPlaying;
			}
		}

		private void DrawGeneralSettings()
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.LabelField("Incremental GC:");
				PlayerSettings.gcIncremental = EditorGUILayout.Toggle(PlayerSettings.gcIncremental);
			}
			if(!PlayerSettings.gcIncremental)
			{
				GUILayout.Label("We recommended to enable this for the best game experience", styleRedBold);
			}
		}

		private void DrawWebGLFoldout()
		{
			EditorGUILayout.Space(5);
			EditorGUILayout.BeginVertical();
			GUILayout.Label ("Required Settings", EditorStyles.boldLabel);
			
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Compression Format:");
			WebGLCompressionFormat currentCompression = PlayerSettings.WebGL.compressionFormat;
			WebGLCompressionFormat newCompression = (WebGLCompressionFormat)EditorGUILayout.EnumPopup(currentCompression);
			if(currentCompression != newCompression)
			{
				Debug.Log($"Update WebGL compression to {newCompression}");
				PlayerSettings.WebGL.compressionFormat = newCompression;
			}
			EditorGUILayout.EndHorizontal();
			if(newCompression == WebGLCompressionFormat.Disabled)
			{
				GUILayout.Label("We recommended to GZip compression for the best game experience.", styleRedBold);
			}
			if(newCompression == WebGLCompressionFormat.Brotli)
			{
				GUILayout.Label("ERROR: AirConsole does not support Brotli compression!", styleRedBold);
			}
			
			EditorGUILayout.Space(20);
			GUILayout.Label ("Recommended Settings", EditorStyles.boldLabel);
			
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
				GUILayout.Label("We recommended to use High Code Stripping for the best game experience.", styleRedBold);
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
				Debug.Log("Reduce Android Gamepad Support to minimum: DPad only");
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
				                               "Unity 2019 requires Android Studio to use the required Android SDK 30 features and Gradle 5.6. We will update the Build Settings now to export an Android Studio Project",
				                               "I understand, please update the build settings to export an Android Studio project",
				                               "I prefer to update the project to Unity 2020.3 or newer!"))
				{
					EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
				}
				else
				{
					throw new BuildFailedException("User aborted build");
				}
			}
		}
		
		internal static bool AndroidBuildNotAllowed;
		static ListRequest Request;
		static readonly List<string> packages = new List<string>();
		static readonly List<string> packagesFound = new List<string>();
		
		[InitializeOnLoadMethod]
		internal static void ReportDisallowedUnityPackages()
		{
			packages.Add("com.unity.ads.ios-support");
			packages.Add("com.unity.ads");
			packages.Add("com.unity.purchasing");
			packages.Add("com.unity.purchasing.udp");
			Request = Client.List(true, true);
			SettingWindow.packagesFound.Clear();
			EditorApplication.update -= Progress;
			EditorApplication.update += Progress;
		}
		
		static void Progress()
		{
			if (Request.IsCompleted)
			{
				if(EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
				{
					switch (Request.Status)
					{
						case StatusCode.Success:
						{
							foreach (PackageInfo packageInfo in Request.Result)
							{
								SettingWindow.packages.Where(package => packageInfo.packageId.StartsWith($"{package}@"))
								             .ToList()
								             .ForEach(package => SettingWindow.packagesFound.Add(package));
							}
			
							AndroidBuildNotAllowed = SettingWindow.packagesFound.Count > 0;
							if(AndroidBuildNotAllowed && EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
							{
								if(!EditorUtility.DisplayDialog("AirConsole Android Error",
								                               $"To deploy to AirConsole AndroidTV, please remove the following packages from the PackageManager:\n-{string.Join("\n-", SettingWindow.packagesFound)}",
								                               $"I understand and will remove {(SettingWindow.packagesFound.Count == 1 ? "it" : "them")}!",
								                               "Please remove them for me"))
								{
									string manifestPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Packages", "manifest.json"));
									Dictionary<string, Dictionary<string,string>> manifest = JsonConvert.DeserializeObject<Dictionary<string,Dictionary<string,string>>>(File.ReadAllText(manifestPath));
									SettingWindow.packagesFound.ForEach(package => manifest["dependencies"].Remove(package));
									File.WriteAllText(manifestPath, JsonConvert.SerializeObject(manifest, Formatting.Indented));
								}
								else
								{
								    LogFoundDisallowedPackages();
								}
							}
							
							break;
						}
						case StatusCode.Failure:
						{
							Debug.LogError(Request.Error.message);
							break;
						}
					}
				}
				EditorApplication.update -= Progress;
			}
		}

		internal static void LogFoundDisallowedPackages()
		{
			SettingWindow.packagesFound.ForEach(it => Debug.LogError($"AirConsole Android Error: Please remove package \"{it}\" from 'Window > Package Manager'"));
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
		
		[MenuItem("Window/AirConsole/Recommendations/WebGL")]
		internal static void QueryAndApplyRecommendedWebGLSettings()
		{
			if(PlayerSettings.WebGL.compressionFormat != WebGLCompressionFormat.Gzip)
			{
				if(EditorUtility.DisplayDialog("Verification",
				                                "You do currently not use GZip Compression. This is", "a release", "a test build"))
				{
					Debug.Log("Set WebGL compression to Gzip");
					PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
				}
				else
				{
					Debug.LogWarning("User declined to set WebGL compression to Gzip");
				}
			}
			
			if(PlayerSettings.WebGL.exceptionSupport != WebGLExceptionSupport.None)
			{
				if(EditorUtility.DisplayDialog("Verification",
				                                "You do currently not have WebGL exception support disabled. This is", "a release", "a test build"))
				{
					Debug.Log("Disable WebGL exception support");
					PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
				}
				else
				{
					Debug.LogWarning("WebGL exception support left enabled for test build");
				}
			}
			
			if(PlayerSettings.WebGL.compressionFormat == WebGLCompressionFormat.Brotli)
			{
				if(!EditorUtility.DisplayDialog("Error",
				                                "AirConsole does not currently support Brotli compression for WebGL builds", "Cancel"))
				{
					throw new Exception("AirConsole does not currently support Brotli compression for WebGL builds");
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

		private static void ResetWebsocketServer()
		{
			WebSocketServer wsServer = new WebSocketServer(Settings.webSocketPort);
			wsServer.Stop();
		}
	}
}
#endif
