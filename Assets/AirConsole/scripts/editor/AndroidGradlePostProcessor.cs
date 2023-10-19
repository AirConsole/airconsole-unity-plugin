#if !DISABLE_AIRCONSOLE
using System.IO;
using System.Text;
using System.Xml;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

public class AndroidGradlePostProcessor : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 1;


    // Approach based on https://stackoverflow.com/questions/56886994/c-sharp-adding-attribute-to-xml-element-appends-the-namespace-to-the-end-of-the
    public void OnPostGenerateGradleAndroidProject(string path)
    {
        string xmlPath = GetManifestPath(path);
        Debug.Log("adjusted AndroidManifest.xml with application/airconsole Query");
        AndroidManifest androidManifest = new AndroidManifest(xmlPath);

        XmlNode manifestNode = androidManifest.SelectSingleNode("//manifest");
        if(manifestNode == null)
        {
            throw new
                BuildFailedException("Invalid AndroidManifest found. Please use the one provided by the AirConsole Unity Plugin and configure it as Custom AndroidManifest");
        }
        AndroidGradlePostProcessor.AddAirConsoleQueryIntent(androidManifest, manifestNode);
        AndroidGradlePostProcessor.AddAirConsoleIntentFilter(androidManifest);
        AndroidGradlePostProcessor.ConfigureUsedFeatures(androidManifest, manifestNode);
        
        Debug.Log($"Manifest {androidManifest.Save()}");
    }

    private static void ConfigureUsedFeatures(AndroidManifest androidManifest, XmlNode manifestRoot)
    {
        var usesFeatureList = androidManifest.SelectNodes("//uses-feature");
        bool foundLeanback = false;
        bool foundTouchscreen = false;
        bool foundMultitouch = false;
        bool foundMultitouchDistinct = false;
        bool foundES2 = false;
        if(usesFeatureList != null)
        {
            foreach (XmlNode usedFeature in usesFeatureList)
            {
                XmlNode name = usedFeature.Attributes.GetNamedItem("android:name");
                XmlNode esVersion = usedFeature.Attributes.GetNamedItem("android:glEsVersion");
                XmlNode required = usedFeature.Attributes.GetNamedItem("android:required");

                if(name != null)
                {
                    switch (name.Value)
                    {
                        case "android.software.leanback":
                        {
                            AndroidGradlePostProcessor.SetRequiredAttribute(androidManifest, required, usedFeature, true);
                            foundLeanback = true;
                            break;
                        }
                        case "android.hardware.touchscreen":
                        {
                            AndroidGradlePostProcessor.SetRequiredAttribute(androidManifest, required, usedFeature, false);
                            foundTouchscreen = true;
                            break;
                        }
                        case "android.hardware.touchscreen.multitouch":
                        {
                            AndroidGradlePostProcessor.SetRequiredAttribute(androidManifest, required, usedFeature, false);
                            foundMultitouch = true;
                            break;
                        }
                        case "android.hardware.touchscreen.multitouch.distinct":
                        {
                            AndroidGradlePostProcessor.SetRequiredAttribute(androidManifest, required, usedFeature, false);
                            foundMultitouchDistinct = true;
                            break;
                        }
                    }
                }

                if(esVersion != null)
                {
                    esVersion.Value = "0x00020000";
                    foundES2 = true;
                }
                
            }
            if(!foundLeanback)
            {
                manifestRoot.AppendChild(AndroidGradlePostProcessor.CreateUsedFeature(androidManifest, "android.software.leanback", true));
            }
            if(!foundTouchscreen)
            {
                manifestRoot.AppendChild(AndroidGradlePostProcessor.CreateUsedFeature(androidManifest, "android.hardware.touchscreen", false));
            }
            if(!foundMultitouch)
            {
                manifestRoot.AppendChild(AndroidGradlePostProcessor.CreateUsedFeature(androidManifest, "android.hardware.touchscreen.multitouch", false));
            }
            if(!foundMultitouchDistinct)
            {
                manifestRoot.AppendChild(AndroidGradlePostProcessor.CreateUsedFeature(androidManifest, "android.hardware.touchscreen.multitouch.distinct", false));
            }
            if(!foundES2)
            {
                XmlElement usedFeature = androidManifest.CreateElement("uses-feature");
                XmlAttribute glAttribute =
                    androidManifest.GenerateAttribute("android", "glEsVersion", "0x00020000", androidManifest.AndroidXmlNamespace);
                usedFeature.Attributes.Append(glAttribute);
                manifestRoot.AppendChild(usedFeature);
            }
        }
    }

    private static string getManifestBoolean(bool value) => value ? "true" : "false";
    private static XmlElement CreateUsedFeature(AndroidManifest androidManifest, string featureName, bool featureRequired)
    {
        XmlElement usedFeature = androidManifest.CreateElement("uses-feature");
        XmlAttribute name =
            androidManifest.GenerateAttribute("android", "name", featureName, androidManifest.AndroidXmlNamespace);
        XmlAttribute required =
            androidManifest.GenerateAttribute("android", "required", AndroidGradlePostProcessor.getManifestBoolean(featureRequired), androidManifest.AndroidXmlNamespace);
        usedFeature.Attributes.Append(name);
        usedFeature.Attributes.Append(required);
        return usedFeature;
    }

    private static void SetRequiredAttribute(AndroidManifest androidManifest, XmlNode required, XmlNode usedFeature, bool requiredFlag)
    {
        if(required != null)
        {
            required.Value = AndroidGradlePostProcessor.getManifestBoolean(requiredFlag);
        }
        else
        {
            XmlAttribute reqAttrib = androidManifest.GenerateAttribute("android", "required", AndroidGradlePostProcessor.getManifestBoolean(requiredFlag), androidManifest.AndroidXmlNamespace);
            usedFeature.Attributes?.Append(reqAttrib);
        }
    }

    private static void AddAirConsoleQueryIntent(AndroidManifest androidManifest, XmlNode manifestNode)
    {
        XmlNode queriesNode = androidManifest.SelectSingleNode("//manifest//queries");
        if(queriesNode == null)
        {
            queriesNode = androidManifest.CreateNode("element", "queries", "");
            manifestNode.AppendChild(queriesNode);
        }
        XmlNode intentNode = queriesNode.SelectSingleNode("//intent");
        if(intentNode == null || !queriesNode.InnerXml.Contains("<data android:mimeType=\"application/airconsole\""))
        {
            queriesNode.AppendChild(AndroidGradlePostProcessor.GenerateIntentXmlTree(androidManifest, "intent"));
        }
    }

    private static void AddAirConsoleIntentFilter(AndroidManifest androidManifest)
    {
        XmlNode activityNode = androidManifest.SelectSingleNode("//manifest//application//activity");
        if(activityNode == null)
        {
            throw new
                BuildFailedException("Invalid AndroidManifest found. Please use the one provided by the AirConsole Unity Plugin and configure it as Custom AndroidManifest");
        }
        XmlNodeList intentFilters = androidManifest.SelectNodes("//manifest//application//activity//intent-filter");
        bool foundIntentFilter = false;
        if(intentFilters != null) // && !activityNode.InnerXml.Contains("<data android:mimeType=\"application/airconsole\""))
        {
            foreach (XmlNode intentFilter in intentFilters)
            {
                if(!string.IsNullOrEmpty(intentFilter.InnerXml) &&
                   intentFilter.InnerXml.Contains("<data android:mimeType=\"application/airconsole\""))
                    foundIntentFilter = true;
            }
        }
        if(!foundIntentFilter)
        {
            activityNode.AppendChild(AndroidGradlePostProcessor.GenerateIntentXmlTree(androidManifest, "intent-filter"));
        }
    }

    private static XmlNode GenerateIntentXmlTree(AndroidManifest androidManifest, string intentNodeName)
    {
        XmlNode intentFilter = androidManifest.CreateNode("element", intentNodeName, "");
        XmlAttribute actionAttribute =
            androidManifest.GenerateAttribute("android", "name", "android.intent.action.MAIN", androidManifest.AndroidXmlNamespace);
        XmlElement action = androidManifest.CreateElement("element", "action", "");
        action.SetAttributeNode(actionAttribute);

        XmlAttribute dataAttribute =
            androidManifest.GenerateAttribute("android", "mimeType", "application/airconsole", androidManifest.AndroidXmlNamespace);
        XmlElement data = androidManifest.CreateElement("element", "data", "");
        data.SetAttributeNode(dataAttribute);

        intentFilter.AppendChild(action);
        intentFilter.AppendChild(data);

        return intentFilter;
    }

    //<manifest xmlns:android="http://schemas.android.com/apk/res/android" package="com.unity3d.player" xmlns:tools="http://schemas.android.com/tools" android:installLocation="preferExternal">
    // <supports-screens android:smallScreens="true" android:normalScreens="true" android:largeScreens="true" android:xlargeScreens="true" android:anyDensity="true" />
    // <application android:theme="@style/UnityThemeSelector" android:icon="@drawable/app_icon" android:label="@string/app_name" android:isGame="true" android:banner="@drawable/app_banner">
    // <activity android:name="com.unity3d.player.UnityPlayerActivity" android:label="@string/app_name" android:screenOrientation="fullSensor" android:launchMode="singleTask" android:configChanges="mcc|mnc|locale|touchscreen|keyboard|keyboardHidden|navigation|orientation|screenLayout|uiMode|screenSize|smallestScreenSize|fontScale|layoutDirection|density" android:hardwareAccelerated="true">
    // <intent-filter>
    // <action android:name="android.intent.action.MAIN" />
    // <category android:name="android.intent.category.LAUNCHER" />
    // <category android:name="android.intent.category.LEANBACK_LAUNCHER" />
    // </intent-filter>
    // <intent-filter>
    // <action android:name="android.intent.action.MAIN" />
    // <data android:mimeType="application/airconsole"/>
    // </intent-filter>
    
    // <queries>
    // <intent>
    // <action android:name="android.intent.action.MAIN" />
    // <data android:mimeType="application/airconsole" />
    // </intent>
    // </queries>
    
    private string GetManifestPath(string basePath) {
        var pathBuilder = new StringBuilder(basePath);
        pathBuilder.Append(Path.DirectorySeparatorChar).Append("src");
        pathBuilder.Append(Path.DirectorySeparatorChar).Append("main");
        pathBuilder.Append(Path.DirectorySeparatorChar).Append("AndroidManifest.xml");
        return pathBuilder.ToString();
    }
}
#endif