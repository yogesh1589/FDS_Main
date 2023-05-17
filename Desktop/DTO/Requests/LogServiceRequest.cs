﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDS.DTO.Requests
{
    public class LogServiceRequest
    {
        public string serial_number { get; set; }
        public string mac_address { get; set; }
        public string authorization_token { get; set; }
        public string sub_service_authorization_code { get; set; }
        public string sub_service_name { get; set; }
        public string current_user { get; set; }
        public bool executed { get; set; }
        public string file_deleted { get; set; }
    }
}
