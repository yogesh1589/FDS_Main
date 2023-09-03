using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace FDS.Common
{
    public class BrowsersGeneric
    {
        public static List<string> BrowsersProfileLists(string browserPath)
        {
            List<string> profiles = new List<string>();

            string defaultProfilePath = Path.Combine(browserPath, "Default");
            if (Directory.Exists(defaultProfilePath))
            {
                profiles.Add(defaultProfilePath);
            }
            if (Directory.Exists(browserPath))
            {
                string[] profileDirectories = Directory.GetDirectories(browserPath, "Profile *");

                foreach (string profileDir in profileDirectories)
                {
                    string profilePath = Path.Combine(browserPath, profileDir);
                    profiles.Add(profilePath);
                }
            }
            return profiles;
        }


        public static double CheckFileExistBrowser(string fullPath)
        {
            FileInfo fileInfo = new FileInfo(fullPath);
            double fileSizeInKb = 0;
            if (fileInfo.Exists)
            {
                long fileSizeInBytes = fileInfo.Length;
                fileSizeInKb = fileSizeInBytes / 1024.0; // Convert to kilobytes                


            }
            return fileSizeInKb;
        }


        public static bool IsBrowserOpen(string browser)
        {
            int bCnt = 0;
            bool result = false;
            Process[] chromeProcesses = Process.GetProcessesByName(browser);
            string test = string.Empty;
            foreach (Process process in chromeProcesses)
            {
                string processOwner = GetProcessOwner2(process.Id);
                if (!string.IsNullOrEmpty(processOwner))
                {
                    test = processOwner;
                    if (System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToUpper().ToString().Contains(processOwner.ToUpper().ToString()))
                    {
                        bCnt++;
                    }
                }
            }

            if (bCnt > 0)
            {
                result = true;
            }
            return result;
        }

        public static string GetProcessOwner2(int processId)
        {
            string query = "SELECT * FROM Win32_Process WHERE ProcessId = " + processId;
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();

            foreach (ManagementObject obj in processList)
            {
                string[] ownerInfo = new string[2];
                obj.InvokeMethod("GetOwner", (object[])ownerInfo);
                return ownerInfo[0];
            }
            return null;
        }


        public static bool IsBrowserInstalled(string browserName)
        {

            string[] browserNames = { "chrome", "edge", "firefox", "opera" };
            string bName = string.Empty;
            foreach (string browserName1 in browserNames)
            {
                if (browserName.Contains(browserName1))
                {
                    bName = browserName1;
                    break;
                }
            }
            using (var registryKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Clients\StartMenuInternet"))
            {
                if (registryKey != null)
                {
                    string[] subKeyNames = registryKey.GetSubKeyNames();
                    foreach (string subKeyName in subKeyNames)
                    {
                        if (subKeyName.IndexOf(bName, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

    }
}
