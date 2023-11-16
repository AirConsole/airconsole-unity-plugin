mergeInto(LibraryManager.library, {
    /**
     * Enables the PlayerSilencing feature.
     * Do not rename this. This is native code bound to AirConsole [DLLImport] private static extern void EnablePlayerSilencing();
     */
    EnablePlayerSilencing: function () {
        window.app.enablePlayerSilencing();
    },
});