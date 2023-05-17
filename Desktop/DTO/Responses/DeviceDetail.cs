using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDS.DTO.Responses
{
    public class DeviceDetail
    {
        public int id { get; set; }
        public string mac_address { get; set; }
        public string serial_number { get; set; }
        public string device_name { get; set; }
        public string device_type { get; set; }
        public string public_key_server { get; set; }
        public string public_key_device { get; set; }
        public string private_key_server { get; set; }
        public string qr_code_token { get; set; }
        public DateTime qr_code_generated_on { get; set; }
        public string authorization_token { get; set; }
        public string authentication_token { get; set; }
        public DateTime created_on { get; set; }
        public DateTime updated_on { get; set; }
        public object last_seen { get; set; }
        public bool is_active { get; set; }
        public bool is_authenticated { get; set; }
        public bool is_re_authenticated { get; set; }
        public DateTime re_authenticated_on { get; set; }
        public bool credentials_shared { get; set; }
        public bool qr_code_token_used { get; set; }
        public string device_location { get; set; }
        public int authenticated_by { get; set; }
    }

}
