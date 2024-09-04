using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDS.DTO.Responses
{
    public class LogEntry
    {
        public int file_deleted { get; set; }
        public string sentence { get; set; }
        public string service_name { get; set; }
        public string current_user { get; set; }
        public string time { get; set; }
        public string title { get; set; }
        public string header { get; set; }
        public string changed_by { get; set; }
    }

    public class LogData
    {
        public List<LogEntry> logs { get; set; }
        public bool next { get; set; }
    }

    public class LogResponse
    {
        public string message { get; set; }
        public LogData data { get; set; }
    }

}
