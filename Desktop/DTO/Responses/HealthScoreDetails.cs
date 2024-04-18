using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FDS.DTO.Responses
{
    public class HealthScoreDetails
    {

        public double? health_report { get; set; } // Change to nullable double
        public int? blacklisted_cert_count { get; set; } // Change to nullable int
        public int? blacklisted_proxy_count { get; set; }

        public HttpStatusCode HttpStatusCode { get; set; }

        public bool Success { get; set; }
    }
}
