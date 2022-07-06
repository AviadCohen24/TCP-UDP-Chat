using System;
using System.IO;
using System.Net;
using System.Net.Sockets;



namespace Server1
{
    class Clients
    {

        public TcpClient client { get; set; }
        public string name { get; set; }
        public Int32 portAddress { get; set; }
        public IPAddress ipClient { get; set; }
        
        public Clients(TcpClient client, string name, Int32 port, IPAddress ipClient)
        {
            this.client = client;
            this.name = name;
            this.portAddress = port;
            this.ipClient = ipClient;
        }
    }
    class Program
    {
        protected static List<Clients> members;
        protected static List<Clients> online;
        protected static TcpListener server;
        public static bool serverOn = true;
        public static void Main()
        {
            members = new List<Clients>();
            online = new List<Clients>();

            TcpListener server = null;
            Int32 port = 13000;
            IPAddress localAddr = IPAddress.Parse("127.0.0.1");
            server = new TcpListener(localAddr, port);
            server.Start();
            while (serverOn)
            {
                Console.WriteLine("Waiting for connection...");
                TcpClient cl = server.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(ServerRunnig, cl);
            }
            
        }

        private static void ServerRunnig(Object obj)
        {
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
                            case "Connect":
                                {
                                    ConnectToClient(clientConnected, data);


                                    /* -----Ask for online members-----*/

                                    break;
                                }
                            case "Send":
                                {
                                    string nameSender=data.Split("/").First();   
                                    SendMessage(data,nameSender);
                                    break;
                                }
                            case "Disconnect":
                                {
                                    foreach (var c in online)
                                    {
                                        if (c.client == clientConnected)
                                            online.Remove(c);
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

        private static void SendMessage(string data, string nameSender)
        {
            foreach (Clients c in online)
            {
                if (c.name == data.Split("/").First())
                {
                    string msg = nameSender+": "+data.Split("/")[1];
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
            members.Add(new Clients(clientConnected, data.Split("/").First(), portClient, ((IPEndPoint)clientConnected.Client.RemoteEndPoint).Address));
            online.Add(new Clients(clientConnected, data.Split("/").First(), portClient, ((IPEndPoint)clientConnected.Client.RemoteEndPoint).Address));
            string onlineMem = ListToSTring(data.Split("/").First());
            byte[] buffer = System.Text.Encoding.ASCII.GetBytes(onlineMem);
            NetworkStream stream = clientConnected.GetStream();
            stream.Write(buffer, 0, buffer.Length);
        }

        private static string ListToSTring(string name)
        {
            string ret = "";
            foreach (var cl in online)
            {
                if(cl.name!=name)
                    ret+=$"{cl.name}\\";
            }
            Console.WriteLine($"User online sent {ret}");
            if (ret != "")
                return ret.Substring(0, ret.Length - 1);
            else
                return "/";
        }
    }
}
