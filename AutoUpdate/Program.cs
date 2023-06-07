using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AutoUpdate
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Program pgm = new Program();
            string TempPath = "C:\\Temp\\FDS";
            Console.WriteLine("Hi! you are about to update your FDS application");
            string installationPath = "";
            if (!Directory.Exists(TempPath))
                Directory.CreateDirectory(TempPath);
            RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
            if (registryKey != null)
            {
                object obj = registryKey.GetValue("FDS");
                if (obj != null)
                    installationPath = Path.GetDirectoryName(obj.ToString());
            }
            pgm.ReplaceFiles(TempPath,installationPath);
        }
        #region AutoUpdate
       
        private void ReplaceFiles(string temporaryPath, string installationPath)
        {
            try
            {
                foreach (Process process in Process.GetProcessesByName("FDS"))
                {
                    process.Kill();
                    process.WaitForExit();
                }
                foreach (FileInfo file in new DirectoryInfo(temporaryPath).GetFiles())
                    file.CopyTo(Path.Combine(installationPath, file.Name));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error replacing files: " + ex.Message);
            }
        }
        #endregion
    }
}
