using FDS.DTO.Responses; 
using FDS.Logging;
using FDS.Services.Interface;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.Design;

namespace FDS.Services
{
    public class DNSCacheProtection : IService
    {

        public bool RunService(SubservicesData subservices, string serviceTypeDetails)
        {
            return true;
        }
        public int RunService()
        {
            string flushDnsCmd = @"/C ipconfig /flushdns";
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo("cmd.exe", flushDnsCmd)

                };
                process.Start();

                KillCmd();

                Console.WriteLine(String.Format("Successfully Flushed DNS:'{0}'", flushDnsCmd), EventLogEntryType.Information);

                //LogServicesData(subservices.Sub_service_authorization_code, subservices.Sub_service_name, 0, Convert.ToString(subservices.Id), subservices.Execute_now);

            }
            catch (Exception exp)
            {
                Console.WriteLine(String.Format("Failed to Flush DNS:'{0}' . Error:{1}", flushDnsCmd, exp.Message), EventLogEntryType.Error);
            }
            return 0;
        }

        public void KillCmd()
        {
            Array.ForEach(Process.GetProcessesByName("cmd"), x => x.Kill());
        }


    }
}
