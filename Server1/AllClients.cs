using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Server1
{
    public abstract class AllClients
    {
        public string name { get; set; }
        public Int32 portAddress { get; set; }
        public IPAddress ipClient { get; set; }
        public bool IsOnline { get; set; }

        public AllClients(string n, IPEndPoint client, bool isOnline)
        {
            this.name = n;
            this.portAddress = client.Port;
            this.ipClient = client.Address;
            IsOnline = isOnline;
        }
    }
}
