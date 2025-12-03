#if !DISABLE_AIRCONSOLE
namespace NDream.AirConsole {
    public interface IRuntimeConfigurator {
        /// <summary>
        /// Refreshes the runtime configuration to match the expected runtime configuration.
        /// </summary>
        void RefreshConfiguration();
    }
}
#endif
