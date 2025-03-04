using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace NDream.AirConsole.Examples {
    public class ExamplePongLogic : MonoBehaviour {
        public Rigidbody2D racketLeft;
        public Rigidbody2D racketRight;
        public Rigidbody2D ball;
        public float ballSpeed = 10f;
        public Text uiText;
#if !DISABLE_AIRCONSOLE
        private int scoreRacketLeft;
        private int scoreRacketRight;

        private string screen = LOBBY;

        private const string LOBBY = "LOBBY";
        private const string GAME = "GAME";

        private void Awake() {
            AirConsole.instance.onMessage += OnMessage;
            AirConsole.instance.onPause += OnPause;
            AirConsole.instance.onResume += OnResume;
            AirConsole.instance.onAdShow += OnAdShow;
            AirConsole.instance.onAdComplete += OnAdComplete;
            AirConsole.instance.onConnect += OnConnect;
            AirConsole.instance.onDisconnect += OnDisconnect;
        }

        /// <summary>
        ///     Each time a new device connects we check if we have enough players to start the game
        ///     NOTE: We store the controller device_ids of the active players. We do not hardcode player device_ids 1 and 2,
        ///     because the two controllers that are connected can have other device_ids e.g. 3 and 7.
        ///     For more information read: http://developers.airconsole.com/#/guides/device_ids_and_states
        /// </summary>
        /// <param name="device_id">The device_id that connected</param>
        private void OnConnect(int device_id) {
            CheckTwoPlayers();
        }

        /// <summary>
        ///     If the game is running and one of the active players leaves, we reset the game.
        /// </summary>
        /// <param name="device_id">The device_id that has left.</param>
        private void OnDisconnect(int device_id) {
            var player = AirConsole.instance.ConvertDeviceIdToPlayerNumber(device_id);
            if (player >= 0)
                // Player that was in the game left the game.
                // Setting active players to length 0
                AirConsole.instance.SetActivePlayers(0);

            CheckTwoPlayers();
        }

        /// <summary>
        ///     We check for the start game command and otherwise which one of the active players has moved the paddle.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="data">Data.</param>
        private void OnMessage(int device_id, JToken data) {
            if (data["start_game"] != null) {
                StartGame();
                return;
            }

            var active_player = AirConsole.instance.ConvertDeviceIdToPlayerNumber(device_id);
            if (active_player != -1) {
                if (active_player == 0) racketLeft.velocity = Vector3.up * (float)data["move"];

                if (active_player == 1) racketRight.velocity = Vector3.up * (float)data["move"];
            }
        }

        private void OnPause() {
            Time.timeScale = 0;
            // If we were playing any sounds we must mute them when the game gets paused
            AudioListener.pause = true;
        }

        private void OnResume() {
            Time.timeScale = 1;
            AudioListener.pause = false;
        }

        /// <summary>
        /// Pause the game when Ad is shown to the player
        /// </summary>
        private void OnAdShow() {
            Time.timeScale = 0;
            // If we were playing any sounds we must mute them when an ad is shown
            AudioListener.pause = true;
        }

        /// <summary>
        /// Resume the game after the Ad completed
        /// </summary>
        /// <param name="adShown"></param>
        private void OnAdComplete(bool adShown) {
            Time.timeScale = 1;
            AudioListener.pause = false;
        }

        /// <summary>
        ///     Updates the displayed text on the screen and sets the device state property "enough_players"
        ///     which allows the controller to react to the new device state
        /// </summary>
        private void CheckTwoPlayers() {
            List<int> connectedControllers = AirConsole.instance.GetControllerDeviceIds();

            // Only update if the game didn't have active players.
            if (AirConsole.instance.GetActivePlayerDeviceIds.Count == 0) {
                if (connectedControllers.Count > 2)
                    uiText.text = "Only 2 players can play, sorry";
                else if (connectedControllers.Count == 2)
                    uiText.text = "Ready to start";
                else if (connectedControllers.Count == 1)
                    uiText.text = "Need 1 more player!";
                else
                    uiText.text = "Need 2 more players!";

                AirConsole.instance.SetCustomDeviceStateProperty("enough_players", connectedControllers.Count == 2);
            }
        }

        /// <summary>
        ///     Sends a message to vibrate for a certain duration to the specific player device.
        /// </summary>
        /// <param name="player"></param>
        private void Vibrate(int player) {
            AirConsole.instance.Message(AirConsole.instance.ConvertPlayerNumberToDeviceId(player), new { vibrate = 1000 });
        }

        /// <summary>
        ///     Sets the device state property "screen" to the given string
        /// </summary>
        /// <param name="newScreen"></param>
        private void SetGameScreen(string newScreen) {
            screen = newScreen;
            AirConsole.instance.SetCustomDeviceStateProperty("screen", screen);
        }

        private void StartGame() {
            SetGameScreen(GAME);
            AirConsole.instance.SetActivePlayers(2);
            ResetBall(true);
            scoreRacketLeft = 0;
            scoreRacketRight = 0;
            UpdateScoreUI();
        }

        private void BackToLobby() {
            SetGameScreen(LOBBY);
            AirConsole.instance.SetActivePlayers(0);
            ResetBall(false);
            racketLeft.velocity = Vector2.zero;
            racketRight.velocity = Vector2.zero;
            scoreRacketLeft = 0;
            scoreRacketRight = 0;
            CheckTwoPlayers();
        }

        private void ResetBall(bool move) {
            // place ball at center
            ball.position = Vector3.zero;

            // push the ball in a random direction
            if (move) {
                Vector3 startDir = new Vector3(Random.Range(-1, 1f), Random.Range(-0.1f, 0.1f), 0);
                ball.velocity = startDir.normalized * ballSpeed;
            } else {
                ball.velocity = Vector3.zero;
            }
        }

        private void UpdateScoreUI() {
            uiText.text = scoreRacketLeft + ":" + scoreRacketRight;
        }

        private void FixedUpdate() {
            // check if ball reached one of the ends
            if (ball.position.x < -9f) {
                scoreRacketRight++;
                UpdateScoreUI();
                ResetBall(true);
                Vibrate(0);

                if (scoreRacketRight > 3) BackToLobby();
            }

            // check if ball reached the other one of the ends
            if (ball.position.x > 9f) {
                scoreRacketLeft++;
                UpdateScoreUI();
                ResetBall(true);
                Vibrate(1);

                if (scoreRacketLeft > 3) BackToLobby();
            }
        }

        private void OnDestroy() {
            // unregister airconsole events on scene change
            if (AirConsole.instance != null) {
                AirConsole.instance.onMessage -= OnMessage;
                AirConsole.instance.onPause -= OnPause;
                AirConsole.instance.onResume -= OnResume;
                AirConsole.instance.onConnect -= OnConnect;
                AirConsole.instance.onDisconnect -= OnDisconnect;
            }
        }
#endif
    }
}