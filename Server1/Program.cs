using System;
using System.IO;
using System.Net;
using System.Net.Sockets;



namespace Server1
{
    class Program
    {
        protected static List<AllClients> members;
        protected static TcpListener server; 
        public static bool serverOn;
        public static Int32 port, portUdp;
        protected static IPEndPoint groupEP;
        public static UdpClient listener;

        public static void Main()
        {
            SetUp();
            server.Start();
            while (serverOn)
            {
                Console.WriteLine("Waiting for connection...");
                TcpClient cl = server.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(ServerRunnig, cl);
                ThreadPool.QueueUserWorkItem(ServerRunningUdp, groupEP);
            }
            
        }

        private static void SetUp()
        {
            members = new List<AllClients>();
            server = null;
            port = 13000;
            portUdp = 14000;
            IPAddress localAddr = IPAddress.Parse("127.0.0.1");
            groupEP = new IPEndPoint(IPAddress.Any, 0);
            listener = new UdpClient(portUdp);
            server = new TcpListener(localAddr, port);
            serverOn = true;
        }

        private static void ServerRunningUdp(Object obj)
        {            
            Console.WriteLine("Server Udp started");            
            IPEndPoint group = (IPEndPoint)obj;
            while (true)
            {
                if (listener == null)
                    listener = new UdpClient(portUdp);
                try
                {
                    listenOnUdp(listener, group);
                }
                catch (SocketException e)
                {
                    Console.WriteLine(e);
                }
            }               
        }

        private static void listenOnUdp(UdpClient listener, IPEndPoint group)
        {
            Console.WriteLine("Waiting for broadcast");
            byte[] bytes = listener.Receive(ref group);
            string data = System.Text.Encoding.ASCII.GetString(bytes, 0, bytes.Length);
            Console.WriteLine($"Received broadcast from {group} : {data}");
            string action = data.Split("/").Last();
            RecivedActionByUdp(action, data, group);
        }

        private static void RecivedActionByUdp(string action, string data, IPEndPoint group)
        {
            switch (action)
            {
                case "ConnectUdp":
                    {
                        ConnectServerByUdp(group, data);
                        break;
                    }
                case "Send":
                    {
                        string nameSender = data.Split("/").First();
                        SendMessageByUDP(data, nameSender);
                        break;
                    }
                case "Disconnect":
                    {
                        DisconnectClientOnUdp(group.Port);
                        break;
                    }
            }
        }

        private static void DisconnectClientOnUdp(int port)
        {
            foreach(var client in members)
            {
                if (client.portAddress == port && client.name.Contains("UdpTemp"))
                {
                    members.Remove(client);
                    break;
                }
            }
        }

        private static void ConnectServerByUdp(IPEndPoint group, string data)
        {
            string name = data.Split("/").First() + "UdpTemp";
            members.Add(new ClientsUdp(name,group, true));
        }

        private static void ServerRunnig(Object obj)
        {
                Console.WriteLine("Server Tcp started");
                TcpClient clientConnected = (TcpClient)obj;                
                while (true)
                {
                    if (clientConnected.Connected)
                    {
                        Console.WriteLine($"{((IPEndPoint)clientConnected.Client.RemoteEndPoint).Address.ToString()} Connected!");
                        try
                        {
                            listenOnTcp(clientConnected);
                        }
                        catch (SocketException e)
                        {
                            Console.WriteLine($"Connection closed {e}");
                        }
                    }
                }
                server.Stop();
            Console.WriteLine("\nHit enter to continue...");
            Console.Read();
        }

        private static void listenOnTcp(TcpClient clientConnected)
        {
            String data = null;
            Byte[] bytes = new Byte[256];
            NetworkStream stream = clientConnected.GetStream();
            int i;

            while (clientConnected.Connected && (i = stream.Read(bytes, 0, bytes.Length)) != 0)
            {

                data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                Console.WriteLine("Received: {0}", data);
                string action = data.Split("/").Last();
                RecivedActionByTcp(action, data, clientConnected);
            }
        }

        private static void RecivedActionByTcp(string action, string data, TcpClient clientConnected)
        {
            switch (action)
            {
                case "ConnectTcp":
                    {
                        ConnectToClient(clientConnected, data);
                        break;
                    }
                case "Send":
                    {
                        string nameSender = data.Split("/").First();
                        SendMessage(data, nameSender);
                        break;
                    }
                case "Disconnect":
                    {
                        DisconnectClientOnTcp(clientConnected);
                        break;
                    }
            }
        }

        private static void DisconnectClientOnTcp(TcpClient clientConnected)
        {
            foreach (var client in members)
            {
                if (client.portAddress == ((IPEndPoint)clientConnected.Client.RemoteEndPoint).Port)
                {
                    if (client.GetType() == typeof(ClientsTcp))
                        CloseSocketForTcp(clientConnected);
                    members.Remove(client);
                    break;
                }
            }
            SendUpdateContactList();
        }

        private static void CloseSocketForTcp(TcpClient client)
        {
            lock (client)
            {
                client.GetStream().Close();
                client.Close();
            }
        }

        private static void SendMessageByUDP(string data, string nameSender)
        {
            foreach(AllClients client in members)
            {
                if (client.name == (data.Split("/")[1]+"UdpTemp"))
                {
                    byte[] msgToSend = CreateMessage(data, nameSender);
                    listener.Send(msgToSend, msgToSend.Length, new IPEndPoint(client.ipClient,client.portAddress));
                    break;
                }
            }      
        }

        private static byte[] CreateMessage(string data, string nameSender)
        {
            string msg = nameSender + ": " + data.Split("/")[2] + "-Message";
            byte[] msgToSend = System.Text.Encoding.ASCII.GetBytes(msg);
            return msgToSend;
        }

        private static void SendMessage(string data, string nameSender)
        {
            foreach (AllClients client in members)
            {
                if (client.GetType()==typeof(ClientsTcp)&&client.name == data.Split("/")[1])
                {
                    ClientsTcp tempClient = (ClientsTcp)client;
                    byte[] msgToSend = CreateMessage(data, nameSender);
                    NetworkStream stream = tempClient.client.GetStream(); 
                    stream.Write(msgToSend, 0, msgToSend.Length);
                    break;
                }               
            }
        }

        private static void ConnectToClient(TcpClient clientConnected, string data)
        {
            string name = data.Split("/").First();
            members.Add(new ClientsTcp(clientConnected, name , true));            
            SendUpdateContactList();
        }

        private static void SendUpdateContactList()
        {
            foreach (AllClients client in members.ToList())
            {
                if (client.GetType() == typeof(ClientsTcp))
                {
                    ClientsTcp temp = (ClientsTcp)client;
                    string onlineMem = ListToSTring(temp.name);
                    byte[] buffer = System.Text.Encoding.ASCII.GetBytes(onlineMem);
                    NetworkStream stream = temp.client.GetStream();
                    stream.Write(buffer, 0, buffer.Length);
                }
            }
        }

        private static string ListToSTring(string name)
        {
            string ret = "";
            foreach (var cl in members)
            {
                if(cl.name!=name&&cl.IsOnline&&!cl.name.Contains("UdpTemp"))
                    ret+=$"{cl.name}/";
            }
            ret = ret + "-Contacts";
            Console.WriteLine($"User online sent {ret}");
            return ret;
        }
    }
}
