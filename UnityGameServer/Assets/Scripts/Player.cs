using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Player : MonoBehaviour {
        public int id;
        public string username;

        private float moveSpeed = 5f / Constants.TICKS_PER_SEC;
        private bool[] inputs;

        public void Initialize(int id, string username) {
                this.id = id;
                this.username = username;

                inputs = new bool[4];
        }

        public void FixedUpdate() {
                Vector2 inputDirection = Vector2.zero;

                if (inputs[0])
                        inputDirection.y += 1;
                if (inputs[1])
                        inputDirection.y -= 1;
                if (inputs[2])
                        inputDirection.x -= 1;
                if (inputs[3])
                        inputDirection.x += 1;

                Move(inputDirection);
        }

        private void Move(Vector2 inputDirection) {
                Vector3 moveDirection = transform.right * inputDirection.x + transform.forward * inputDirection.y;
                transform.position += moveDirection * moveSpeed;

                ServerSend.PlayerPosition(this);
                ServerSend.PlayerRotation(this);
        }

        public void SetInput(bool[] inputs, Quaternion rotation) {
                this.inputs = inputs;
                transform.rotation = rotation;
        }
}