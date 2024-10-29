using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace FDS.DTO.Responses
{
    public class VPNResponseNew
    {
        [JsonProperty("data")]
        public Data Data { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("duration")]
        public int Duration { get; set; }

        [JsonProperty("hostname")]
        public string Hostname { get; set; }
    }

    public class Data
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("config")]
        public string Config { get; set; }
    }
}
