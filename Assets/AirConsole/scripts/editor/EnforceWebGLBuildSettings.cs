#if !DISABLE_AIRCONSOLE
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace NDream.AirConsole.Editor {
	internal class EnforceWebGLBuildSettings : IPreprocessBuildWithReport {
		public int callbackOrder => 1;

		private static bool ShallExecute() {
			if(EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL) return false;

			return true;
		}

		[InitializeOnLoadMethod]
		private static void Init() {
			if(!ShallExecute()) return;

			// SettingWindow.ApplyWebGLRequiredSettings();
		}

		public void OnPreprocessBuild(BuildReport report) {
			if(!ShallExecute()) return;

			SettingWindow.QueryAndApplyRecommendedWebGLSettings();
		}
	}
}
#endif