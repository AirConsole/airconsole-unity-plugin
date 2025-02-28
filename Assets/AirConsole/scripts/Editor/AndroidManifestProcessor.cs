#if !DISABLE_AIRCONSOLE
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEngine;
using UnityEditor.Build;

namespace NDream.AirConsole.Editor {
    public class AndroidManifestProcessor : IPreprocessBuildWithReport {
        public int callbackOrder => 0;

        [MenuItem("Tools/AirConsole/Process Android Manifest")]
        public static void TestManifestMenuItem() {
            UpdateAndroidManifest();
        }

        public void OnPreprocessBuild(UnityEditor.Build.Reporting.BuildReport report) {
            if (report.summary.platform != BuildTarget.Android) {
                return;
            }

            UpdateAndroidManifest();
        }

        private static void CreateDefaultUnityManifest(string targetPath) {
            string manifestPath = Path.Combine(Path.GetDirectoryName(EditorApplication.applicationPath), "PlaybackEngines", "AndroidPlayer",
                "Apk",
                "UnityManifest.xml");
            File.Copy(manifestPath, targetPath);
        }

        private static void UpdateAndroidManifest() {
            string manifestPath = GetManifestPath();
            EnsureCustomManifestExists(manifestPath);

            if (File.Exists(manifestPath)) {
                UpgradeManifest(manifestPath);
                Debug.Log("AirConsole: Successfully upgraded AndroidManifest.xml");
            } else {
                Debug.LogWarning(
                    "AirConsole: AndroidManifest.xml not found at expected path. Make sure custom manifest generation is enabled.");
            }
        }

        private static void SetCustomManifestActive(bool active) {
            SerializedObject playerSettings = new(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);

            SerializedProperty filterTouchesProperty = playerSettings.FindProperty("useCustomMainManifest");
            filterTouchesProperty.boolValue = active;

            playerSettings.ApplyModifiedProperties();
            AssetDatabase.Refresh();
        }

        private static bool GetCustomManifestActive() {
            SerializedObject playerSettings = new(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);

            SerializedProperty filterTouchesProperty = playerSettings.FindProperty("useCustomMainManifest");
            return filterTouchesProperty.boolValue;
        }

        private static void EnsureCustomManifestExists(string manifestPath) {
            if (!GetCustomManifestActive()) {
                Debug.Log("AirConsole: Enabling custom AndroidManifest.xml generation");
                SetCustomManifestActive(true);

                string manifestDir = Path.Combine(Application.dataPath, "Plugins", "Android");
                if (!Directory.Exists(manifestDir)) {
                    Directory.CreateDirectory(manifestDir);
                }
            }

            if (!File.Exists(manifestPath)) {
                CreateDefaultUnityManifest(manifestPath);
                if (int.Parse(Application.unityVersion.Split('.')[0]) >= 6000) { }
            }
        }

        private static string GetManifestPath() {
            string manifestPath = Path.Combine(Application.dataPath, "Plugins", "Android", "AndroidManifest.xml");

            if (!File.Exists(manifestPath)) {
                string[] manifestFiles = Directory.GetFiles(Application.dataPath, "AndroidManifest.xml", SearchOption.AllDirectories);
                if (manifestFiles.Length > 0) {
                    manifestPath = manifestFiles[0];
                }
            }

            return manifestPath;
        }

        private static void UpgradeManifest(string manifestPath) {
            AndroidManifestTransformer transformer = new(manifestPath);
            transformer.Transform();
        }
    }

    internal class AndroidManifestTransformer {
        private readonly AndroidManifest manifest;
        private readonly XmlElement manifestElement;
        private readonly XmlElement applicationElement;
        private readonly XmlElement activityElement;
        private readonly XmlElement gameActivityElement;

        internal AndroidManifestTransformer(string path) {
            manifest = new AndroidManifest(path);
            manifestElement = manifest.SelectSingleNode("/manifest") as XmlElement;
            applicationElement = manifest.SelectSingleNode("/manifest/application") as XmlElement;
            activityElement = manifest.SelectSingleNode("/manifest/application/activity[contains(@android:name, 'UnityPlayerActivity')]",
                CreateNamespaceManager()) as XmlElement;
            gameActivityElement = manifest.SelectSingleNode(
                "/manifest/application/activity[contains(@android:name, 'UnityPlayerGameActivity')]",
                CreateNamespaceManager()) as XmlElement;
        }

        private XmlNamespaceManager CreateNamespaceManager() {
            XmlNamespaceManager nsManager = new(manifest.NameTable);
            nsManager.AddNamespace("android", manifest.AndroidXmlNamespace);
            nsManager.AddNamespace("tools", manifest.ToolsXmlNamespace);
            return nsManager;
        }

        internal void Transform() {
            UpdateManifestAttributes();

            AddSupportsScreens();
            AddQueries();
            UpdateApplicationAttributes();

            UpdateActivityAttributes();
            ActivityAddAirConsoleIntentFilter();

            if (int.Parse(Application.unityVersion.Split('.')[0]) >= 6000) {
                UpdateGameActivityAttributes();
                GameActivityAddAirConsoleIntentFilter();
            }

            AddApplicationMetaData();
            AddUsesFeatureAndPermissions();

            manifest.Save();
        }

        private void UpdateManifestAttributes() {
            SetAttributeIfMissing(manifest, manifestElement, "android", "package", "com.unity3d.player", manifest.AndroidXmlNamespace);
            SetAttributeIfMissing(manifest, manifestElement, "android", "installLocation", "preferExternal", manifest.AndroidXmlNamespace);
        }

        private void AddSupportsScreens() {
            XmlElement supportsScreens = GetOrCreateElement(manifest, manifestElement, "supports-screens");

            SetAttributeIfMissing(manifest, supportsScreens, "android", "smallScreens", "true", manifest.AndroidXmlNamespace);
            SetAttributeIfMissing(manifest, supportsScreens, "android", "normalScreens", "true", manifest.AndroidXmlNamespace);
            SetAttributeIfMissing(manifest, supportsScreens, "android", "largeScreens", "true", manifest.AndroidXmlNamespace);
            SetAttributeIfMissing(manifest, supportsScreens, "android", "xlargeScreens", "true", manifest.AndroidXmlNamespace);
            SetAttributeIfMissing(manifest, supportsScreens, "android", "anyDensity", "true", manifest.AndroidXmlNamespace);
        }

        private void AddQueries() {
            XmlElement queries = GetOrCreateElement(manifest, manifestElement, "queries");
            XmlElement intent = GetOrCreateElement(manifest, queries, "intent");
            XmlElement action = GetOrCreateElement(manifest, intent, "action");
            SetAttributeIfMissing(manifest, action, "android", "name", "android.intent.action.MAIN", manifest.AndroidXmlNamespace);

            XmlElement data = GetOrCreateElement(manifest, intent, "data");
            SetAttributeIfMissing(manifest, data, "android", "mimeType", "application/airconsole", manifest.AndroidXmlNamespace);
        }

        private void UpdateApplicationAttributes() {
            SetAttributeIfMissing(manifest, applicationElement, "tools", "replace", "android:theme,android:icon",
                manifest.ToolsXmlNamespace);
            SetAttributeIfMissing(manifest, applicationElement, "android", "usesCleartextTraffic", "true", manifest.AndroidXmlNamespace);
            SetAttributeIfMissing(manifest, applicationElement, "android", "icon", "@drawable/app_icon", manifest.AndroidXmlNamespace);
            SetAttributeIfMissing(manifest, applicationElement, "android", "label", "@string/app_name", manifest.AndroidXmlNamespace);
            SetAttributeIfMissing(manifest, applicationElement, "android", "isGame", "true", manifest.AndroidXmlNamespace);
            SetAttributeIfMissing(manifest, applicationElement, "android", "banner", "@drawable/app_banner", manifest.AndroidXmlNamespace);
        }

        private void UpdateActivityAttributes() {
            if (activityElement == null) {
                return;
            }

            SetAttributeIfMissing(manifest, activityElement, "android", "label", "@string/app_name", manifest.AndroidXmlNamespace);
            SetAttributeIfMissing(manifest, activityElement, "android", "screenOrientation", "fullSensor", manifest.AndroidXmlNamespace);
            SetAttributeIfMissing(manifest, activityElement, "android", "launchMode", "singleTask", manifest.AndroidXmlNamespace);
            SetAttributeIfMissing(manifest, activityElement, "android", "configChanges",
                "mcc|mnc|locale|touchscreen|keyboard|keyboardHidden|navigation|orientation|screenLayout|uiMode|screenSize|smallestScreenSize|fontScale|layoutDirection|density",
                manifest.AndroidXmlNamespace);
            SetAttributeIfMissing(manifest, activityElement, "android", "hardwareAccelerated", "true", manifest.AndroidXmlNamespace);
        }

        private void UpdateGameActivityAttributes() {
            if (gameActivityElement == null) {
                return;
            }

            SetAttributeIfMissing(manifest, gameActivityElement, "android", "label", "@string/app_name", manifest.AndroidXmlNamespace);
            SetAttributeIfMissing(manifest, gameActivityElement, "android", "screenOrientation", "fullSensor",
                manifest.AndroidXmlNamespace);
            SetAttributeIfMissing(manifest, gameActivityElement, "android", "launchMode", "singleTask", manifest.AndroidXmlNamespace);
            SetAttributeIfMissing(manifest, gameActivityElement, "android", "configChanges",
                "mcc|mnc|locale|touchscreen|keyboard|keyboardHidden|navigation|orientation|screenLayout|uiMode|screenSize|smallestScreenSize|fontScale|layoutDirection|density",
                manifest.AndroidXmlNamespace);
            SetAttributeIfMissing(manifest, gameActivityElement, "android", "hardwareAccelerated", "true", manifest.AndroidXmlNamespace);
        }

        private void ActivityAddAirConsoleIntentFilter() {
            XmlElement existingIntentFilter = activityElement.SelectSingleNode(
                "intent-filter[action/@android:name='android.intent.action.MAIN']",
                CreateNamespaceManager()) as XmlElement;

            if (existingIntentFilter != null) {
                XmlElement leanbackCategory = GetOrCreateElement(manifest, existingIntentFilter, "category",
                    node => GetAttributeValue(node, "android", "name", manifest.AndroidXmlNamespace)
                            == "android.intent.category.LEANBACK_LAUNCHER");
                SetAttributeIfMissing(manifest, leanbackCategory, "android", "name", "android.intent.category.LEANBACK_LAUNCHER",
                    manifest.AndroidXmlNamespace);
            }

            XmlNamespaceManager nsManager = CreateNamespaceManager();
            XmlElement airConsoleIntentFilter = activityElement.SelectSingleNode(
                "intent-filter[data/@android:mimeType='application/airconsole']", nsManager) as XmlElement;

            if (airConsoleIntentFilter == null) {
                airConsoleIntentFilter = manifest.CreateElement("intent-filter");
                activityElement.AppendChild(airConsoleIntentFilter);

                XmlElement actionElement = manifest.CreateElement("action");
                SetAttributeIfMissing(manifest, actionElement, "android", "name", "android.intent.action.MAIN",
                    manifest.AndroidXmlNamespace);
                airConsoleIntentFilter.AppendChild(actionElement);

                XmlElement dataElement = manifest.CreateElement("data");
                SetAttributeIfMissing(manifest, dataElement, "android", "mimeType", "application/airconsole", manifest.AndroidXmlNamespace);
                airConsoleIntentFilter.AppendChild(dataElement);
            }
        }

        private void GameActivityAddAirConsoleIntentFilter() {
            XmlElement existingIntentFilter = gameActivityElement.SelectSingleNode(
                "intent-filter[action/@android:name='android.intent.action.MAIN']",
                CreateNamespaceManager()) as XmlElement;

            if (existingIntentFilter != null) {
                XmlElement leanbackCategory = GetOrCreateElement(manifest, existingIntentFilter, "category",
                    node => GetAttributeValue(node, "android", "name", manifest.AndroidXmlNamespace)
                            == "android.intent.category.LEANBACK_LAUNCHER");
                SetAttributeIfMissing(manifest, leanbackCategory, "android", "name", "android.intent.category.LEANBACK_LAUNCHER",
                    manifest.AndroidXmlNamespace);
            }

            XmlNamespaceManager nsManager = CreateNamespaceManager();
            XmlElement airConsoleIntentFilter = gameActivityElement.SelectSingleNode(
                "intent-filter[data/@android:mimeType='application/airconsole']", nsManager) as XmlElement;

            if (airConsoleIntentFilter == null) {
                airConsoleIntentFilter = manifest.CreateElement("intent-filter");
                gameActivityElement.AppendChild(airConsoleIntentFilter);

                XmlElement actionElement = manifest.CreateElement("action");
                SetAttributeIfMissing(manifest, actionElement, "android", "name", "android.intent.action.MAIN",
                    manifest.AndroidXmlNamespace);
                airConsoleIntentFilter.AppendChild(actionElement);

                XmlElement dataElement = manifest.CreateElement("data");
                SetAttributeIfMissing(manifest, dataElement, "android", "mimeType", "application/airconsole", manifest.AndroidXmlNamespace);
                airConsoleIntentFilter.AppendChild(dataElement);
            }
        }

        private void AddApplicationMetaData() {
            string buildId = System.Guid.NewGuid().ToString();

            AddMetaData("unity.build-id", buildId);
            AddMetaData("unity.splash-mode", "0");
            AddMetaData("unity.splash-enable", "True");
        }

        private void AddMetaData(string name, string value) {
            XmlElement metaData = GetOrCreateElement(manifest, applicationElement, "meta-data",
                node => GetAttributeValue(node, "android", "name", manifest.AndroidXmlNamespace) == name);

            SetAttributeIfMissing(manifest, metaData, "android", "name", name, manifest.AndroidXmlNamespace);
            SetAttributeIfMissing(manifest, metaData, "android", "value", value, manifest.AndroidXmlNamespace);
        }

        private void AddUsesFeatureAndPermissions() {
            if (int.Parse(Application.unityVersion.Split('.')[0]) < 6000) {
                AddUsesFeature("android.glEsVersion", "0x00020000");
            }

            AddUsesFeature("android.software.leanback", null, "true");
            AddUsesFeature("android.hardware.touchscreen", null, "false");
            AddUsesFeature("android.hardware.touchscreen.multitouch", null, "false");
            AddUsesFeature("android.hardware.touchscreen.multitouch.distinct", null, "false");

            AddUsesPermission("android.permission.INTERNET");
        }

        private void AddUsesFeature(string name, string glEsVersion = null, string required = null) {
            XmlElement usesFeature;

            if (glEsVersion != null) {
                usesFeature = GetOrCreateElement(manifest, manifestElement, "uses-feature",
                    node => GetAttributeValue(node, "android", "glEsVersion", manifest.AndroidXmlNamespace) == glEsVersion);
                SetAttributeIfMissing(manifest, usesFeature, "android", "glEsVersion", glEsVersion, manifest.AndroidXmlNamespace);
            } else {
                usesFeature = GetOrCreateElement(manifest, manifestElement, "uses-feature",
                    node => GetAttributeValue(node, "android", "name", manifest.AndroidXmlNamespace) == name);
                SetAttributeIfMissing(manifest, usesFeature, "android", "name", name, manifest.AndroidXmlNamespace);

                if (required != null) {
                    SetAttributeIfMissing(manifest, usesFeature, "android", "required", required, manifest.AndroidXmlNamespace);
                }
            }
        }

        private void AddUsesPermission(string name) {
            XmlElement usesPermission = GetOrCreateElement(manifest, manifestElement, "uses-permission",
                node => GetAttributeValue(node, "android", "name", manifest.AndroidXmlNamespace) == name);

            SetAttributeIfMissing(manifest, usesPermission, "android", "name", name, manifest.AndroidXmlNamespace);
        }

        #region Helper Methods

        private static XmlElement GetOrCreateElement(AndroidManifest manifest, XmlElement parent, string elementName,
            System.Predicate<XmlElement> predicate = null) {
            XmlNodeList existingNodes = parent.SelectNodes(elementName);

            if (existingNodes is { Count: > 0 }) {
                if (predicate == null) {
                    return existingNodes[0] as XmlElement;
                }

                foreach (XmlElement element in existingNodes) {
                    if (predicate(element)) {
                        return element;
                    }
                }
            }

            XmlElement newElement = manifest.CreateElement(elementName);
            parent.AppendChild(newElement);
            return newElement;
        }

        private static void SetAttributeIfMissing(AndroidManifest manifest, XmlElement element, string prefix, string name, string value,
            string xmlNamespace) {
            if (element.GetAttribute(name, xmlNamespace) == string.Empty) {
                XmlAttribute attr = manifest.GenerateAttribute(prefix, name, value, xmlNamespace);
                element.SetAttributeNode(attr);
            }
        }

        private static string GetAttributeValue(XmlElement element, string prefix, string name, string xmlNamespace) {
            return element.GetAttribute(name, xmlNamespace);
        }

        #endregion
    }
}
#endif