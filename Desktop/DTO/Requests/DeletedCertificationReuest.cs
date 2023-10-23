using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDS.DTO.Requests
{
    public class DeletedCertificationReuest
    {
        public string device_uuid { get; set; }
        public List<CertificateDeletionRequest> deleted_certificates { get; set; }

        public List<CertificateDeletionRequest> no_deleted_certificates { get; set; }
    }
}
