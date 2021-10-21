using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

class Client {
        public static int dataBufferSize = 4096;

        public int id;
        public Player player;
        public TCP tcp;
        public UDP udp;

        public Client(int id) {
                this.id = id;
                tcp = new TCP(id);
                udp = new UDP(id);
        }

        public class TCP {
                public TcpClient socket;

                private readonly int _id;
                private NetworkStream _stream;
                private Packet _receivedData;
                private byte[] _receiveBuffer;

                public TCP(int id) => _id = id;

                public void Connect(TcpClient socket) {
                        this.socket = socket;
                        this.socket.ReceiveBufferSize = dataBufferSize;
                        this.socket.SendBufferSize = dataBufferSize;

                        _stream = this.socket.GetStream();

                        _receivedData = new Packet();
                        _receiveBuffer = new byte[dataBufferSize];

                        _stream.BeginRead(_receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);

                        ServerSend.Welcome(_id, "Welcome on the server!");
                }

                public void SendData(Packet packet) {
                        try {
                                if (socket != null) {
                                        _stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
                                }
                        }
                        catch (Exception ex) {
                                Debug.Log($"Error sending data to player {_id} via TCP: {ex}");
                        }
                }

                private void ReceiveCallback(IAsyncResult result) {
                        try {
                                int byteLength = _stream.EndRead(result);

                                if (byteLength <= 0) {
                                        Server.Clients[_id].Disconnect();
                                        return;
                                }

                                byte[] data = new byte[byteLength];
                                Array.Copy(_receiveBuffer, data, byteLength);

                                _receivedData.Reset(HandleData(data));
                                _stream.BeginRead(_receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
                        }
                        catch (Exception ex) {
                                Debug.Log($"Error receiving TCP data: {ex}");
                                Server.Clients[_id].Disconnect();
                        }
                }
                private bool HandleData(byte[] data) {
                        int packetLength = 0;

                        _receivedData.SetBytes(data);

                        if (_receivedData.UnreadLength() >= 4) {
                                packetLength = _receivedData.ReadInt();

                                if (packetLength <= 0)
                                        return true;
                        }

                        while (packetLength > 0 && packetLength <= _receivedData.UnreadLength()) {
                                byte[] packetBytes = _receivedData.ReadBytes(packetLength);
                                ThreadManager.ExecuteOnMainThread(() => {
                                        using(Packet packet = new Packet(packetBytes)) {
                                                int packetId = packet.ReadInt();
                                                Server.PacketHandlers[packetId](_id, packet);
                                        }
                                });

                                packetLength = 0;
                                if (_receivedData.UnreadLength() >= 4) {
                                        packetLength = _receivedData.ReadInt();

                                        if (packetLength <= 0)
                                                return true;
                                }
                        }

                        if (packetLength <= 1)
                                return true;

                        return false;
                }

                public void Disconnect() {
                        socket.Close();
                        _stream = null;
                        _receivedData = null;
                        _receiveBuffer = null;
                        socket = null;
                }
        }

        public class UDP {
                public IPEndPoint endPoint;
                public int id;

                public UDP(int id) => this.id = id;

                public void Connect(IPEndPoint endPoint) {
                        this.endPoint = endPoint;
                }

                public void SendData(Packet packet) {
                        Server.SendUDPData(endPoint, packet);
                }

                public void HandleData(Packet packetData) {
                        int packetLength = packetData.ReadInt();
                        byte[] packetBytes = packetData.ReadBytes(packetLength);

                        ThreadManager.ExecuteOnMainThread(() => {
                                using(Packet packet = new Packet(packetBytes)) {
                                        int packetId = packet.ReadInt();
                                        Server.PacketHandlers[packetId](id, packet);
                                }
                        });
                }

                public void Disconnect() {
                        endPoint = null;
                }
        }

        public void SendIntoGame(string playerName) {
                player = NetworkManager.instance.InstantiatePlayer();
                player.Initialize(id, playerName);

                foreach (Client client in Server.Clients.Values) {
                        if (client.player != null) {
                                if (client.id != id) {
                                        ServerSend.SpawnPlayer(id, client.player);
                                }
                        }
                }

                foreach (Client client in Server.Clients.Values) {
                        if (client.player != null) {
                                ServerSend.SpawnPlayer(client.id, player);
                        }
                }
        }

        public void Disconnect() {
                Debug.Log($"{tcp.socket.Client.RemoteEndPoint} has disconnected.");

                UnityEngine.Object.Destroy(player.gameObject);
                player = null;

                tcp.Disconnect();
                udp.Disconnect();
        }
}