using FDS.Services.AbstractClass;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

namespace FDS.Services
{


    public class CheckBrowsers
    {
        List<BaseService> services = new List<BaseService>
        {
            new WebCacheProtection,
            new WebSessionProtection,
            new WebTrackingProtection
        };

        string[] browserNames = { "chrome", "edge", "firefox" };

        public bool RunWebServices()
        {
            foreach (string browserName in browserNames)
            {
                foreach (var service in services)
                {
                    service.ExecuteLogicForBrowser(browserName);
                }
            }
            return true;
        }






    }
}
