using FDS.Logging;
using FDS.Services;
using FDS.Services.AbstractClass;
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
        private readonly ILogger _logger;

        public ServiceFactory(ILogger logger)
        {
            _logger = logger;
        }

        public BaseService CreateService(ServiceTypeName type)
        {
            switch (type)
            {
                case ServiceTypeName.DNSCacheProtection:
                    return new DNSCacheProtection(_logger);
                case ServiceTypeName.FreeStorageProtection:
                    return new FreeStorageProtection(_logger);
                case ServiceTypeName.TrashDataProtection:
                    return new TrashDataProtection(_logger);
                case ServiceTypeName.WebCacheProtection:
                    return new WebCacheProtection(_logger);
                case ServiceTypeName.WebSessionProtection:
                    return new WebSessionProtection(_logger);
                case ServiceTypeName.WebTrackingProtection:
                    return new WebTrackingProtection(_logger);
                case ServiceTypeName.WindowsRegistryProtection:
                    return new WindowsRegistryProtection(_logger);
                // Create other service instances similarly
                default:
                    throw new ArgumentException("Invalid service type.");
            }
        }

    }
}
