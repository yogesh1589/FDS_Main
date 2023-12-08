using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace FDSWinService
{
    [RunInstaller(true)]
    public class ProjectInstaller : Installer
    {
        private ServiceProcessInstaller processInstaller;
        private ServiceProcessInstaller serviceProcessInstaller;
        private ServiceInstaller serviceInstaller1;
        private ServiceInstaller serviceInstaller;

        public ProjectInstaller()
        {
            processInstaller = new ServiceProcessInstaller();
            serviceInstaller = new ServiceInstaller();

            // Set the account under which the service should run
            processInstaller.Account = ServiceAccount.LocalSystem;

            // Set service installer properties
            serviceInstaller.ServiceName = "FDS_Service";
            serviceInstaller.DisplayName = "FDS_Service";
            serviceInstaller.StartType = ServiceStartMode.Automatic;

            // Add installers to the collection
            Installers.Add(processInstaller);
            Installers.Add(serviceInstaller);
        }

        private void InitializeComponent()
        {
            this.serviceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.serviceInstaller1 = new System.ServiceProcess.ServiceInstaller();
            // 
            // serviceProcessInstaller
            // 
            this.serviceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.serviceProcessInstaller.Password = null;
            this.serviceProcessInstaller.Username = null;
            // 
            // serviceInstaller1
            // 
            this.serviceInstaller1.Description = "windows service for FDS";
            this.serviceInstaller1.DisplayName = "FDS_Watchdog";
            this.serviceInstaller1.ServiceName = "Service_FDS";
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.serviceProcessInstaller,
            this.serviceInstaller1});

        }
    }

}
