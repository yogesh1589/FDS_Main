using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LauncherApp
{
    internal class Program
    {
        private const string FdsProcessName = "FDS";

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;

        static void Main(string[] args)
        {
            // Get the handle to the console window
            IntPtr hWndConsole = GetConsoleWindow();

            // Hide the console window
            ShowWindow(hWndConsole, SW_HIDE);

            Console.WriteLine("Start");
            Timer timer = new Timer(CheckAndStartProcess, FdsProcessName, TimeSpan.Zero, TimeSpan.FromSeconds(30));

            Console.WriteLine("Checking for FDS.exe. Press Enter to exit.");
            Console.ReadLine();

            timer.Dispose(); // Dispose the timer when exiting
        }

        static void CheckAndStartProcess(object state)
        {
            string processName = (string)state;

            bool isInstalledByMSI = IsInstalledByMSI(FdsProcessName);
            WriteLog("Check Installer FDS 2" + DateTime.Now);
            //if (isInstalledByMSI)
            //{
            WriteLog("FDS.exe is installed via MSI 2. " + DateTime.Now);
            Console.WriteLine("FDS.exe is installed via MSI.2");

            if (!IsAppRunning(FdsProcessName))
            {
                Console.WriteLine("Calling Start UI");

                StartUIApplication();
            }
            else
            {
                Console.WriteLine("fds.exe is already running.");
            }
            //}
        }



        public static bool IsInstalledByMSI(string appName)
        {
            try
            {
                string query = $"SELECT * FROM Win32_Product WHERE Name = '{appName}'";
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
                {
                    ManagementObjectCollection results = searcher.Get();
                    foreach (ManagementObject obj in results)
                    {
                        // Check if the application was installed via MSI
                        if (obj["InstallSource"] != null)
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking installation: {ex.Message}");
            }
            return false;
        }


        private static void StartUIApplication()
        {
            try
            {
                WriteLog("Start UI App" + DateTime.Now);

                string applicationPath = "";
                RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
                if (registryKey != null)
                {
                    object obj = registryKey.GetValue("FDS");
                    if (obj != null)
                        applicationPath = Path.GetDirectoryName(obj.ToString());
                }
                string exeFile = Path.Combine(applicationPath, "FDS.exe");
                // Replace "YourUIApplication.exe" with the actual executable name or path of your UI application
                // string uiAppPath = "C:\\Fusion Data Secure\\FDS\\FDS.exe";
                string uiAppPath = exeFile;

                // Start the UI application process
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = uiAppPath,
                    // Additional settings or parameters as needed
                };

                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting UI application: {ex.Message}");
            }
        }

        private static void WriteLog(string logMessage)
        {
            try
            {
                string path = AppDomain.CurrentDomain.BaseDirectory + "Logs_Launcher";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string filePath = Path.Combine(path, "ServiceLog_" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt");

                using (StreamWriter streamWriter = File.AppendText(filePath))
                {
                    streamWriter.WriteLine($"{DateTime.Now} - {logMessage}");
                }
            }
            catch (Exception ex)
            {

            }
        }

        public static bool IsAppRunning(string processName)
        {
            int bCnt = 0;
            bool result = false;
            Process[] chromeProcesses = Process.GetProcessesByName(processName);
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

    }
}
