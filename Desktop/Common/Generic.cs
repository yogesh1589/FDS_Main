using FDS.DTO.Responses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.Devices.Usb;
using Windows.Security.Cryptography.Certificates;

namespace FDS.Common
{
    static class Generic
    {

        static bool showMessageBoxes = true;
        private const string GoogleHost = "www.google.com";


        public static bool CheckInternetConnection()
        {
            try
            {
                using (var ping = new Ping())
                {
                    PingReply reply = ping.Send(GoogleHost);
                    return reply != null && reply.Status == IPStatus.Success;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static int AlreadyRunningInstance()
        {
            int instanceCount = 0;
            try
            {
                Process currentProcess = Process.GetCurrentProcess();
                Process[] processes = Process.GetProcessesByName("FDS");

                foreach (var p in processes)
                {
                    string username = GetProcessOwner(p.Id);
                    if (p.ProcessName.Equals("fds", StringComparison.OrdinalIgnoreCase) &&
                        username.Equals(Environment.UserName, StringComparison.OrdinalIgnoreCase))
                    {
                        instanceCount++;
                        IntPtr hFound = p.MainWindowHandle;
                        if (User32API.IsIconic(hFound))
                            User32API.ShowWindow(hFound, User32API.SW_RESTORE);
                        User32API.SetForegroundWindow(hFound);
                    }
                }
            }
            catch { }
            return instanceCount;
        }

        public static string GetProcessOwner(int processId)
        {
            string query = "Select * From Win32_Process Where ProcessID = " + processId;
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();
            string username = string.Empty;
            foreach (ManagementObject obj in processList)
            {
                string[] argList = new string[] { string.Empty, string.Empty };
                int returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
                if (returnVal == 0)
                {
                    username = argList[0];
                }
            }
            return username;
        }



    }
}
