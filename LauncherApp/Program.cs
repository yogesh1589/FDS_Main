using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
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
        public static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;  // Hides the window
        const int SW_SHOW = 5;  // Shows the window

        static void HideConsoleWindow()
        {
            IntPtr hWndConsole = GetConsoleWindow();
            if (hWndConsole != IntPtr.Zero)
            {
                ShowWindow(hWndConsole, SW_HIDE);
            }
        }


        static void Main(string[] args)
        {

            HideConsoleWindow();
            int cnt = 0;

            try
            {

               
                string basePathEncryption = String.Format("{0}Tempfolder", "C:\\Fusion Data Secure\\FDS\\");

                string encryptOutPutFile = basePathEncryption + @"\Main";

                //string applicationPath = "F:\\Fusion\\FDS\\windowsapp\\Desktop\\bin\\Debug\\";

                string applicationPath = ReturnApplicationPath();

                string exePath = Path.Combine(applicationPath, "FDS.exe");

                if (!string.IsNullOrEmpty(exePath))
                {
                    //DisableStartupEntry("FDS");
                    //DisableStartupEntry("LauncherApp");
                    //SetStartupApp(applicationPath);

                    while (true)
                    {

                        //System.Threading.Thread.Sleep(5000);
                        Thread.Sleep(TimeSpan.FromSeconds(10));

                        if ((!CheckAppRunning(FdsProcessName)) && ((File.Exists(encryptOutPutFile)) || (cnt == 0)))
                        {
                            // Start your WPF application
                            ProcessStartInfo startInfo = new ProcessStartInfo
                            {
                                FileName = exePath,
                                UseShellExecute = false,
                                CreateNoWindow = true,
                                Verb = "runas", // Request elevation
                                WindowStyle = ProcessWindowStyle.Hidden
                            };

                            Process wpfApp = Process.Start(startInfo);

                            IntPtr mainWindowHandle = wpfApp.MainWindowHandle;

                            if (mainWindowHandle != IntPtr.Zero)
                            {
                                // Hide the window of the launched process
                                ShowWindow(mainWindowHandle, SW_HIDE);
                            }


                            cnt++;

                            Thread.Sleep(TimeSpan.FromSeconds(10));

                            // Monitor the WPF application
                            while (!wpfApp.HasExited)
                            {

                                wpfApp.WaitForExit(1000);
                            }
                            // Restart the application if it's closed

                            // Wait for a moment before restarting
                            wpfApp.Dispose(); // Clean up the process                           
                        }

                    }
                }


            }
            catch (Exception ex)
            {
                WriteLog("Error3 " + ex.ToString());
                ex.ToString();
            }
        }

       
        static bool IsAnotherProcessRunning(string processName)
        {
            // Get all running processes with the specified name
            Process[] processes = Process.GetProcessesByName(processName);

            // Exclude the current process from the list
            Process currentProcess = Process.GetCurrentProcess();
            processes = Array.FindAll(processes, p => p.Id != currentProcess.Id);

            // Check if any other processes with the specified name are running
            return processes.Length > 0;
        }

        static string ReturnApplicationPath()
        {
            string applicationPath = "C:\\Fusion Data Secure\\FDS\\";
            try
            {

                string appPath1 = AppDomain.CurrentDomain.BaseDirectory;
                if (!string.IsNullOrEmpty(appPath1))
                {
                    applicationPath = appPath1;
                }


                if ((!string.IsNullOrEmpty(appPath1)) && (appPath1.Contains("LauncherApp")))
                {
                    // Get the current directory where the console app is running
                    string currentDirectory = Environment.CurrentDirectory;

                    // Navigate to the desired path from the current directory
                    string desiredPath = Path.Combine(currentDirectory, @"..\Desktop");
                    string originalPath = Path.GetFullPath(desiredPath);

                    string desiredPath1 = Path.Combine(Path.GetDirectoryName(originalPath), "..\\..\\Desktop\\bin\\Debug\\");
                    string fullPath = Path.GetFullPath(desiredPath1);

                    applicationPath = fullPath;
                }


                //string AutoStartBaseDir = applicationPath;

            }
            catch (Exception ex)
            {
                WriteLog("Error4 " + ex.ToString());
            }
            return applicationPath;
        }


        public static bool CheckAppRunning(string process)
        {
            var currentSessionID = Process.GetCurrentProcess().SessionId;
            return Process.GetProcessesByName(process).Where(p => p.SessionId == currentSessionID).Any();
        }

        public static bool IsAppRunning(string processName)
        {
            bool result = false;
            try
            {
                int bCnt = 0;

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
            }
            catch (Exception ex)
            {
                WriteLog("IsAppRunning2 " + ex.ToString());
            }

            return result;
        }

        public static string GetProcessOwner2(int processId)
        {
            try
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
            }
            catch (Exception ex)
            {

                WriteLog("GetOwner " + ex.ToString());
            }
            return null;
        }

        private static void WriteLog(string logMessage)
        {
            try
            {
                string path = AppDomain.CurrentDomain.BaseDirectory + "LancherLoger";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string filePath = Path.Combine(path, "LancherLoger_" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt");

                using (StreamWriter streamWriter = File.AppendText(filePath))
                {
                    streamWriter.WriteLine($"{DateTime.Now} - {logMessage}");
                }
            }
            catch (Exception ex)
            {
                WriteLog("WriteLog " + ex.ToString());
            }
        }

    }
}
