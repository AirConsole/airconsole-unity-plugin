using UnityEngine;

namespace NDream.AirConsole.Examples {
    public class Racket : MonoBehaviour {

        public ExamplePongLogic logic;

        // Use this for initialization
        void Start() { }

        void OnCollisionEnter2D(Collision2D col) {

            if (col.gameObject.GetComponent<Rigidbody2D>() != null) {

                float hitPos = (col.transform.position.y - transform.position.y) / (GetComponent<Collider2D>().bounds.size.y / 2);
                float hitDir = 1f;

                if (col.relativeVelocity.x > 0) {
                    hitDir = -1f;
                }

                Vector2 dir = new Vector2(hitDir, hitPos).normalized;
                col.gameObject.GetComponent<Rigidbody2D>().velocity = dir * logic.ballSpeed;

            }
        }
    }
}
