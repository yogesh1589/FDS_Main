using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FDS.DTO.Responses
{
    public class ServiceResponseNew
    {
        public List<DataNode> Data { get; set; }

        public string msg { get; set; }
        public string error { get; set; }
        public bool Success { get; set; }

        public HttpStatusCode HttpStatusCode { get; set; }

    }

    public class DataNode
    {
        public List<ServiceDataNew> Services { get; set; }
    }

    public class ServiceDataNew
    {
        public int Id { get; set; }
        public string Service_name { get; set; }
        public int Service_count { get; set; }
        public bool Service_active { get; set; }
        public List<SubserviceData> Subservices { get; set; }
    }

    public class SubserviceData
    {
        public int Id { get; set; }
        public string name { get; set; }
        public int Service_count { get; set; }
        public bool Service_active { get; set; }
        public string sub_service_name { get; set; } // Change 'Sub_service_name' to 'sub_service_name'
        public string sub_service_authorization_code { get; set; } // Change 'Sub_service_authorization_code' to 'sub_service_authorization_code'
        public bool sub_service_active { get; set; } // Change 'Sub_service_active' to 'sub_service_active'
        public string execution_period { get; set; } // Change 'Execution_period' to 'execution_period'
        public bool execute_now { get; set; } // Change 'Execute_now' to 'execute_now'
        public bool skip_execution { get; set; } // Add 'skip_execution'
        public bool Execute_Skipped_Service { get; set; } // Add 'Execute_Skipped_Service'
        public bool subscribe { get; set; } // Add 'subscribe'
    }
    
}
