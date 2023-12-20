using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDS_Administrator.General
{
    public static class Generic
    {
        public static string GetApplicationpath()
        {
            string applicationPath = "";
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
            if (registryKey != null)
            {
                object obj = registryKey.GetValue("FDS");
                if (obj != null)
                    return Path.GetDirectoryName(obj.ToString());
            }

            if (string.IsNullOrEmpty(applicationPath))
            {
                string currentPath = System.Reflection.Assembly.GetEntryAssembly()?.Location;
                applicationPath = Path.GetDirectoryName(currentPath);
            }
            return applicationPath;
        }
    }
}
