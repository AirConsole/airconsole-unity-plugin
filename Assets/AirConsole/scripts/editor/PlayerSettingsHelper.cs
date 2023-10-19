#if !DISABLE_AIRCONSOLE
using System;
using System.Reflection;
using UnityEditor;

namespace NDream.AirConsole.Editor
{
    // The implementation is based on the UnityEditor.PlayerSettingsEditor and its internal only aspects
    internal static class PlayerSettingsHelper
    {
        internal static void SetLowLightmapEncodingQualityForPlatformGroup(BuildTargetGroup platformGroup)
        {
            MethodInfo method = typeof(PlayerSettings).GetMethod("SetLightmapEncodingQualityForPlatformGroup", BindingFlags.Static | BindingFlags.NonPublic);
            Type enumType = typeof(PlayerSettings).Assembly.GetType("UnityEditor.LightmapEncodingQuality");
            int qualityLevelValue = (int)enumType.GetField("Low").GetValue(null);
            method.Invoke(null, new object[] { platformGroup, qualityLevelValue });
        }
		
        internal static bool GetIsLowLightmapEncodingQualityForPlatformGroup(BuildTargetGroup platformGroup)
        {
            MethodInfo method = typeof(PlayerSettings).GetMethod("GetLightmapEncodingQualityForPlatformGroup", BindingFlags.Static | BindingFlags.NonPublic);
            object qualityLevel = method.Invoke(null, new object[] { platformGroup });
            return qualityLevel.ToString().ToLower() == "low";
        }
		
        internal static AndroidGamepadSupportLevel GetAndroidGamepadSupportLevel()
        {
            PropertyInfo property = typeof(PlayerSettings.Android).GetProperty("androidGamepadSupportLevel", BindingFlags.Static | BindingFlags.NonPublic);
            int gamepadSupportLevel = (int)property.GetValue(null);
            return (AndroidGamepadSupportLevel)gamepadSupportLevel;
        }
		
        internal static void SetAndroidGamepadSupportLevel(AndroidGamepadSupportLevel gamepadSupportLevel)
        {
            PropertyInfo property = typeof(PlayerSettings.Android).GetProperty("androidGamepadSupportLevel", BindingFlags.Static | BindingFlags.NonPublic);
            property.SetValue(null, (int)gamepadSupportLevel);
        }
    }
}
#endif