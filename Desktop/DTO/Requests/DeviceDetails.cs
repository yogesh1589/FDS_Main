﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Desktop.DTO.Requests
{
    public class DeviceDetails
    {
        public string serial_number { get; set; }
        public string mac_address { get; set; }
        public string authorization_token { get; set; }
        public string code_version { get; set; }
    }
}
