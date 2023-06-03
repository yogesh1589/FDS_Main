using FDS.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDS.DTO.Responses
{
    public class CountryCodeResponse
    {
        public List<CountryCode> data { get; set; }
    }
    public class CountryCode
    {
        public string name { get; set; }
        public string phone_code { get; set; }
        public string flag { get; set; }
        public string country_code { get; set; }
        public string flag_emoji { get; set; }
    }
}
