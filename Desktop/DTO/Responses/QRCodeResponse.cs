using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDS.DTO.Responses
{
    public class QRCodeResponse
    {
        public string Authorization_token { get; set; }
        public string Authentication_token { get; set; }
        public string Public_key { get; set; }
    }
}
