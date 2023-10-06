using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDS.DTO.Responses
{
    public class CertificatePayloadResponse
    {
        public List<CertificateResponse> local_user_personal_certs { get; set; }
        public List<CertificateResponse> local_user_trusted_certs { get; set; }
        public List<CertificateResponse> current_user_personal_certs { get; set; }
        public List<CertificateResponse> current_user_trusted_certs { get; set; }

       
    }
}
