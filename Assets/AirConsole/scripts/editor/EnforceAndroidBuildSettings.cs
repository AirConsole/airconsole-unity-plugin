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

            if(SettingWindow.AndroidBuildNotAllowed)
            {
                SettingWindow.LogFoundDisallowedPackages();
                throw new BuildFailedException("Android build is not allowed due to disallowed packages");
            }

            // TODO (Marc): Act based on identified need for Query and Intent in AndroidManifest.xml
            // SettingWindow.CheckUnityVersionForBuildSupport();
            SettingWindow.ApplyAndroidRequiredSettings();
            SettingWindow.QueryAndApplyRecommendedAndroidSettings();
        }
    }
}
#endif