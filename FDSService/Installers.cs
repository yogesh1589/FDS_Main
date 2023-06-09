using System.Collections;
using System.ComponentModel;
using System.ServiceProcess;
using System.Configuration.Install;

namespace FDSService
{
    [RunInstaller(true)]
    public partial class Installers : Installer
    {
        public Installers()
        {
            InitializeComponent();

            //// Disable "Just Me" option
            //foreach (var dialog in this.Installers)
            //{
            //    if (dialog is InstallDialogSequence)
            //    {
            //        var installDialogSequence = (InstallDialogSequence)dialog;
            //        foreach (var dialog1 in installDialogSequence)
            //        {
            //            if (dialog1 is WelcomeDialog)
            //            {
            //                var welcomeDialog = (WelcomeDialog)dialog1;
            //                welcomeDialog.Display = InstallDialogMode.Full;
            //            }
            //        }
            //    }
            //}


            ServiceProcessInstaller processInstaller = new ServiceProcessInstaller();
            ServiceInstaller serviceInstaller = new ServiceInstaller();

            processInstaller.Account = ServiceAccount.LocalSystem;
            serviceInstaller.StartType = ServiceStartMode.Automatic;
            serviceInstaller.DelayedAutoStart = true;
            serviceInstaller.ServiceName = "FDS";
            serviceInstaller.DisplayName = "FDS";
            serviceInstaller.Description = "This is a sample service.";

            Installers.Add(serviceInstaller);
            Installers.Add(processInstaller);
        }
        protected override void OnBeforeInstall(IDictionary savedState)
        {
            base.OnBeforeInstall(savedState);

            // Set the ALLUSERS property to "1" to disable the "Just Me" option
            Context.Parameters["ALLUSERS"] = "1";
        }
        //public override void Install(System.Collections.IDictionary stateSaver)
        //{
        //    base.Install(stateSaver);

        //    using (ServiceProcessInstaller processInstaller = new ServiceProcessInstaller())
        //    {
        //        processInstaller.Account = ServiceAccount.LocalSystem;

        //        using (ServiceInstaller serviceInstaller = new ServiceInstaller())
        //        {
        //            serviceInstaller.ServiceName = "FDS";
        //            serviceInstaller.DisplayName = "FDS";
        //            serviceInstaller.Description = "FDS is improving performance and managing space in system";
        //            serviceInstaller.StartType = ServiceStartMode.Automatic;
        //            serviceInstaller.DelayedAutoStart = true;

        //            Installers.Add(processInstaller);
        //            Installers.Add(serviceInstaller);

        //            Context.LogMessage("Installing service...");
        //        }
        //    }
        //}

        //public override void Uninstall(System.Collections.IDictionary savedState)
        //{
        //    base.Uninstall(savedState);

        //    using (ServiceController controller = new ServiceController("FDS"))
        //    {
        //        if (controller.Status == ServiceControllerStatus.Running)
        //        {
        //            controller.Stop();
        //        }

        //        Installers.Remove(Installers[0]);
        //        Installers.Remove(Installers[1]);

        //        Context.LogMessage("Service uninstalled.");
        //    }
        //}
    }
}
