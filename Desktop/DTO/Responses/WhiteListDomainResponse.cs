using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDS.DTO.Responses
{
    public class WhiteListDomainResponse
    {
        //public List<org_whitelist> org_whitelist;
        public List<whitelist_domain> device_domains;
        public List<whitelist_domain> org_domains;

    }
    public class org_whitelist
    {
        public string id { get; set; }
        public string domain_name { get; set; }
        public string url { get; set; }
        public string associated_to { get; set; }
        public string favicon { get; set; }
    }
    public class whitelist_domain
    {
        public string id { get; set; }
        public string domain_name { get; set; }
        public string url { get; set; }
        public string associated_to { get; set; }
        public string favicon { get; set; }
    }
}
