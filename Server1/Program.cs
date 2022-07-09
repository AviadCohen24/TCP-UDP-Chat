using System;
using System.IO;
using System.Net;
using System.Net.Sockets;



namespace Server1
{
    public abstract class AllClients
    {
        public string name { get; set; }
        public Int32 portAddress { get; set; }
        public IPAddress ipClient { get; set; }
        public bool IsOnline { get; set; }  

        public AllClients(string n, Int32 port, IPAddress ip, bool isOnline)
        {
            this.name = n;
            this.portAddress = port;
            this.ipClient = ip;
            IsOnline = isOnline;
        }
    }
    
    class Clients:AllClients
    {

        public TcpClient client { get; set; }
        
        
        public Clients(TcpClient client, string name, Int32 port, IPAddress ipClient, bool ol):base(name,port,ipClient,ol)
        {
            this.client = client;
            
        }
    }
    class ClientsUdp:AllClients
    {

        public IPEndPoint groupEP { get; set; }

        public ClientsUdp( string Name, Int32 por, IPAddress ip, IPEndPoint g,bool ol):base(Name,por,ip,ol)
        {
                   
            groupEP = g;
        }
    }
    class Program
    {
        protected static List<AllClients> members;
        
        protected static TcpListener server;
        
        public static bool serverOn = true;
        public static Int32 port, portUdp;
        protected static IPEndPoint groupEP;
        public static UdpClient listener;

        public static void Main()
        {
            members = new List<AllClients>();            
            server = null;
            port = 13000;
            portUdp = 14000;            
            IPAddress localAddr = IPAddress.Parse("127.0.0.1");            
            groupEP = new IPEndPoint(IPAddress.Any, 0);
            listener = new UdpClient(portUdp);
            server = new TcpListener(localAddr, port);
            server.Start();
            while (serverOn)
            {
                Console.WriteLine("Waiting for connection...");
                TcpClient cl = server.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(ServerRunnig, cl);
                ThreadPool.QueueUserWorkItem(ServerRunningUdp, groupEP);
            }
            
        }

        private static void ServerRunningUdp(Object obj)
        {            
            Console.WriteLine("Server Udp started");            
            IPEndPoint group = (IPEndPoint)obj;
            try
            {
                while (true)
                {
                    if (listener == null)
                        listener = new UdpClient(portUdp);
                    Console.WriteLine("Waiting for broadcast");                   
                    byte[] bytes = listener.Receive(ref group);
                    string data = System.Text.Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                    Console.WriteLine($"Received broadcast from {group} : {data}");
                    string action = data.Split("/").Last();

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
                                foreach (var c in members)
                                {
                                    if (c.portAddress == group.Port)
                                    {
                                        c.IsOnline = false;
                                    }
                                }

                                break;
                            }
                    }
                    
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine(e);
            }
           
            
        }

        private static void ConnectServerByUdp(IPEndPoint group, string data)
        {
            members.Add(new ClientsUdp(data.Split("/").First()+"UdpTemp",group.Port,group.Address,group, true));
        }

        private static void ServerRunnig(Object obj)
        {
                Console.WriteLine("Server Tcp started");
                TcpClient clientConnected = (TcpClient)obj;
                Byte[] bytes = new Byte[256];
                String data = null;

                // Enter the listening loop.
                while (true)
                {
                    
                    Console.WriteLine($"{((IPEndPoint)clientConnected.Client.RemoteEndPoint).Address.ToString()} Connected!");

                    data = null;

                    // Get a stream object for reading and writing
                    NetworkStream stream = clientConnected.GetStream();

                    int i;

                    // Loop to receive all the data sent by the client.
                    
                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        // Translate data bytes to a ASCII string.
                        data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                        Console.WriteLine("Received: {0}", data);
                        string action = data.Split("/").Last();
                        // Process the data sent by the client.
                        switch (action)
                        {
                            case "ConnectTcp":
                                {
                                    ConnectToClient(clientConnected, data);


                                    /* -----Ask for online members-----*/

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
                                    foreach (var c in members)
                                    {
                                       if(c.portAddress== ((IPEndPoint)clientConnected.Client.RemoteEndPoint).Port)
                                       {
                                        c.IsOnline = false;
                                       }
                                    }

                                    break;
                                }
                        }
                    }   
                }
            server.Stop();
            

            Console.WriteLine("\nHit enter to continue...");
            Console.Read();
        }

        private static void SendMessageByUDP(string data, string nameSender)
        {
            foreach(AllClients client in members)
            {
                if (client.name == (data.Split("/")[1]+"UdpTemp"))
                {
                    string msg=nameSender+": "+data.Split("/")[2];
                    byte[] msgToSend = System.Text.Encoding.ASCII.GetBytes(msg);
                    listener.Send(msgToSend, msgToSend.Length, new IPEndPoint(client.ipClient,client.portAddress));
                    break;
                }
            }      
        }

        private static void SendMessage(string data, string nameSender)
        {
            foreach (Clients c in members)
            {
                if (c.name == data.Split("/")[1])
                {
                    string msg = nameSender+": "+data.Split("/")[2]+ "/Message";
                    byte[] msgToSend = System.Text.Encoding.ASCII.GetBytes(msg);
                    NetworkStream stream=c.client.GetStream();  
                    stream.Write(msgToSend, 0, msgToSend.Length);
                }
                break;
            }
        }

        private static void ConnectToClient(TcpClient clientConnected, string data)
        {
            Int32 portClient = Int32.Parse(data.Split("/")[1]);
            members.Add(new Clients(clientConnected, data.Split("/").First(), portClient, ((IPEndPoint)clientConnected.Client.RemoteEndPoint).Address, true));            

            SendUpdateContactList();
        }

        private static void SendUpdateContactList()
        {
            foreach(AllClients c in members)
            {
                if (c.GetType() == typeof(Clients))
                {
                    Clients temp = (Clients)c;
                    string onlineMem = ListToSTring(temp.name);
                    onlineMem += "/Contacts";
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
                    ret+=$"{cl.name}\\";
            }
            Console.WriteLine($"User online sent {ret}");
            if (ret != "")
                return ret.Substring(0, ret.Length - 1);
            else
                return "\\";
        }
    }
}
