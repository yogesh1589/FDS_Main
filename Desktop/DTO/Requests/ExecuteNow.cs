using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDS.DTO.Requests
{
    public class ExecuteNow
    {
        public bool execute_now { get; set; }
        public string sub_serviceId { get; set; }
    }
}
