using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Desktop
{
    internal class _Uninstall
    {
        public static void UninstallApp()
        {
            // Remove any files installed by the application
            string installPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Directory.Delete(installPath, true);

            // Remove any registry entries created by the application
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Fusion Tech Solution", true))
            {
                if (key != null)
                {
                    key.DeleteSubKeyTree("Fusion Tech Solution");
                }
            }

            // Notify the user that the application has been uninstalled
            MessageBox.Show("MyApplication has been uninstalled.", "Uninstall Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
