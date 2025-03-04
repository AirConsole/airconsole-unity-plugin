#if !DISABLE_AIRCONSOLE
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEngine;
using UnityEditor.Build;

namespace NDream.AirConsole.Editor {
    public class AndroidManifestProcessor : IPreprocessBuildWithReport {
        public int callbackOrder => 999;

        [MenuItem("Tools/AirConsole/Development/Update Android Manifest")]
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
                if (Settings.IsUnity6OrHigher()) { }
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
        private static readonly string AirConsoleAndroidMimeType = "application/airconsole";
        private readonly AndroidManifest manifest;
        private readonly XmlElement applicationElement;
        private readonly XmlElement manifestElement;
        private readonly XmlElement activityElement;
        private readonly XmlElement gameActivityElement;

        private const string ANDROID_ACTIVITY_THEME = "@style/UnityThemeSelector";
        private const string ANDROID_GAMEACTIVITY_THEME = "@style/BaseUnityGameActivityTheme";

        internal AndroidManifestTransformer(string path) {
            manifest = new AndroidManifest(path);
            manifestElement = manifest.SelectSingleNode("/manifest") as XmlElement;
            applicationElement = manifest.SelectSingleNode("/manifest/application") as XmlElement;
            activityElement = manifest.SelectSingleNode("/manifest/application/activity[contains(@android:name, 'UnityPlayerActivity')]",
                CreateNamespaceManager(manifest)) as XmlElement;
            gameActivityElement = manifest.SelectSingleNode(
                "/manifest/application/activity[contains(@android:name, 'UnityPlayerGameActivity')]",
                CreateNamespaceManager(manifest)) as XmlElement;
        }

        private static XmlNamespaceManager CreateNamespaceManager(AndroidManifest manifest) {
            XmlNamespaceManager nsManager = new(manifest.NameTable);
            nsManager.AddNamespace("android", manifest.AndroidXmlNamespace);
            nsManager.AddNamespace("tools", manifest.ToolsXmlNamespace);
            return nsManager;
        }

        internal void Transform() {
            UpdateManifestAttributes(manifest, manifestElement);

            AddSupportsScreens(manifest, manifestElement);
            AddQueries(manifest, manifestElement);
            UpdateApplicationAttributes(applicationElement);

            UpdateActivityAttributes(manifest, activityElement, ANDROID_ACTIVITY_THEME);
            ActivityAddAirConsoleIntentFilter(manifest, activityElement);

            if (Settings.IsUnity6OrHigher()) {
                UpdateActivityAttributes(manifest, gameActivityElement, ANDROID_GAMEACTIVITY_THEME);
                ActivityAddAirConsoleIntentFilter(manifest, gameActivityElement);
            }

            AddUsesFeatureAndPermissions(manifest, manifestElement);

            manifest.Save();
        }

        private static void UpdateManifestAttributes(AndroidManifest manifest, XmlElement manifestElement) {
            SetAttributeIfMissing(manifest, manifestElement, "android", "installLocation", "auto", manifest.AndroidXmlNamespace);
            RemoveAttributeIfPresent(manifestElement, "android", "package");
        }

        private static void AddSupportsScreens(AndroidManifest manifest, XmlElement manifestElement) {
            XmlElement supportsScreens = GetOrCreateElement(manifest, manifestElement, "supports-screens");

            SetAttributeIfMissing(manifest, supportsScreens, "android", "smallScreens", "true", manifest.AndroidXmlNamespace);
            SetAttributeIfMissing(manifest, supportsScreens, "android", "normalScreens", "true", manifest.AndroidXmlNamespace);
            SetAttributeIfMissing(manifest, supportsScreens, "android", "largeScreens", "true", manifest.AndroidXmlNamespace);
            SetAttributeIfMissing(manifest, supportsScreens, "android", "xlargeScreens", "true", manifest.AndroidXmlNamespace);
            SetAttributeIfMissing(manifest, supportsScreens, "android", "anyDensity", "true", manifest.AndroidXmlNamespace);
        }

        private static void AddQueries(AndroidManifest manifest, XmlElement manifestElement) {
            XmlElement queries = GetOrCreateElement(manifest, manifestElement, "queries");
            XmlElement intent = GetOrCreateElement(manifest, queries, "intent");
            XmlElement action = GetOrCreateElement(manifest, intent, "action");
            SetAttributeIfMissing(manifest, action, "android", "name", "android.intent.action.MAIN", manifest.AndroidXmlNamespace);

            XmlElement data = GetOrCreateElement(manifest, intent, "data");
            SetAttributeIfMissing(manifest, data, "android", "mimeType", AirConsoleAndroidMimeType, manifest.AndroidXmlNamespace);
        }

        private static void UpdateApplicationAttributes(XmlElement applicationElement) {
            RemoveAttributeIfPresent(applicationElement, "tools", "replace");
            RemoveAttributeIfPresent(applicationElement, "android", "usesCleartextTraffic");
            RemoveAttributeIfPresent(applicationElement, "android", "icon");
            RemoveAttributeIfPresent(applicationElement, "android", "label");
            RemoveAttributeIfPresent(applicationElement, "android", "isGame");
            RemoveAttributeIfPresent(applicationElement, "android", "banner");
            RemoveAttributeIfPresent(applicationElement, "xmlns", "tools");
        }

        private static void UpdateActivityAttributes(AndroidManifest manifest, XmlElement activityElement, string themeAttribute) {
            if (activityElement == null) {
                return;
            }

            SetAttributeIfMissing(manifest, activityElement, "android", "screenOrientation", "landscape", manifest.AndroidXmlNamespace);
            SetAttributeIfMissing(manifest, activityElement, "android", "launchMode", "singleTask", manifest.AndroidXmlNamespace);
            SetAttributeIfMissing(manifest, activityElement, "android", "configChanges",
                "mcc|mnc|locale|touchscreen|keyboard|keyboardHidden|navigation|orientation|screenLayout|uiMode|screenSize|smallestScreenSize|fontScale|layoutDirection|density",
                manifest.AndroidXmlNamespace);
            SetAttributeIfMissing(manifest, activityElement, "android", "hardwareAccelerated", "true", manifest.AndroidXmlNamespace);
            SetAttributeIfMissing(manifest, activityElement, "android", "theme", themeAttribute, manifest.AndroidXmlNamespace);
        }

        private static void ActivityAddAirConsoleIntentFilter(AndroidManifest manifest, XmlElement activityElement) {
            XmlNamespaceManager nsManager = CreateNamespaceManager(manifest);
            XmlElement existingIntentFilter = activityElement.SelectSingleNode(
                "intent-filter[action/@android:name='android.intent.action.MAIN']",
                nsManager) as XmlElement;

            if (existingIntentFilter != null) {
                XmlElement leanbackCategory = GetOrCreateElement(manifest, existingIntentFilter, "category",
                    node => GetAttributeValue(node, "android", "name", manifest.AndroidXmlNamespace)
                            == "android.intent.category.LEANBACK_LAUNCHER");
                SetAttributeIfMissing(manifest, leanbackCategory, "android", "name", "android.intent.category.LEANBACK_LAUNCHER",
                    manifest.AndroidXmlNamespace);
            }

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
                SetAttributeIfMissing(manifest, dataElement, "android", "mimeType", AirConsoleAndroidMimeType,
                    manifest.AndroidXmlNamespace);
                airConsoleIntentFilter.AppendChild(dataElement);
            }
        }

        private static void AddUsesFeatureAndPermissions(AndroidManifest manifest, XmlElement manifestElement) {
            if (!Settings.IsUnity6OrHigher()) {
                AddUsesFeature(manifest, manifestElement, "android.glEsVersion", "0x00020000");
            } else {
                RemoveUsesFeature(manifestElement, "android.glEsVersion");
            }

            AddUsesFeature(manifest, manifestElement, "android.software.leanback", null, "true");
            AddUsesFeature(manifest, manifestElement, "android.hardware.touchscreen", null, "false");
            AddUsesFeature(manifest, manifestElement, "android.hardware.touchscreen.multitouch", null, "false");
            AddUsesFeature(manifest, manifestElement, "android.hardware.touchscreen.multitouch.distinct", null, "false");

            AddUsesPermission(manifest, manifestElement, "android.permission.INTERNET");
        }

        private static void AddUsesFeature(AndroidManifest manifest, XmlElement manifestElement, string name, string glEsVersion = null,
            string required = null) {
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

        private static void RemoveUsesFeature(XmlElement manifestElement, string name) {
            manifestElement.RemoveAttribute(name);
        }

        private static void AddUsesPermission(AndroidManifest manifest, XmlElement manifestElement, string name) {
            XmlElement usesPermission = GetOrCreateElement(manifest, manifestElement, "uses-permission",
                node => GetAttributeValue(node, "android", "name", manifest.AndroidXmlNamespace) == name);

            SetAttributeIfMissing(manifest, usesPermission, "android", "name", name, manifest.AndroidXmlNamespace);
        }

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

        private static void RemoveAttributeIfPresent(XmlElement element, string prefix, string name) {
            string elementName = $"{prefix}:{name}";
            if (element.GetAttribute(elementName) != string.Empty) {
                element.RemoveAttribute(elementName);
            }
        }

        private static string GetAttributeValue(XmlElement element, string prefix, string name, string xmlNamespace) {
            return element.GetAttribute(name, xmlNamespace);
        }
    }
}
#endif