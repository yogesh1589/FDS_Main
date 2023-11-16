using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDS.DTO.Requests
{
    public class ProxyDeletionRequest
    {

        public string proxy_on { get; set; }
        public string proxy_on_plugin { get; set; }
        public string proxy_type { get; set; }
        public string proxy_address { get; set; }
        public string proxy_port { get; set; }
        public string proxy_ip { get; set; }

    }
}
