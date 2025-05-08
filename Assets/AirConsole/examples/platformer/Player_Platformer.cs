namespace NDream.AirConsole.Examples {
    using UnityEngine;

    public class Player_Platformer : MonoBehaviour {
        private Rigidbody rigidBody;

        private bool movingLeft;
        private bool movingRight;

        private float playerSpeed = 0.1f;
        private float jumpForce = 350f;

        private bool isInSphere;
#if !DISABLE_AIRCONSOLE
        private void Start() {
            rigidBody = GetComponent<Rigidbody>();
        }

        public void ButtonInput(string input) {
            switch (input) {
                case "right":
                    movingRight = true;
                    break;
                case "left":
                    movingLeft = true;
                    break;
                case "right-up":
                    movingRight = false;
                    break;
                case "left-up":
                    movingLeft = false;
                    break;
                case "jump":
                    rigidBody.AddForce(transform.up * jumpForce);
                    break;
                case "interact":
                    if (isInSphere) {
                        if (Camera.main.backgroundColor == Color.yellow) {
                            Camera.main.backgroundColor = Color.blue;
                        } else {
                            Camera.main.backgroundColor = Color.yellow;
                        }
                    }

                    break;
            }
        }

        private void FixedUpdate() {
            if (movingLeft && !movingRight) {
                rigidBody.MovePosition(rigidBody.position + new Vector3(-playerSpeed, 0, 0));
            } else if (!movingLeft && movingRight) {
                rigidBody.MovePosition(rigidBody.position + new Vector3(playerSpeed, 0, 0));
            }
        }

        //Track if the player capsule is currently inside the transparent sphere or not
        private void OnTriggerEnter(Collider trigger) {
            if (trigger.tag == "PlatformSphere") {
                isInSphere = true;
            }
        }

        private void OnTriggerExit(Collider trigger) {
            if (trigger.tag == "PlatformSphere") {
                isInSphere = false;
            }
        }
#endif
    }
}