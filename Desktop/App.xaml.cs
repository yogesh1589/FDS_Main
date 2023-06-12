using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace FDS
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            //if (SingleInstance.AlreadyRunning())
            //    App.Current.Shutdown(); // Just shutdown the current application,if any instance found.  

            //if (!IsRunningAsAdmin())
            //{
            //    LaunchElevationHelperProcess();
            //    Shutdown();
            //    return;
            //}
            //[ProgramFilesFolder]
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\CurrentVersion\Policies\System", "EnableLUA", 0);
            base.OnStartup(e);
        }

        public static bool IsRunningAsAdmin()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        private void LaunchElevationHelperProcess()
        {
            MessageBox.Show("LaunchElevationHelperProcess" + Directory.GetCurrentDirectory());
            var currentProcess = Process.GetCurrentProcess();
            var elevationHelperProcessName = "elevation_helper.exe"; // The name of your elevation helper executable

            // Check if the elevation helper process is already running
            var isElevationHelperRunning = Process.GetProcessesByName(elevationHelperProcessName)
                .Any(process => process.Id != currentProcess.Id);

            if (isElevationHelperRunning)
            {
                // Exit the WPF application without launching again
                Current.Shutdown();
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = Directory.GetCurrentDirectory() + "\\FDS.exe", // Path to your WPF application executable
                Verb = "runas" // Request elevation
            };

            try
            {
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur during the process start
                // For example, you can display an error message or log the exception
                MessageBox.Show($"Failed to launch WPF application with elevation: {ex.Message}");
            }
        }
    }

}
