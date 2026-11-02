#if !DISABLE_AIRCONSOLE
namespace NDream.AirConsole.Support {
    using UnityEditor;
    using UnityEngine;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;

#if !UNITY_2022_3_OR_NEWER
    internal class UnityVersionNotSupported : MonoBehaviour, IPreprocessBuildWithReport {
        private static string Message =>
            $"AirConsole requires Unity 2022.3 or newer. This project uses Unity {Application.unityVersion}.\n"
            + "Upgrade the project to Unity 2022.3 or newer, or add the scripting define symbol 'DISABLE_AIRCONSOLE' "
            + "in the Player Settings to turn off this check.";

        [InitializeOnLoadMethod]
        private static void TriggerNotSupportedVersion() {
            EditorUtility.DisplayDialog("Unity version not supported", Message, "Ok");
            Debug.LogError(Message);
        }

        private void Awake() {
            TriggerNotSupportedVersion();
        }
        private void Start() {
            EditorApplication.isPlaying = false;
        }

        public int callbackOrder {
            get => 1;
        }

        public void OnPreprocessBuild(BuildReport report) {
            throw new BuildFailedException(Message);
        }
    }
#endif
}
#endif
