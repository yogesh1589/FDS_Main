using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDS.DTO.Requests
{
    public class ServiceDPP
    {
        public string ServiceName { get; set; }
        public bool IsActive { get; set; }

        public bool IsSubscribe { get; set; }

        public int ServiceID { get; set; }
    }
}
