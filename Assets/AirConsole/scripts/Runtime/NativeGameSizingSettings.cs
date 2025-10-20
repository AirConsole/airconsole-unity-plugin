using UnityEngine;

namespace NDream.AirConsole {
    /// <summary>
    /// Provides runtime access to the native game sizing setting that is configured in the editor.
    /// </summary>
    public sealed class NativeGameSizingSettings : ScriptableObject {
        public const string ResourceName = "NativeGameSizingSettings";

        [SerializeField]
        private bool nativeGameSizingSupported = true;

        public bool NativeGameSizingSupported => nativeGameSizingSupported;

#if UNITY_EDITOR
        public void SetNativeGameSizingSupported(bool value) {
            nativeGameSizingSupported = value;
        }
#endif
    }
}
