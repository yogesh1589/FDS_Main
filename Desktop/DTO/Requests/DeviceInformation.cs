using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Desktop.DTO.Requests
{
    public class DeviceInformation
    {
        public string serial_number { get; set; }
        public string device_name { get; set; }
        public string mac_address { get; set; }
        public string device_type { get; set; }
    }

}
