using UnityEngine;
// ReSharper disable UnusedType.Global

namespace NDream.Unity
{
    public static class Builder
    {
        public static void BuildAndroid()
        {
          Debug.Log("Building Android");
        }

        public static void BuildWebGL(){
          Debug.Log("Building WebGL");
        }
    }
}