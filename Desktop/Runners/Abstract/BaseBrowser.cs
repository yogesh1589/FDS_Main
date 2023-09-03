using FDS.Common;
using FDS.DTO.Responses;
using FDS.Factories;
using FDS.Logging;
using FDS.Services;
using FDS.Services.AbstractClass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDS.Runners.Abstract
{
    public abstract class BaseBrowser
    {
        //public abstract bool RunLogic(List<BaseService> servicesToRun, List<string> whitelistedDomain);

        public abstract bool LogData(List<BaseService> servicesToRun, Dictionary<string, SubservicesData> dicEventServices, string serviceRunType);

        public List<BaseService> ServiceToRun(Dictionary<string, SubservicesData> dicEventServices)
        {
            List<BaseService> resultService = new List<BaseService>();

            foreach (ServiceTypeName serviceType in Enum.GetValues(typeof(ServiceTypeName)))
            {
                if (dicEventServices.ContainsKey(serviceType.ToString()))
                {
                    switch (serviceType)
                    {
                        case ServiceTypeName.WebCacheProtection:
                            resultService.Add(new WebCacheProtection());
                            break;
                        case ServiceTypeName.WebSessionProtection:
                            resultService.Add(new WebSessionProtection());
                            break;
                        case ServiceTypeName.WebTrackingProtecting:
                            resultService.Add(new WebTrackingProtecting());
                            break;
                    }
                }
            }

            return resultService;
        }

        public async Task<List<string>> GetWhiteListDomains(Dictionary<string, SubservicesData> dicEventServices)
        {
            //Get white domain list for web session protection service
            List<string> whitelistedDomain1 = new List<string>();
            if (dicEventServices.TryGetValue(ServiceTypeName.WebSessionProtection.ToString(), out SubservicesData Subservices))
            {
                DatabaseLogger databaseLogger = new DatabaseLogger();
                whitelistedDomain1 = await databaseLogger.GetWhiteListDomains(Subservices.Id.ToString());
            }
            return whitelistedDomain1;
        }

        public bool RunLogic(List<BaseService> servicesToRun, List<string> whitelistedDomain)
        {
            bool result = false;
            try
            {
                string[] browserNames = { "chrome", "msedge", "firefox", "opera" };
                foreach (string browserName in browserNames)
                {
                    bool isBrowserRunning = BrowsersGeneric.IsBrowserOpen(browserName);


                    bool isBrowserInstalled = BrowsersGeneric.IsBrowserInstalled(browserName);

                    if (isBrowserInstalled)
                    {
                        foreach (var service in servicesToRun)
                        {
                            int logicResult = service.ExecuteLogicForBrowserIfClosed(browserName, !isBrowserRunning, whitelistedDomain);
                            service.AddLogicResult(logicResult);
                        }
                    }

                }
                result = true;
            }
            catch (Exception ex)
            {
                result = false;
            }
            return result;
        }

    }
}
