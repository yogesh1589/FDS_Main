using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace FDS.DTO.Responses
{
    public class ProxyPayloadResponse
    {
        [JsonProperty("data")]      

        public ProxyResponse[] ProxyResponse { get; set; }
    }
}
