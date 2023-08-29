using FDS.DTO.Responses;
using FDS.Logging;
using FDS.Services.AbstractClass;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDS.Services
{
    public class FreeStorageProtection : BaseService
    {
        public FreeStorageProtection(ILogger logger) : base(logger)
        {
        }

        public override void RunService(SubservicesData subservices)
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
                LogServicesData(subservices.Sub_service_authorization_code, subservices.Sub_service_name, 0, Convert.ToString(subservices.Id), subservices.Execute_now);

            }
            catch (Exception exp)
            {
                exp.ToString();
            }
        }
    }
}
