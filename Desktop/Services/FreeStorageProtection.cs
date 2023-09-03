using FDS.DTO.Responses;
 
using FDS.Logging;
using FDS.Services.AbstractClass;
using FDS.Services.Interface;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

namespace FDS.Services
{
    public class FreeStorageProtection : IService,ILogger
    { 

        public bool RunService(SubservicesData subservices, string serviceTypeDetails)
        {

            try
            {
                string memoryCleaning = @"cipher /w:c:\";
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo("cmd.exe", memoryCleaning)
                };
                process.Start();
                KillCmd();

                LogInformation(subservices.Sub_service_authorization_code, subservices.Sub_service_name, 0, Convert.ToString(subservices.Id), subservices.Execute_now, serviceTypeDetails);

            }
            catch (Exception exp)
            {
                exp.ToString();
            }          

            return true;
        }

        public void LogInformation(string authorizationCode, string subServiceName, long FileProcessed, string ServiceId, bool IsManualExecution, string serviceTypeDetails)
        {
            DatabaseLogger databaseLogger = new DatabaseLogger();
            databaseLogger.LogInformation(authorizationCode, subServiceName, FileProcessed, ServiceId, IsManualExecution, serviceTypeDetails);
        }


        public void KillCmd()
        {
            Array.ForEach(Process.GetProcessesByName("cmd"), x => x.Kill());
        }
    }
}
