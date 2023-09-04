﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FDS.DTO.Responses
{
    public class DeviceResponse
    {
        public string qr_code_token { get; set; }
        public bool Success { get; set; }   
        public HttpStatusCode httpStatusCode { get; set; }
    }
}
