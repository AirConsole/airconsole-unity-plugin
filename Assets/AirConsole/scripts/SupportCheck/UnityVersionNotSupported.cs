#if !DISABLE_AIRCONSOLE
namespace NDream.AirConsole.Support {
    using UnityEditor;
    using UnityEngine;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;

#if !UNITY_2022_3_OR_NEWER
    internal class UnityVersionNotSupported : MonoBehaviour, IPreprocessBuildWithReport {
        [InitializeOnLoadMethod]
        private static void TriggerNotSupportedVersion() {
            string message = $"AirConsole requires Unity 2022.3 or newer. Please upgrade the project.";
            EditorUtility.DisplayDialog("Not supported", message, "Ok");
            Debug.LogError(message);
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
            throw new BuildFailedException($"AirConsole requires Unity 2022.3 or newer but found {Application.unityVersion}.");
        }
    }
#endif
}
#endif
