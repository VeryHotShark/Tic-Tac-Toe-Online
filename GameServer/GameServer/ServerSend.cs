using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer
{
        class ServerSend
        {
                private static void SendTCPData(int toClient, Packet packet)
                {
                        packet.WriteLength();
                        Server.Clients[toClient].tcp.SendData(packet);
                }

                private static void SendUDPData(int toClient, Packet packet)
                {
                        packet.WriteLength();
                        Server.Clients[toClient].udp.SendData(packet);
                }

                private static void SendTCPDataToAll( Packet packet)
                {
                        packet.WriteLength();

                        for (int i = 1; i <= Server.MaxPlayers; i++)
                        {
                                Server.Clients[i].tcp.SendData(packet);
                        }
                }

                private static void SendTCPDataToAll(int exceptClient, Packet packet)
                {
                        packet.WriteLength();

                        for (int i = 1; i <= Server.MaxPlayers; i++)
                        {
                                if (i == exceptClient)
                                        continue;

                                Server.Clients[i].tcp.SendData(packet);
                        }
                }

                private static void SendUDPDataToAll(Packet packet)
                {
                        packet.WriteLength();

                        for (int i = 1; i <= Server.MaxPlayers; i++)
                        {
                                Server.Clients[i].udp.SendData(packet);
                        }
                }

                private static void SendUDPDataToAll(int exceptClient, Packet packet)
                {
                        packet.WriteLength();

                        for (int i = 1; i <= Server.MaxPlayers; i++)
                        {
                                if (i == exceptClient)
                                        continue;

                                Server.Clients[i].udp.SendData(packet);
                        }
                }

                #region Packets
                public static void Welcome(int toClient, string msg)
                {
                        using (Packet packet = new Packet((int)ServerPackets.welcome))
                        {
                                packet.Write(msg);
                                packet.Write(toClient);

                                SendTCPData(toClient, packet);

                        }
                }

                public static void SpawnPlayer(int toClient, Player player)
                {
                        using(Packet packet = new Packet((int)ServerPackets.spawnPlayer))
                        {
                                packet.Write(player.id);
                                packet.Write(player.username);
                                packet.Write(player.position);
                                packet.Write(player.rotation);

                                SendTCPData(toClient, packet);
                        }
                }

                public static void PlayerPosition(Player player)
                {
                        using (Packet packet = new Packet((int)ServerPackets.playerPosition))
                        {
                                packet.Write(player.id);
                                packet.Write(player.position);

                                SendUDPDataToAll( packet);
                        }
                }

                public static void PlayerRotation(Player player)
                {
                        using (Packet packet = new Packet((int)ServerPackets.playerRotation))
                        {
                                packet.Write(player.id);
                                packet.Write(player.rotation);

                                SendUDPDataToAll(player.id, packet);
                        }
                }
                #endregion
        }
}
