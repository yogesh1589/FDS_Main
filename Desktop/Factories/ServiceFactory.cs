
using FDS.Logging;
using FDS.Runners.Abstract;
using FDS.Services;
using FDS.Services.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace FDS.Factories
{
    public class ServiceFactory
    {
        
        public IService CreateService(ServiceTypeName type)
        {
            switch (type)
            {
                case ServiceTypeName.DNSCacheProtection:
                    return new DNSCacheProtection();
                case ServiceTypeName.FreeStorageProtection:
                    return new FreeStorageProtection();
                case ServiceTypeName.TrashDataProtection:
                    return new TrashDataProtection();
                case ServiceTypeName.WebCacheProtection:
                    return new WebCacheProtection();
                case ServiceTypeName.WebSessionProtection:
                    return new WebSessionProtection();
                case ServiceTypeName.WebTrackingProtecting:
                    return new WebTrackingProtecting();
                case ServiceTypeName.WindowsRegistryProtection:
                    return new WindowsRegistryProtection();
                // Create other service instances similarly
                default:
                    throw new ArgumentException("Invalid service type.");
            }
        }

    }
}
