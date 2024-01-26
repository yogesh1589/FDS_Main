using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoUpdate
{
    public class Program
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;  // Hides the window
        const int SW_SHOW = 5;  // Shows the window

        const string baseTempFileDir = "Tempfolder";

        public static bool isAdmin = false;




        public static void Main(string[] args)
        {
            //Hide the console window
            IntPtr hWnd = GetConsoleWindow();
            if (hWnd != IntPtr.Zero)
            {
                ShowWindow(hWnd, SW_HIDE);
            }

            string TempFDSPath = "C:\\web\\Temp\\FDS\\";
            Console.WriteLine("Hi! you are about to update your FDS application");
            string installationPath = "C:\\Fusion Data Secure\\FDS";
            //string installationPath2 = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(new[] { '\\' }).ToString();
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
            if (registryKey != null)
            {
                object obj = registryKey.GetValue("FDS");
                if (obj != null)
                    installationPath = Path.GetDirectoryName(obj.ToString());
            }

            Array.ForEach(Process.GetProcessesByName("FDS"), x => x.Kill());

            try
            {
                if (IsProcessOpen("LauncherApp"))
                {
                    Array.ForEach(Process.GetProcessesByName("LauncherApp"), x => x.Kill());
                    isAdmin = true;
                }
            }
            catch (Exception ex)
            {
                isAdmin = false;
                ex.ToString();
            }

            DeleteDirectoryContents(TempFDSPath, installationPath + "\\");
        }



        static bool IsProcessOpen(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            return processes.Length > 0;
        }



        public static void DeleteDirectoryContents(string sourcePath, string directoryPath)
        {
            try
            {

                // Get the directory info
                DirectoryInfo directoryInfo = new DirectoryInfo(directoryPath);



                // Delete all files within the directory
                foreach (FileInfo file in directoryInfo.GetFiles())
                {
                    Thread.Sleep(5);
                    try
                    {
                        if ((file.Name == "LauncherApp.exe" && !isAdmin) || (file.Name == "FDS_Administrator.exe"))
                        {
                        }
                        else { file.Delete(); }
                    }
                    catch (Exception ex)
                    {
                        ex.ToString();
                        WriteLog(" error in file deletion = " + ex.ToString());
                    }

                    //Console.WriteLine($"{file.Name} deleted from the installation path");

                    //Console.WriteLine(file + " Files Deleted from installation path");                    
                }


                // Delete all subdirectories and their contents
                foreach (DirectoryInfo subdirectory in directoryInfo.GetDirectories())
                {
                    if (!subdirectory.Name.Contains(baseTempFileDir))
                    {
                        subdirectory.Delete(true);
                    }
                }


                //Hide the console window
                IntPtr hWnd = GetConsoleWindow();
                if (hWnd != IntPtr.Zero)
                {
                    ShowWindow(hWnd, SW_HIDE);
                }
                //Console.WriteLine("Files Deleted from installation path");
                //Thread.Sleep(2000);

                Console.WriteLine("Start Files Extracted to installation path");
                ExtractMSIContent(sourcePath + "FDS.msi", directoryPath);

                Thread.Sleep(20000);
                //Console.WriteLine("Files Extracted to installation path");

                string AutoUpdateExePath = string.Empty;
                if (File.Exists(directoryPath + "LauncherApp.exe"))
                {
                    AutoUpdateExePath = directoryPath + "LauncherApp.exe";
                }
                else
                {
                    AutoUpdateExePath = directoryPath + "FDS.exe";
                }

                //StartService("FDSWatchDog");
                //StopRemoveStartupApplication(directoryPath, "LauncherApp.exe");
                //Console.WriteLine("start FDS from " + AutoUpdateExePath);
                try
                {
                    if (!IsProcessOpen("LauncherApp"))
                    {
                        Process.Start(AutoUpdateExePath);
                    }

                }
                catch
                {
                    WriteLog("LauncherApp Catch error running");
                }

            }
            catch (Exception ex)
            {
                WriteLog("An error occurred while deleting directory contents: " + ex.Message);
                Console.WriteLine("An error occurred while deleting directory contents: " + ex.Message);
                Console.ReadLine();
            }
        }


        public static void SendCommandToService(string command)
        {

            string AutoStartBaseDir = "C:\\Fusion Data Secure\\FDS\\";
            string exeFile1 = Path.Combine(AutoStartBaseDir, "FDS_Administrator.exe");

            string arguments = command; // Replace this with your desired arguments

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = exeFile1,
                Verb = "runas", // Run as administrator if needed
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = arguments // Pass arguments here
            };


            try
            {
                Process.Start(psi);
            }
            catch
            {
                // Handle any exception due to user declining the UAC prompt
                Console.WriteLine("User declined UAC prompt or didn't have necessary privileges.");
            }
        }

        public static void ExtractMSIContent(string msiFilePath, string outputDirectory)
        {
            //Hide the console window
            IntPtr hWnd = GetConsoleWindow();
            if (hWnd != IntPtr.Zero)
            {
                ShowWindow(hWnd, SW_HIDE);
            }

            SendCommandToService("AutoUpdate");

            //Process process = new Process();
            //process.StartInfo.FileName = "msiexec";
            //process.StartInfo.Arguments = $"/a \"{msiFilePath}\" /qn TARGETDIR=\"{outputDirectory}\"";
            //process.StartInfo.UseShellExecute = false;
            //process.StartInfo.RedirectStandardOutput = true;
            //process.StartInfo.RedirectStandardError = true;
            //process.StartInfo.CreateNoWindow = true;

            //try
            //{
            //    process.Start();
            //    Thread.Sleep(5000);
            //    //process.BeginOutputReadLine();

            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine("An error occurred: " + ex.Message);
            //}
            //finally
            //{
            //    process.Close();

            //}
        }




        public static void WriteLog(string logMessage)
        {
            try
            {
                string path = AppDomain.CurrentDomain.BaseDirectory + "AutoUpdate";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string filePath = Path.Combine(path, "AutoUpdate_" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt");

                using (StreamWriter streamWriter = File.AppendText(filePath))
                {
                    streamWriter.WriteLine($"{DateTime.Now} - {logMessage}");
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}
