using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDS.Logging
{
    public interface ILogger
    {
        void LogInformation(string authorizationCode, string subServiceName, long FileProcessed, string ServiceId, bool IsManualExecution);
    }
}
