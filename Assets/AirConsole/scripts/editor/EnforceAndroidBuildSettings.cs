#if !DISABLE_AIRCONSOLE
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace NDream.AirConsole.Editor
{
    internal class EnforceAndroidBuildSettings : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        
        [InitializeOnLoadMethod]
        private static void Init()
        {
            SettingWindow.ApplyAndroidRequiredSettings();
        }
        
        public void OnPreprocessBuild(BuildReport report)
        {
            if(SettingWindow.AndroidBuildNotAllowed) throw new System.Exception("Android build is not allowed due to disallowed packages");
            SettingWindow.ApplyAndroidRequiredSettings();
            SettingWindow.QueryAndApplyRecommendedAndroidSettings();
        }
    }
}
#endif