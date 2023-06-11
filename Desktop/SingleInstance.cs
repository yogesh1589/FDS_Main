using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace FDS
{
    public sealed class SingleInstance
    {
        public static bool AlreadyRunning()
        {
            bool running = false;
            try
            {
                // Getting collection of process  
                Process currentProcess = Process.GetCurrentProcess();
                string username = GetProcessOwner(currentProcess.Id);
                // Check with other process already running   
                foreach (var p in Process.GetProcesses())
                {
                    if (p.Id != currentProcess.Id) // Check running process   
                    {
                        if (p.ProcessName.Equals(currentProcess.ProcessName) == true && username == Environment.UserName)
                        {
                            running = true;
                            IntPtr hFound = p.MainWindowHandle;
                            if (User32API.IsIconic(hFound)) // If application is in ICONIC mode then  
                                User32API.ShowWindow(hFound, User32API.SW_RESTORE);
                            User32API.SetForegroundWindow(hFound); // Activate the window, if process is already running  
                            break;
                        }
                    }
                }
            }
            catch { }
            return running;
        }
        public static string GetProcessOwner(int processId)
        {
            string query = "Select * From Win32_Process Where ProcessID = " + processId;
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();

            foreach (ManagementObject obj in processList)
            {
                string[] argList = new string[] { string.Empty, string.Empty };
                int returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
                if (returnVal == 0)
                {
                    // return DOMAIN\user 
                    //return argList[1] + "\\" + argList[0];
                    return argList[0];
                }
            }

            return "NO OWNER";
        }
    }
}
