﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace GameServer
{
        class Server
        {
                public static int MaxPlayers { get; private set; }
                public static int Port { get; private set; }
                public static Dictionary<int, Client> Clients = new Dictionary<int, Client>();

                public delegate void PacketHandler(int fromClient, Packet packet);
                public static Dictionary<int, PacketHandler> PacketHandlers;

                private static TcpListener tcpListener;
                private static UdpClient udpListener;

                public static void Start(int maxPlayers, int port)
                {
                        MaxPlayers = maxPlayers;
                        Port = port;

                        Console.WriteLine("Starting Server...");
                        InitializeServerData();

                        tcpListener = new TcpListener(IPAddress.Any, Port);
                        tcpListener.Start();
                        tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

                        udpListener = new UdpClient(Port);
                        udpListener.BeginReceive(UDPReceiveCallback, null);

                        Console.WriteLine($"Server started on {Port}.");
                }

                private static void TCPConnectCallback(IAsyncResult result)
                {
                        TcpClient client = tcpListener.EndAcceptTcpClient(result);
                        tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
                        Console.WriteLine($"Incoming connection from {client.Client.RemoteEndPoint}... ");

                        for (int i = 1; i <= MaxPlayers; i++)
                        {
                                if (Clients[i].tcp.socket == null)
                                {
                                        Clients[i].tcp.Connect(client);
                                        return;
                                }
                        }

                        Console.WriteLine($"{client.Client.RemoteEndPoint} failed to connect: Server full!");
                }

                private static void UDPReceiveCallback(IAsyncResult result)
                {
                        try
                        {
                                IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                                byte[] data = udpListener.EndReceive(result, ref clientEndPoint);
                                udpListener.BeginReceive(UDPReceiveCallback, null);

                                if (data.Length < 4)
                                {
                                        return;
                                }

                                using (Packet packet = new Packet(data))
                                {
                                        int clientId = packet.ReadInt();

                                        if (clientId == 0)
                                                return;

                                        if(Clients[clientId].udp.endPoint == null)
                                        {
                                                Clients[clientId].udp.Connect(clientEndPoint);
                                                return;
                                        }

                                        if(Clients[clientId].udp.endPoint.ToString() == clientEndPoint.ToString() )
                                        {
                                                Clients[clientId].udp.HandleData(packet);
                                        }
                                }

                        } 
                        catch (Exception ex)
                        {
                                Console.WriteLine($"Error receiving UDP data: {ex}");
                        }
                }

                public static void SendUDPData(IPEndPoint clientEndPoint, Packet packet)
                {
                        try
                        {
                                if(clientEndPoint != null)
                                {
                                        udpListener.BeginSend(packet.ToArray(), packet.Length(), clientEndPoint, null, null);
                                }
                        }
                        catch (Exception ex)
                        {
                                Console.WriteLine($"Error sending data to  {clientEndPoint} via UDP: {ex}");
                        }
                }

                private static void InitializeServerData()
                {
                        for (int i = 1; i <= MaxPlayers; i++)
                        {
                                Clients.Add(i, new Client(i));
                        }

                        PacketHandlers = new Dictionary<int, PacketHandler>()
                        {
                                { (int) ClientPackets.welcomeReceived, ServerHandle.WelcomeReceived},
                                { (int) ClientPackets.playerMovement, ServerHandle.PlayerMovement},
                        };

                        Console.WriteLine("Initialized Packets");
                }
        }
}
