using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
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
            RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            string AutoStartBaseDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string exeFile = Path.Combine(AutoStartBaseDir, "FDS.exe");
            Assembly curAssembly = Assembly.GetExecutingAssembly();
            key.SetValue("FDS", exeFile);

            //if (SingleInstance.AlreadyRunning())
            //    App.Current.Shutdown(); // Just shutdown the current application,if any instance found.  

            base.OnStartup(e);
        }
    }

}
