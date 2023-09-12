using FDS.DTO.Responses;
using FDS.Factories;
using FDS.Logging;
using FDS.Services;
using FDS.Services.AbstractClass;
using FDS.Services.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

namespace FDS.Runners
{
    public class ScheduleRunner
    {
        bool IsAdmin => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

        public async Task<List<BaseService>> ServiceToRun(Dictionary<string, SubservicesData> dicEventServices, string serviceTypeDetails)
        {
            List<BaseService> resultService = new List<BaseService>();

            var tasks = new List<Task>();
            IService service = null;

            var serviceFactory = new ServiceFactory();

            foreach (ServiceTypeName serviceType in Enum.GetValues(typeof(ServiceTypeName)))
            {
                if (dicEventServices.ContainsKey(serviceType.ToString()))
                {
                    if (dicEventServices.TryGetValue(serviceType.ToString(), out SubservicesData subservices))
                    {

                    }

                    switch (serviceType)
                    {
                        case ServiceTypeName.WindowsRegistryProtection:
                            if (IsAdmin)
                            {
                                service = serviceFactory.CreateService(ServiceTypeName.WindowsRegistryProtection);
                                tasks.Add(Task.Run(() => service.RunService(subservices, serviceTypeDetails)));
                            }
                            break;
                        case ServiceTypeName.DnsCacheProtection:
                            service = serviceFactory.CreateService(ServiceTypeName.DnsCacheProtection);
                            tasks.Add(Task.Run(() => service.RunService(subservices, serviceTypeDetails)));
                            break;
                        case ServiceTypeName.TrashDataProtection:
                            service = serviceFactory.CreateService(ServiceTypeName.TrashDataProtection);
                            tasks.Add(Task.Run(() => service.RunService(subservices, serviceTypeDetails)));
                            break;
                        case ServiceTypeName.FreeStorageProtection:
                            service = serviceFactory.CreateService(ServiceTypeName.FreeStorageProtection);
                            tasks.Add(Task.Run(() => service.RunService(subservices, serviceTypeDetails)));
                            break;
                        case ServiceTypeName.SystemNetworkMonitoringProtection:
                            service = serviceFactory.CreateService(ServiceTypeName.SystemNetworkMonitoringProtection);
                            tasks.Add(Task.Run(() => service.RunService(subservices, serviceTypeDetails)));
                            break;
                        case ServiceTypeName.WebSessionProtection:
                            resultService.Add(new WebSessionProtection());
                            break;
                        case ServiceTypeName.WebTrackingProtecting:
                            resultService.Add(new WebTrackingProtecting());
                            break;
                        case ServiceTypeName.WebCacheProtection:
                            resultService.Add(new WebCacheProtection());
                            break;                       
                    }
                    await Task.WhenAll(tasks);
                }
            }

            return (resultService);
        }

        
        public async Task<bool> RunAll(Dictionary<string, SubservicesData> dicScheduleServices, string serviceRunType, List<string> whitelistedDomain)
        {
            List<BaseService> resultService = await ServiceToRun(dicScheduleServices, serviceRunType);

            if (resultService.Count > 0)
            {
                EventRunner eventRunner = new EventRunner();
                eventRunner.RunAll(dicScheduleServices, serviceRunType, whitelistedDomain);
            }

            return true;
        }
    }
}
