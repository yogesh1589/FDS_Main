﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FDS.DTO.Requests
{
    public class RetriveServices:KeyExchange
    {
        public string current_user { get; set; }

        public HttpStatusCode HttpStatusCode { get; set; }
    }
}
