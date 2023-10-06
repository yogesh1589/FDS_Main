﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDS.DTO.Responses
{
    public class CertificateResponse
    {
        public int id { get; set; }
        public string Subject { get; set; }
        public bool whitelisted { get; set; }
        public string Severity { get; set; }
        public string Thumbprint { get; set; }
        public string FriendlyName { get; set; }
        public string SerialNumber { get; set; }
        public string SignatureAlgorithm { get; set; }
        public string Issuer { get; set; }
        public string PublicKey { get; set; }
        public string StoreName { get; set; }
        public string StoreLocation { get; set; }
        public string Version { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public bool rejected { get; set; }
        public int Relatedto { get; set; }
    }
}
