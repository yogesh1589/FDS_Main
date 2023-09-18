using FDS.Common;
using FDS.DTO.Requests;
using FDS.DTO.Responses;
using FDS.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Windows.Media.Protection.PlayReady;

namespace FDS.Services.AbstractClass
{
    public abstract class BaseService
    {
        private int totalLogicResult = 0;
        public HttpClient client { get; }
        public string OriginalTypeName { get; set; }

        public void AddLogicResult(int result)
        {
            totalLogicResult += result;
        }
        public int GetTotalLogicResult()
        {
            return totalLogicResult;
        }

        public abstract int ExecuteLogicForBrowserIfClosed(string browserName, bool isBrowserClosed, List<string> whiteListDomain);

        public abstract void LogService(string authorizationCode, string subServiceName, long FileProcessed, string ServiceId, bool IsManualExecution, string serviceTypeDetails);

    }
}
