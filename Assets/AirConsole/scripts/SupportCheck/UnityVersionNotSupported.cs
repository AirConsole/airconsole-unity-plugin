using System.IO;
#if !DISABLE_AIRCONSOLE
namespace NDream.AirConsole.Support {
    using UnityEditor;
    using UnityEngine;

    [InitializeOnLoad]
    internal class UnityVersionNotSupported {
        static UnityVersionNotSupported() {
#if !UNITY_2022_3_OR_NEWER
            string message = $"AirConsole requires Unity 2022.3 or newer. Deleting AirConsole directory again.";
            EditorUtility.DisplayDialog("Not supported", message, "Ok");
            Debug.LogError(message);
            
            
            EditorApplication.LockReloadAssemblies();
            AssetDatabase.DeleteAsset("Assets/AirConsole");
            Directory.Delete(Path.Combine(Application.dataPath, "AirConsole"));
            EditorApplication.UnlockReloadAssemblies();
            AssetDatabase.Refresh();
#endif
        }
    }
}
#endif