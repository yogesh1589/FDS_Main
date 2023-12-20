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
        const int SW_HIDE = 0; // Hides the window
        const int SW_SHOW = 5; // Shows the window

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
            //// Get the handle to the console window
            //IntPtr hWndConsole = GetConsoleWindow();

            ////// Hide the console window
            //ShowWindow(hWndConsole, SW_HIDE);

            //Thread.Sleep(TimeSpan.FromSeconds(30));

            //HideConsoleWindow();

            WriteLog("Strt h sa navi sa");

            try
            {
                Thread continuousThread = new Thread(RunContinuously);
                continuousThread.IsBackground = true; // Set as a background thread (will not prevent the application from exiting)
                continuousThread.Start();

                // Keep the program running indefinitely
                // This main thread will not wait and will continue executing other instructions
                // For demonstration purposes, there's no additional logic here
                // The continuous thread will keep running until the application is manually closed
                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception ex)
            {

                WriteLog("Main " + ex.ToString());
            }
            // Start a new thread to run the method continuously




            //while (true)
            //{
            //    try
            //    {
            //        WriteLog("Checking condition method");
            //        CheckAndStartProcess().Wait(); // Wait for the asynchronous task to complete
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine($"An exception occurred: {ex.Message}");
            //        // Handle the exception or simply continue the loop
            //    }
            //    Thread.Sleep(TimeSpan.FromSeconds(10));
            //}

            //timer.Dispose(); // Dispose the timer when exiting
        }

        static void RunContinuously()
        {
            try
            {
                while (true)
                {
                    // Your continuous logic goes here
                    Console.WriteLine("Running continuously...");

                    WriteLog("Checking condition method");
                    CheckAndStartProcess().Wait(); // Wait for the asynchronous task to complete

                    // Add a delay to prevent high CPU usage
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
            }
            catch (Exception ex)
            {

                WriteLog("RunContinuously " + ex.ToString());
            }
        }

        static void RestartConsoleApplication(string appName)
        {
            try
            {

                Process process = new Process();
                // Start a new instance of the application and exit the current instance
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.FileName = appName;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                Environment.Exit(0);
            }
            catch (Exception ex)
            {

                WriteLog("RestartConsoleApplication " + ex.ToString());
            }
        }

        static async Task CheckAndStartProcess()
        {
            try
            {
                if (!IsAppRunning(FdsProcessName))
                {
                    WriteLog("Opening FDS");
                    await Task.Run(() => StartUIApplication()); // Start UI asynchronously
                }
                else
                {
                    WriteLog("Already running");
                    Thread.Sleep(TimeSpan.FromSeconds(10));
                    //string appName = Process.GetCurrentProcess().ProcessName;
                    //WriteLog("Restarting");
                    //RestartConsoleApplication(appName);
                    Console.WriteLine("fds.exe is already running.");
                }

                if (DateTime.Now.Second % 10 == 0)
                {
                    WriteLog("Condition is true, but the loop continues.");
                    Console.WriteLine("Condition is true, but the loop continues.");
                }
            }
            catch (Exception ex)
            {

                WriteLog("CheckAndStartProcess " + ex.ToString());
            }
        }


        private static void StartUIApplication()
        {
            try
            {

                string applicationPath = "C:\\Fusion Data Secure\\FDS\\";
                //RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
                //if (registryKey != null)
                //{
                //    object obj = registryKey.GetValue("FDS");
                //    if (obj != null)
                //        applicationPath = Path.GetDirectoryName(obj.ToString());
                //}
                //if (string.IsNullOrEmpty(applicationPath))
                //{
                //    applicationPath = "C:\\Fusion Data Secure\\FDS\\";
                //}
                string exeFile = Path.Combine(applicationPath, "FDS.exe");
                WriteLog("UI is opening from " + exeFile);
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
                WriteLog("StartUIApplication " + ex.ToString());
                Console.WriteLine($"Error starting UI application: {ex.Message}");
            }
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
                string path = AppDomain.CurrentDomain.BaseDirectory + "LancherLogs";
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
                WriteLog( "WriteLog " + ex.ToString());
            }
        }

    }
}
