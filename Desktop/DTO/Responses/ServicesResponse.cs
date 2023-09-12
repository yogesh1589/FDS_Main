using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDS.DTO.Responses
{
    public class ServicesResponse
    {
        public List<ServicesData> Services { get; set; }
    }

    public class ServicesData
    {
        public int Id { get; set; }
        public string Service_name { get; set; }
        public int Service_count { get; set; }
        public bool Service_active { get; set; }
        public List<SubservicesData> Subservices { get; set; }

    }
    public class SubservicesData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Sub_service_name { get; set; }
        public string Sub_service_authorization_code { get; set; }
        public bool Sub_service_active { get; set; }
        public string Execution_period { get; set; }
        public bool Execute_now { get; set; }

        public bool Execute_Skipped_Service { get; set; }
    }
}
