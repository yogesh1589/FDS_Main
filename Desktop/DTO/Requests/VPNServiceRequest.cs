using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace FDS.DTO.Requests
{
    public class VPNServiceRequest
    {
        public VPNData Data { get; set; }
        public string Message { get; set; }
        public int Duration { get; set; }
        public string Hostname { get; set; }
    }
    public class VPNData
    {
        public int Code { get; set; }
        public string Config { get; set; }
    }
}
