﻿using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

class ServerHandle {
        public static void WelcomeReceived(int fromClient, Packet packet) {
                int clientIdCheck = packet.ReadInt();
                string username = packet.ReadString();

                Debug.Log($"{Server.Clients[fromClient].tcp.socket.Client.RemoteEndPoint} connected succesfully and is now player {fromClient} aka {username}.");

                if (fromClient != clientIdCheck) {
                        Debug.Log($"Player \" {username} \" (ID: {fromClient}) has assumed the wrong client ID ({clientIdCheck})! ");
                }
                Server.Clients[fromClient].SendIntoGame(username);
        }

        public static void PlayerMovement(int fromClient, Packet packet) {
                bool[] inputs = new bool[packet.ReadInt()];

                for (int i = 0; i < inputs.Length; i++) {
                        inputs[i] = packet.ReadBool();
                }

                Quaternion rotation = packet.ReadQuaternion();
                Server.Clients[fromClient].player.SetInput(inputs, rotation);
        }
}