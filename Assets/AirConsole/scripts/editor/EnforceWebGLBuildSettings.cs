#if !DISABLE_AIRCONSOLE
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace NDream.AirConsole.Editor
{
    internal class EnforceWebGLBuildSettings : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        private static bool ShallExecute()
        {
            if(EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL) return false;

            return true;
        }

        [InitializeOnLoadMethod]
        private static void Init()
        {
            if(!EnforceWebGLBuildSettings.ShallExecute()) return;

            // SettingWindow.ApplyWebGLRequiredSettings();
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            if(!EnforceWebGLBuildSettings.ShallExecute()) return;

            // if(SettingWindow.AndroidBuildNotAllowed) throw new Exception("Android build is not allowed due to disallowed packages");

            // TODO (Marc): Act based on identified need for Query and Intent in AndroidManifest.xml
            // SettingWindow.CheckUnityVersionForBuildSupport();
            // SettingWindow.ApplyAndroidRequiredSettings();
            SettingWindow.QueryAndApplyRecommendedWebGLSettings();
        }
    }
}
#endif