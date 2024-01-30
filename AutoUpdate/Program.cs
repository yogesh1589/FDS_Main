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
            //IntPtr hWnd = GetConsoleWindow();
            //if (hWnd != IntPtr.Zero)
            //{
            //    ShowWindow(hWnd, SW_HIDE);
            //}

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

            if (IsAppRunning("FDS"))
            {
                WriteLog("App was running");
            }

            // Array.ForEach(Process.GetProcessesByName("FDS"), x => x.Kill());

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
                WriteLog("Trying to close Launcher but error");
                isAdmin = false;
                ex.ToString();
            }

            if (string.IsNullOrEmpty(installationPath))
            {
                installationPath = "C:\\Fusion Data Secure\\FDS";
            }

            DeleteDirectoryContents(TempFDSPath, installationPath + "\\");
        }


        static int CountProcesses(string processName)
        {
            int processCount = 0;

            Process[] processes = Process.GetProcesses();

            foreach (Process process in processes)
            {
                if (process.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase))
                {
                    processCount++;
                }
            }

            return processCount;
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

        public static bool IsAppRunning(string processName)
        {
            int bCnt = 0;
            bool result = false;
            string exeFileName = System.IO.Path.GetFileNameWithoutExtension(processName);
            Process[] chromeProcesses = Process.GetProcessesByName(exeFileName);
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
                        try
                        {
                            process.Kill();
                            WriteLog("Process " + process.ProcessName + " (ID: " + process.Id + ") closed successfully.");
                        }
                        catch (Exception ex)
                        {
                            WriteLog("Error closing process: " + ex.Message);
                        }
                    }
                }
            }

            if (bCnt > 0)
            {
                WriteLog("Process Count = " + bCnt);
                result = true;
            }
            return result;
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
                //if(string.IsNullOrEmpty(directoryPath))
                //{

                //}

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
                        WriteLog(" error in file deletion = " + file.Name.ToString());
                    }

                    //Console.WriteLine($"{file.Name} deleted from the installation path");

                    //Console.WriteLine(file + " Files Deleted from installation path");                    
                }


                // Delete all subdirectories and their contents

                foreach (DirectoryInfo subdirectory in directoryInfo.GetDirectories())
                {
                    try
                    {
                        if (!subdirectory.Name.Contains(baseTempFileDir))
                        {
                            subdirectory.Delete(true);
                        }
                    }
                    catch
                    {
                        WriteLog("Error in folder deletion = " + subdirectory.Name.ToString());
                    }
                }



                //Hide the console window
                //IntPtr hWnd = GetConsoleWindow();
                //if (hWnd != IntPtr.Zero)
                //{
                //    ShowWindow(hWnd, SW_HIDE);
                //}
                //Console.WriteLine("Files Deleted from installation path");
                //Thread.Sleep(2000);

                Console.WriteLine("Start Files Extracted to installation path");

                ExtractMSIContent(sourcePath + "FDS.msi", directoryPath);

                Thread.Sleep(20000);
                //Console.WriteLine("Files Extracted to installation path");


                string AutoUpdateExePath = string.Empty;
                string FDSpath = directoryPath + "FDS.exe";

                

                if (File.Exists(directoryPath + "LauncherApp.exe"))
                {
                    AutoUpdateExePath = directoryPath + "LauncherApp.exe";
                }
                else
                {
                    AutoUpdateExePath = directoryPath + "FDS.exe";
                }
                
                try
                {
                    if (!IsProcessOpen("LauncherApp"))
                    {
                       // WriteLog("Start = " + AutoUpdateExePath);
                        Process.Start(AutoUpdateExePath);
                    }
                    else
                    {
                        //WriteLog("Start = " + FDSpath);
                        Process.Start(FDSpath);
                    }

                }
                catch (Exception ex)
                {
                    WriteLog("LauncherApp Catch error running " + ex.Message.ToString());
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
                //WindowStyle = ProcessWindowStyle.Hidden,
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

            //SendCommandToService("AutoUpdate");

            string path = AppDomain.CurrentDomain.BaseDirectory + "AutoUpdate\\";
            //string path = "C:\\web\\Temp\\FDS\\AutoUpdate\\";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            Process process = new Process();
            process.StartInfo.FileName = "msiexec";
            process.StartInfo.Arguments = $"/a \"{msiFilePath}\" /qn TARGETDIR=\"{path}\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            try
            {
                process.Start();
                Thread.Sleep(5000);
                //process.BeginOutputReadLine();

            }
            catch (Exception ex)
            {
                WriteLog("Error in Extracting");
                Console.WriteLine("An error occurred: " + ex.Message);
            }
            finally
            {
                process.Close();
            }

            //WriteLog("Source =" + path);
            //WriteLog("Destination =" + outputDirectory);

            CopyDirectory(path, outputDirectory);

        }



        static void CopyDirectory(string sourceDir, string destDir)
        {
            try
            {

           
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            // Copy files
            foreach (string filePath in Directory.GetFiles(sourceDir))
            {
                try
                {
                    string fileName = Path.GetFileName(filePath);
                    string destFilePath = Path.Combine(destDir, fileName);

                    File.Copy(filePath, destFilePath, true); // Set 'true' to overwrite existing files
                    System.Threading.Thread.Sleep(100);
                    File.Delete(filePath);
                }
                catch
                {
                    WriteLog("error in copy file " + filePath);
                }
            }

            // Copy subdirectories
            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                try
                {
                    string subDirName = Path.GetFileName(subDir);
                    string destSubDir = Path.Combine(destDir, subDirName);
                    CopyDirectory(subDir, destSubDir);
                    System.Threading.Thread.Sleep(100);
                    Directory.Delete(subDir);
                }
                catch { WriteLog("error in folder " + subDir); }
            }

            }
            catch (Exception)
            {
                WriteLog("Eception in copying file");                
            }
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
