using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.AccessControl;
using UnityEngine;

public class Client : MonoBehaviour {
    public static Client instance;
    public static int dataBufferSize = 4096;

    public string ip = "127.0.0.1";
    public int port = 28010;
    public int myId = 0;
    public TCP tcp;
    public UDP udp;

    private delegate void PacketHandler(Packet packet);
    private static Dictionary<int, PacketHandler> _packetHandlers;

    private void Awake() {
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(this);
    }

    private void Start() {
        tcp = new TCP();
        udp = new UDP();
    }

    public void ConnectToServer() {
        IntiializeClientData();
        tcp.Connect();
    }

    public class TCP {
        public TcpClient socket;

        private NetworkStream _stream;
        private Packet _receivedData;
        private byte[] _receiveBuffer;

        public void Connect() {
            socket = new TcpClient {
                ReceiveBufferSize = dataBufferSize,
                SendBufferSize = dataBufferSize,
            };

            _receiveBuffer = new byte[dataBufferSize];
            socket.BeginConnect(instance.ip, instance.port, ConnectCallback, socket);
        }

        private void ConnectCallback(IAsyncResult result) {
            socket.EndConnect(result);

            if (!socket.Connected)
                return;

            _stream = socket.GetStream();

            _receivedData = new Packet();

            _stream.BeginRead(_receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
        }

        public void SendData(Packet packet) {
            try {
                if (socket != null) {
                    _stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
                }
            } catch (Exception ex) {
                Debug.Log($"Error sending data to server via TCP: {ex}");
            }
        }

        private void ReceiveCallback(IAsyncResult result) {
            try {
                int byteLength = _stream.EndRead(result);

                if (byteLength <= 0) {
                    // TODO: disconnect
                    return;
                }

                byte[] data = new byte[byteLength];
                Array.Copy(_receiveBuffer, data, byteLength);

                // TODO: handle data
                _receivedData.Reset(HandleData(data));

                _stream.BeginRead(_receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            } catch (Exception ex) {
                Console.WriteLine($"Error receiving TCP data: {ex}");
                // TODO: disconnect
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
                        _packetHandlers[packetId](packet);
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
    }

    public class UDP {
        public UdpClient socket;
        public IPEndPoint endPoint;

        public UDP() {
            endPoint = new IPEndPoint(IPAddress.Parse(instance.ip), instance.port);
        }

        public void Connect(int localPort) {
            socket = new UdpClient(localPort);

            socket.Connect(endPoint);
            socket.BeginReceive(ReceiveCallback, null);

            using(Packet packet = new Packet()) {
                SendData(packet);
            }
        }

        public void SendData(Packet packet) {
            try {
                packet.InsertInt(instance.myId);

                if (socket != null)
                    socket.BeginSend(packet.ToArray(), packet.Length(), null, null);

            } catch (Exception ex) {
                Debug.Log($"Error sending data to server via UDP: {ex}");
            }
        }

        private void ReceiveCallback(IAsyncResult result) {
            try {
                byte[] data = socket.EndReceive(result, ref endPoint);
                socket.BeginReceive(ReceiveCallback, null);

                if (data.Length < 4) {
                    // TODO : disconnect
                    return;
                }

                HandleData(data);
            } catch (Exception ex) {
                // TODO discaonnect
            }
        }

        private void HandleData(byte[] data) {
            using(Packet packet = new Packet(data)) {
                int packetLength = packet.ReadInt();
                data = packet.ReadBytes(packetLength);
            }

            ThreadManager.ExecuteOnMainThread(() => {
                using(Packet packet = new Packet(data)) {
                    int packetId = packet.ReadInt();
                    _packetHandlers[packetId](packet);
                }
            });
        }
    }

    private void IntiializeClientData() {
        _packetHandlers = new Dictionary<int, PacketHandler>() {
            { (int) ServerPackets.welcome, ClientHandle.Welcome },
            {(int) ServerPackets.udpTest, ClientHandle.UDPTest }
        };

        Debug.Log("Initialized Packets");
    }

}