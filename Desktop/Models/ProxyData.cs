﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDS.Models
{

    public class ProxyData
    {
        public string device_uuid { get; set; }    

        public List<ProxyLists> proxy_info { get; set; }
        
    }
}
