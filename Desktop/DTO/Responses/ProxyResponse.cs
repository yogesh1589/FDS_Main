using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDS.DTO.Responses
{
    public class ProxyResponse
    {
        [JsonProperty("proxy_on")]
        public string ProxyOn { get; set; }

        [JsonProperty("proxy_on_plugin")]
        public string ProxyOnPlugin { get; set; }

        [JsonProperty("proxy_type")]
        public string ProxyType { get; set; }

        [JsonProperty("proxy_address")]
        public string ProxyAddress { get; set; }

        [JsonProperty("proxy_port")]
        public string ProxyPort { get; set; }

        [JsonProperty("proxy_ip")]
        public string ProxyIp { get; set; }


    }
}
