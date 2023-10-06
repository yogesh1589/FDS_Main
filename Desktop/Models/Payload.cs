using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDS.Models
{
    public class Payload
    {
        public List<CertificationLists> local_user_personal_certs { get; set; }
        public List<CertificationLists> local_user_trusted_certs { get; set; }
        public List<CertificationLists> current_user_personal_certs { get; set; }
        public List<CertificationLists> current_user_trusted_certs { get; set; }
    }
}
