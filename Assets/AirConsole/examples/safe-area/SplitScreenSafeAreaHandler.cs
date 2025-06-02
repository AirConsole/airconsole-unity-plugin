namespace NDream.AirConsole.Examples {
    using UnityEngine;
    using NDream.AirConsole;

    /// <summary>
    /// Example script for handling SafeArea changes with split screen camera setup.
    /// Supports multiple screen configurations including:
    /// - 2 players (horizontal or vertical)
    /// - 3 players (3 cameras + optional UI/birds-eye view)
    /// - 4 players (2x2 grid)
    /// </summary>
    public class SplitScreenSafeAreaHandler : MonoBehaviour {
        private enum SplitMode {
            TwoPlayersHorizontal,
            TwoPlayersVertical,
            ThreePlayers,
            FourPlayers
        }

        [Tooltip("The current split screen configuration")]
        [SerializeField]
        private SplitMode splitMode = SplitMode.TwoPlayersHorizontal;

        [Tooltip("Array of player cameras to be arranged in split-screen")]
        [SerializeField]
        private Camera[] playerCameras;

        [Tooltip("Optional border between split screens (in pixels)")]
        [SerializeField]
        private float borderWidth = 2f;

#if !DISABLE_AIRCONSOLE
        private void Awake() {
            HandleSafeAreaChanged(AirConsole.instance.SafeArea);
            AirConsole.instance.onReady += Setup;
        }

        private void Setup(string code) {
            if (AirConsole.instance && AirConsole.instance.IsAirConsoleUnityPluginReady()) {
                if (AirConsole.instance.SafeArea.width > 0) {
                    HandleSafeAreaChanged(AirConsole.instance.SafeArea);
                }

                AirConsole.instance.OnSafeAreaChanged += HandleSafeAreaChanged;

                Debug.Log("SplitScreenSafeAreaHandler: Subscribed to OnSafeAreaChanged events");
            } else {
                Debug.LogWarning("AirConsole is not ready. Safe area handling won't work correctly.");
            }
        }

        /// <summary>
        /// Handles changes to the safe area by adjusting all cameras according to the split mode
        /// </summary>
        /// <param name="newSafeArea">The new safe area rectangle in pixel coordinates</param>
        private void HandleSafeAreaChanged(Rect newSafeArea) {
            switch (splitMode) {
                case SplitMode.TwoPlayersHorizontal:
                    SetupTwoPlayersHorizontal(newSafeArea);
                    break;
                case SplitMode.TwoPlayersVertical:
                    SetupTwoPlayersVertical(newSafeArea);
                    break;
                case SplitMode.ThreePlayers:
                    SetupThreePlayers(newSafeArea);
                    break;
                case SplitMode.FourPlayers:
                    SetupFourPlayers(newSafeArea);
                    break;
            }

            if (Settings.debug.info) {
                Debug.Log($"Split screen cameras adjusted to safe area: {newSafeArea}");
            }
        }

        #region Split Screen Configuration Methods

        /// <summary>
        /// Sets up two player cameras in a horizontal split (side by side)
        /// </summary>
        private void SetupTwoPlayersHorizontal(Rect safeArea) {
            if (playerCameras.Length < 2) {
                Debug.LogError("Not enough cameras for two player mode. Need at least 2 cameras.");
                return;
            }

            Debug.Log("Init 2 player horizontal splitscreen");

            float halfWidth = (safeArea.width - borderWidth) / 2;

            Rect leftRect = new(
                safeArea.x,
                safeArea.y,
                halfWidth,
                safeArea.height
            );
            playerCameras[0].pixelRect = leftRect;

            Rect rightRect = new(
                safeArea.x + halfWidth + borderWidth,
                safeArea.y,
                halfWidth,
                safeArea.height
            );
            playerCameras[1].pixelRect = rightRect;
        }

        /// <summary>
        /// Sets up two player cameras in a vertical split (top and bottom)
        /// </summary>
        private void SetupTwoPlayersVertical(Rect safeArea) {
            if (playerCameras.Length < 2) {
                Debug.LogError("Not enough cameras for two player mode. Need at least 2 cameras.");
                return;
            }

            Debug.Log("Init 2 player vertical splitscreen");

            float halfHeight = (safeArea.height - borderWidth) / 2;

            Rect topRect = new(
                safeArea.x,
                safeArea.y + halfHeight + borderWidth,
                safeArea.width,
                halfHeight
            );
            playerCameras[0].pixelRect = topRect;

            Rect bottomRect = new(
                safeArea.x,
                safeArea.y,
                safeArea.width,
                halfHeight
            );
            playerCameras[1].pixelRect = bottomRect;
        }

        /// <summary>
        /// Sets up three player cameras plus an optional overview/UI camera
        /// Layout is top left, top right, and bottom left, with bottom right being the overview
        /// </summary>
        private void SetupThreePlayers(Rect safeArea) {
            if (playerCameras.Length < 3) {
                Debug.LogError("Not enough cameras for three player mode. Need at least 3 cameras.");
                return;
            }

            Debug.Log("Init 3 player splitscreen");

            float halfWidth = (safeArea.width - borderWidth) / 2;
            float halfHeight = (safeArea.height - borderWidth) / 2;

            Rect topLeftRect = new(
                safeArea.x,
                safeArea.y + halfHeight + borderWidth,
                halfWidth,
                halfHeight
            );
            playerCameras[0].pixelRect = topLeftRect;

            Rect topRightRect = new(
                safeArea.x + halfWidth + borderWidth,
                safeArea.y + halfHeight + borderWidth,
                halfWidth,
                halfHeight
            );
            playerCameras[1].pixelRect = topRightRect;

            Rect bottomLeftRect = new(
                safeArea.x,
                safeArea.y,
                halfWidth,
                halfHeight
            );
            playerCameras[2].pixelRect = bottomLeftRect;

            if (playerCameras.Length < 4) {
                return;
            }

            Rect bottomRightRect = new(
                safeArea.x + halfWidth + borderWidth,
                safeArea.y,
                halfWidth,
                halfHeight
            );
            playerCameras[3].pixelRect = bottomRightRect;
        }

        /// <summary>
        /// Sets up four player cameras in a 2x2 grid
        /// </summary>
        private void SetupFourPlayers(Rect safeArea) {
            if (playerCameras.Length < 4) {
                Debug.LogError("Not enough cameras for four player mode. Need at least 4 cameras.");
                return;
            }

            Debug.Log("Init 4 player splitscreen");

            float halfWidth = (safeArea.width - borderWidth) / 2;
            float halfHeight = (safeArea.height - borderWidth) / 2;

            Rect topLeftRect = new(
                safeArea.x,
                safeArea.y + halfHeight + borderWidth,
                halfWidth,
                halfHeight
            );
            playerCameras[0].pixelRect = topLeftRect;

            Rect topRightRect = new(
                safeArea.x + halfWidth + borderWidth,
                safeArea.y + halfHeight + borderWidth,
                halfWidth,
                halfHeight
            );
            playerCameras[1].pixelRect = topRightRect;

            Rect bottomLeftRect = new(
                safeArea.x,
                safeArea.y,
                halfWidth,
                halfHeight
            );
            playerCameras[2].pixelRect = bottomLeftRect;

            Rect bottomRightRect = new(
                safeArea.x + halfWidth + borderWidth,
                safeArea.y,
                halfWidth,
                halfHeight
            );
            playerCameras[3].pixelRect = bottomRightRect;
        }

        #endregion

        private void OnEnable() {
            if (AirConsole.instance) {
                AirConsole.instance.OnSafeAreaChanged += HandleSafeAreaChanged;
            }
        }

        private void OnDisable() {
            if (AirConsole.instance) {
                AirConsole.instance.OnSafeAreaChanged -= HandleSafeAreaChanged;
            }
        }
#endif
    }
}