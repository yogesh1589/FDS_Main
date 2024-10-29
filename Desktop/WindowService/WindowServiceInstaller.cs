using FDS.Common;
using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace FDS.WindowService
{
    internal class WindowServiceInstaller
    {


        string serviceExePath = string.Empty;

        public void InstallService(string serviceName, string fileName)
        {

            string AutoStartBaseDir = Generic.GetApplicationpath();
            serviceExePath = Path.Combine(AutoStartBaseDir, fileName);

            if (IsServiceInstalled(serviceName))
            {
                Console.WriteLine("Service already installed.");
            }
            else
            {
                ManagedInstallerClass.InstallHelper(new string[] { serviceExePath });
                Console.WriteLine("Service installed.");
            }
        }

        public bool IsServiceInstalled(string serviceName)
        {
            ServiceController[] services = ServiceController.GetServices();
            foreach (ServiceController service in services)
            {
                if (service.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public void UninstallService(string serviceName, string fileName)
        {
            string AutoStartBaseDir = Generic.GetApplicationpath();
            serviceExePath = Path.Combine(AutoStartBaseDir, fileName);

            if (!IsServiceInstalled(serviceName))
            {
                Console.WriteLine("Service not installed.");
            }
            else
            {
                ManagedInstallerClass.InstallHelper(new string[] { "/u", serviceExePath });
                Console.WriteLine("Service uninstalled.");
            }
        }


        public void StartService(string serviceName)
        {
            ServiceController serviceController = new ServiceController(serviceName);
            if (serviceController.Status == ServiceControllerStatus.Running)
            {
                Console.WriteLine("Service already running.");
            }
            else
            {
                serviceController.Start();
                Console.WriteLine("Service started.");
            }
        }

        public void StopService(string serviceName)
        {
            ServiceController serviceController = new ServiceController(serviceName);
            if (serviceController.Status == ServiceControllerStatus.Stopped)
            {
                Console.WriteLine("Service already stopped.");
            }
            else
            {
                serviceController.Stop();
                serviceController.WaitForStatus(ServiceControllerStatus.Stopped);
                Console.WriteLine("Service stopped.");
            }
        }


    }
}
