using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Server1
{
    class ClientsUdp : AllClients
    {

        public IPEndPoint groupEP { get; set; }

        public ClientsUdp(string Name, IPEndPoint g, bool ol) : base(Name, g, ol)
        {

            groupEP = g;
        }
    }
}
