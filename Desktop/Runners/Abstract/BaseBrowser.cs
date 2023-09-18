using FDS.Common;
using FDS.DTO.Responses;
using FDS.Factories;
using FDS.Logging;
using FDS.Services;
using FDS.Services.AbstractClass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.ApplicationModel.Appointments.DataProvider;

namespace FDS.Runners.Abstract
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public abstract class BaseBrowser
    {
        //public abstract bool RunLogic(List<BaseService> servicesToRun, List<string> whitelistedDomain);

        public abstract bool LogData(List<BaseService> servicesToRun, Dictionary<string, SubservicesData> dicEventServices, string serviceRunType);

        public (List<BaseService>,Dictionary<BaseService, ServiceTypeName>) ServiceToRun(Dictionary<string, SubservicesData> dicEventServices)
        {
            List<BaseService> resultService = new List<BaseService>();
            Dictionary<BaseService, ServiceTypeName> serviceTypeMapping = new Dictionary<BaseService, ServiceTypeName>();


            foreach (ServiceTypeName serviceType in Enum.GetValues(typeof(ServiceTypeName)))
            {
                if (dicEventServices.ContainsKey(serviceType.ToString()))
                {
                    BaseService service = null;
                    switch (serviceType)
                    {
                        case ServiceTypeName.WebCacheProtection:
                            service = new WebCacheProtection();
                            break;
                        case ServiceTypeName.WebSessionProtection:
                            service = new WebSessionProtection();
                            break;
                        case ServiceTypeName.WebTrackingProtecting:
                            service = new WebTrackingProtecting();
                            break;
                    }


                    if (service != null)
                    {
                        resultService.Add(service);
                        serviceTypeMapping.Add(service, serviceType);
                    }
                }
            }

            

            return (resultService, serviceTypeMapping);
        }

        public List<string> CreateEventList()
        {
            List<string> lstResult = new List<string>();

            lstResult.Add("WebCacheProtection");
            lstResult.Add("WebSessionProtection");
            lstResult.Add("WebTrackingProtecting");

            return lstResult;
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

                    if ((isBrowserInstalled) || (browserName == "opera"))
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
