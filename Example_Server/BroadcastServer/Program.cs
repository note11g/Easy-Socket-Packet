using SocketPacket.PacketSocket;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace BroadcastServer
{
    class Program
    {
        private static PacketSocket socket =
            new PacketSocket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static List<PacketSocket> clients = new List<PacketSocket>();

        public static void Main(string[] args)
        {
            socket.NoDelay = true;
            socket.SendTimeout = 600;

            socket.Bind(new IPEndPoint(IPAddress.Any, 2000));
            socket.AcceptCompleted += new EventHandler<PacketSocketAsyncEventArgs>(ClientConnected);
            socket.DisconnectCompleted += new EventHandler<PacketSocketAsyncEventArgs>(ClientDisconnected);
        }

        private static void ClientConnected(object sender, PacketSocketAsyncEventArgs e)
        {
            e.AcceptSocket.ReceiveCompleted += new EventHandler<PacketSocketAsyncEventArgs>(PacketReceived);
            e.AcceptSocket.NoDelay = true;
            e.AcceptSocket.SendTimeout = 600;
            Console.WriteLine("Client Connected : {0}", e.AcceptSocket.RemoteEndPoint.ToString());

            clients.Add(e.AcceptSocket);
        }

        private static void ClientDisconnected(object sender, PacketSocketAsyncEventArgs e)
        {
            if (e.DisconnectSocket != socket)
            {
                Console.WriteLine("Client Disconnected : {0}", e.DisconnectSocket.RemoteEndPoint.ToString());
                clients.Remove(e.DisconnectSocket);
            }
        }

        private static void PacketReceived(object sender, PacketSocketAsyncEventArgs e)
        {
            for (int i = 0; i < e.ReceivePacketAmount; i++)
            {
                SocketPacket.Network.Packet packet = e.ReceiveSocket.Receive();

                // 중간에 리스트 변경 방지
                List<PacketSocket> copyList = clients.ConvertAll(s => s);

                for(int k = 0; k < copyList.Count; k++)
                {
                    try
                    {
                        if (copyList[k] != e.ReceiveSocket) copyList[k].Send(packet);
                    }
                    catch { }
                }

                //보낸이에겐 무조건 가게끔
                e.ReceiveSocket.Send(packet);
            }
        }
    }
}
