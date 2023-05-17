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
    }
}
