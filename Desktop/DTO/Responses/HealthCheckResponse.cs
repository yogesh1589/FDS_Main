using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDS.DTO.Responses
{
    public class HealthCheckResponse
    {
        public bool success { get; set; }
        public bool call_config { get; set; }
    }
}
