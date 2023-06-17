using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace AutoUpdate
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Program pgm = new Program();
            string TempFDSPath = "C:\\web\\Temp\\FDS";
            string TempPath = "C:\\web\\Temp";
            //Console.WriteLine("Hi! you are about to update your FDS application");
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
            //Console.WriteLine("TemPath: " + TempPath);
            //Console.WriteLine("installationPath: " + installationPath);
            //string[] filePaths = Directory.GetFiles(TempPath);
            //FileSecurity fileSecurity = File.GetAccessControl(installationPath);

            //// Create a rule for granting permission to Authenticated Users
            //SecurityIdentifier authenticatedUsersSid = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
            //FileSystemAccessRule accessRule = new FileSystemAccessRule(
            //    authenticatedUsersSid,
            //    FileSystemRights.FullControl,
            //    InheritanceFlags.None,
            //    PropagationFlags.NoPropagateInherit,
            //    AccessControlType.Allow
            //);

            //// Add the access rule to the file security
            //fileSecurity.AddAccessRule(accessRule);

            //// Apply the updated file security
            //File.SetAccessControl(installationPath, fileSecurity);


            foreach (Process process in Process.GetProcessesByName("FDS"))
            {
                process.Kill();
                process.WaitForExit();
            }

            // Iterate over the file paths and perform desired operations
            string[] subdirectories = Directory.GetDirectories(TempFDSPath);
            if (subdirectories.Length > 0)
            {
                foreach (string subdirectory in subdirectories)
                {
                    string subdirectoryName = Path.GetFileName(subdirectory);
                    foreach (FileInfo file in new DirectoryInfo(subdirectory).GetFiles())
                    {
                        string filePath = installationPath + "\\" + file.Name;
                        if (!file.Name.Contains("AutoUpdate.exe"))
                        {
                            if (File.Exists(filePath))
                            {
                                File.Replace(TempFDSPath + "\\" + subdirectoryName + "\\" + file.Name, installationPath + "\\" + subdirectoryName +"\\"+ file.Name, null);
                                //Console.WriteLine("File replaced successfully.");
                            }
                            else
                                file.CopyTo(Path.Combine(installationPath, file.Name));
                        }
                    }
                }
            }
            foreach (FileInfo file in new DirectoryInfo(TempFDSPath).GetFiles())
            {
                string filePath = installationPath + "\\"+ file.Name;
                if(!file.Name.Contains("AutoUpdate.exe"))
                {
                    if (File.Exists(filePath))
                    {
                        File.Replace(TempFDSPath + "\\" + file.Name, installationPath + "\\" + file.Name, null);
                        //Console.WriteLine("File replaced successfully.");
                    }
                    else
                        file.CopyTo(Path.Combine(installationPath, file.Name));
                }
            }
            string AutoUpdateExePath = Directory.GetCurrentDirectory() + "\\FDS.exe";
            Process.Start(AutoUpdateExePath);
            Directory.Delete(TempPath, true);
        }
    }
}
