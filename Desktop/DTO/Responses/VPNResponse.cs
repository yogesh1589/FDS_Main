using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDS.DTO.Responses
{
    public class VPNResponse
    {
        public InterfaceConfiguration Interface { get; set; }
        public PeerConfiguration Peer { get; set; }
    }

    

    public class InterfaceConfiguration
    {
        public string PrivateKey { get; set; }
        public string Address { get; set; }
        public List<string> DNS { get; set; }
    }

    public class PeerConfiguration
    {
        public string PublicKey { get; set; }
        public string Endpoint { get; set; }
        public string AllowedIPs { get; set; }
    }
}
