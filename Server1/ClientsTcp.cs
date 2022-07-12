using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server1
{
    class ClientsTcp : AllClients
    {

        public TcpClient client { get; set; }


        public ClientsTcp(TcpClient client, string name, bool ol) : base(name, ((IPEndPoint)client.Client.RemoteEndPoint), ol)
        {
            this.client = client;

        }
    }
}
