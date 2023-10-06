using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDS.DTO.Responses
{
    public class DeviceConfigCheckResponse
    {
        public bool config_change { get; set; }
        public List<string> call_api { get; set; } = new List<string>();

        public string url { get; set; } 

        public string certificateThumbprint { get; set; }

        public List<string> lstStorename { get; set; }

        public List<string> lstStoreLocation { get; set; }
    }
}
