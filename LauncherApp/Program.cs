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


                string applicationPath = ReturnApplicationPath();

                string exePath = Path.Combine(applicationPath, "FDS.exe");

                if (!string.IsNullOrEmpty(exePath))
                {
                    SetStartupApp(applicationPath);

                    while (true)
                    {
                        if ((!IsAppRunning(FdsProcessName)) && ((File.Exists(encryptOutPutFile)) || (cnt == 0)))
                        {
                            //WriteLog("App is running1");
                            // Start your WPF application
                            ProcessStartInfo startInfo = new ProcessStartInfo
                            {
                                FileName = exePath,
                                UseShellExecute = false,
                                CreateNoWindow = true,
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
                            // Monitor the WPF application
                            while (!wpfApp.HasExited)
                            {
                                //WriteLog("App Restarted");
                                wpfApp.WaitForExit(1000);
                            }
                            // Restart the application if it's closed
                            Console.WriteLine("Application closed. Restarting...");
                            Thread.Sleep(1000); // Wait for a moment before restarting
                            wpfApp.Dispose(); // Clean up the process                           
                        }
                        Thread.Sleep(TimeSpan.FromSeconds(10));
                    }
                }


            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }


        public static void SetStartupApp(string applicationPath)
        {
            try
            {

                // applicationPath =  Path.Combine(applicationPath, "LauncherApp.exe");

                RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
                if (registryKey != null)
                {
                    object obj = registryKey.GetValue("LauncherApp1");
                    if (obj != null)
                        applicationPath = Path.GetDirectoryName(obj.ToString());
                }

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    if (key != null)
                    {
                        string exeFile = Path.Combine(applicationPath, "LauncherApp.exe");

                        // Check if the application is already in startup
                        if (key.GetValue("LauncherApp1") == null)
                        {
                            // If not, add it to startup
                            key.SetValue("LauncherApp1", $"\"{exeFile}\" --opened-at-login --minimize");
                            Console.WriteLine("Application added to startup successfully.");
                        }
                        else
                        {
                            Console.WriteLine("Application already set to start on login.");
                        }

                        // Start the application
                        Process.Start(exeFile);
                        Console.WriteLine("Application started.");
                    }
                    else
                    {
                        Console.WriteLine("Registry key not found.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
        static string ReturnApplicationPath()
        {
            string applicationPath = "C:\\Fusion Data Secure\\FDS\\";
            try
            {
                RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall");
                if (registryKey != null)
                {
                    object obj = registryKey.GetValue("FDS");
                    if (obj != null)
                        applicationPath = Path.GetDirectoryName(obj.ToString());
                }
                if (string.IsNullOrEmpty(applicationPath))
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
            catch
            {

            }
            return applicationPath;
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

                WriteLog("IsAppRunning " + ex.ToString());
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

                string filePath = Path.Combine(path, "ServiceLog_" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt");

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
