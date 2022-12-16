#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class PreBuildProcessing : IPreprocessBuildWithReport {
    public int callbackOrder => 1;
    public void OnPreprocessBuild(BuildReport report) {
        Debug.Log("Used Python path: " + System.Environment.GetEnvironmentVariable("EMSDK_PYTHON"));

        // In case you get a Build exception from Unity such as:
        //   System.ComponentModel.Win32Exception (2): No such file or directory)
        // Make sure that the correct Python version is installed and can be found by Unity during
        // the Build process.

        // If you need to set the Python path manually you can use the code below, uncomment it and
        // set "EMSDK_PYTHON" to the the Python 3 (Or Python 2 for old Unity versions) path:
        // System.Environment.SetEnvironmentVariable("EMSDK_PYTHON", "<python-path-here>");
    }
}
#endif
