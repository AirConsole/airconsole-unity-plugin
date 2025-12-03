#if !DISABLE_AIRCONSOLE
namespace NDream.AirConsole.Editor {
    using UnityEditor.Android;

    public class AndroidProjectProcessor : IPostGenerateGradleAndroidProject {
        public int callbackOrder {
            get => 999;
        }

        public void OnPostGenerateGradleAndroidProject(string projectPath) {
            AndroidGradleProcessor.Execute(projectPath);
            AndroidManifestProcessor.Execute(projectPath);
        }
    }
}
#endif
