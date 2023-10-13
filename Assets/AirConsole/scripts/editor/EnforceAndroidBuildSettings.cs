#if !DISABLE_AIRCONSOLE

#region
using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#endregion

namespace NDream.AirConsole.Editor
{
    internal class EnforceAndroidBuildSettings : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        private static bool ShallExecute()
        {
            if(EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android) return false;
            if(PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android).Contains("DISABLE_AIRCONSOLE")) return false;

            return true;
        }

        [InitializeOnLoadMethod]
        private static void Init()
        {
            if(!EnforceAndroidBuildSettings.ShallExecute()) return;

            SettingWindow.ApplyAndroidRequiredSettings();
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            if(!EnforceAndroidBuildSettings.ShallExecute()) return;

            if(SettingWindow.AndroidBuildNotAllowed) throw new Exception("Android build is not allowed due to disallowed packages");

            SettingWindow.CheckUnityVersionForBuildSupport();
            SettingWindow.ApplyAndroidRequiredSettings();
            SettingWindow.QueryAndApplyRecommendedAndroidSettings();
        }
    }
}
#endif