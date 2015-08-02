using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using NDream.AirConsole;
using Newtonsoft.Json.Linq;

public class ExamplePongLogic : MonoBehaviour {

    public Rigidbody2D racketLeft;
    public Rigidbody2D racketRight;
    public Rigidbody2D ball;
    public float ballSpeed = 10f;
    public Text uiText;

    private int scoreRacketLeft = 0;
    private int scoreRacketRight = 0;


    void Start() {

        // register airconsole events
        AirConsole.instance.onMessage += OnMessage;

        // check if 2 players are connected
        StartCoroutine(WaitForPlayers());
    }

    IEnumerator WaitForPlayers() {

        // wait for 2 players (devices[0] is always the screen)
        while (AirConsole.instance.devices.Count < 3) {

            if (AirConsole.instance.devices.Count == 1) {
                uiText.text = "NEED 2 MORE PLAYERS";
            } else if (AirConsole.instance.devices.Count == 2) {
                uiText.text = "NEED 1 MORE PLAYER";
            }

            yield return null;
        }

        // start ball & update ui text
        ResetBall();
        UpdateScoreUI();
    }

    void ResetBall() {

        // place ball at center
        this.ball.position = Vector3.zero;

        // push the ball in a random direction
        Vector3 startDir = new Vector3(Random.Range(-1, 1f), Random.Range(-0.1f, 0.1f), 0);
        this.ball.velocity = startDir.normalized * this.ballSpeed;
    }

    void OnMessage(int from, JToken data) {

        if (from == 1) {
            // received movement from player 1
            this.racketLeft.velocity = Vector3.up * (float)data["move"];
        }

        if (from == 2) {
            // received movement from player 2
            this.racketRight.velocity = Vector3.up * (float)data["move"];
        }
    }

    void UpdateScoreUI() {
        // update text canvas
        uiText.text = scoreRacketLeft + ":" + scoreRacketRight;
    }

    void FixedUpdate() {

        // check if ball reached one of the ends
        if(this.ball.position.x < -9f){
            scoreRacketRight++;
            UpdateScoreUI();
            ResetBall();
        }

        if (this.ball.position.x > 9f) {
            scoreRacketLeft++;
            UpdateScoreUI();
            ResetBall();
        }
    }

    void OnDestroy() {

        // unregister airconsole events on scene change
        if (AirConsole.instance != null) {
            AirConsole.instance.onMessage -= OnMessage;
        }
    }
}
