using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.AppBroadcasting;

namespace FDS.DTO.Requests
{
    public class CertificateDeletionRequest
    {
        public string Thumbprint { get; set; }
        public string StoreName { get; set; }
        public string StoreLocation { get; set; }       
    }
}
