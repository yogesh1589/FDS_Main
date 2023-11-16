using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDS.DTO.Requests
{
    public class DeletedProxyRequest
    {
        public string device_uuid { get; set; }
        public List<ProxyDeletionRequest> deleted_proxy { get; set; }

        public List<ProxyDeletionRequest> no_deleted_proxy { get; set; }
    }
}
