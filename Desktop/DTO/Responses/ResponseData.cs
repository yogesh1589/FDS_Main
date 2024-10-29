using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FDS.DTO.Responses
{
    public class ResponseData
    {
        public string Data { get; set; }
        public string msg { get; set; }
        public string error { get; set; }
        public bool Success { get; set; }

        public string payload { get; set; }
        public HttpStatusCode HttpStatusCode { get; set; }
    }
}
