using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
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
            string installationPath = string.Empty;
            //string installationPath2 = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(new[] { '\\' }).ToString();
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
            if (registryKey != null)
            {
                object obj = registryKey.GetValue("FDS");
                if (obj != null)
                    installationPath = Path.GetDirectoryName(obj.ToString());
            }
            Array.ForEach(Process.GetProcessesByName("FDS"), x => x.Kill());
            DeleteDirectoryContents(TempFDSPath, installationPath + "\\");
        }
        public static void DeleteDirectoryContents(string sourcePath, string directoryPath)
        {
            try
            {

                Console.WriteLine(directoryPath);
                Thread.Sleep(2000);
                // Get the directory info
                DirectoryInfo directoryInfo = new DirectoryInfo(directoryPath);

                // Delete all files within the directory
                foreach (FileInfo file in directoryInfo.GetFiles())
                {
                    Thread.Sleep(10);
                    //Console.WriteLine(file+ " Files Deleted from installation path");
                    file.Delete();
                }

                // Delete all subdirectories and their contents
                foreach (DirectoryInfo subdirectory in directoryInfo.GetDirectories())
                {
                    if(!subdirectory.Name.Contains(baseTempFileDir))
                    {
                        subdirectory.Delete(true);
                    }
                    
                }
                //Hide the console window
                //IntPtr hWnd = GetConsoleWindow();
                //if (hWnd != IntPtr.Zero)
                //{
                //    ShowWindow(hWnd, SW_HIDE);
                //}
                Console.WriteLine("Files Deleted from installation path");
                Thread.Sleep(2000);
                Console.WriteLine("Start Files Extracted to installation path");
                ExtractMSIContent(sourcePath + "FDS.msi", directoryPath);
                Thread.Sleep(2000);
                Console.WriteLine("Files Extracted to installation path");
                string AutoUpdateExePath = directoryPath + "FDS.exe";
                Console.WriteLine("start FDS from " + AutoUpdateExePath);
                Process.Start(AutoUpdateExePath);
               
                
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while deleting directory contents: " + ex.Message);
            }
        }
        public static void ExtractMSIContent(string msiFilePath, string outputDirectory)
        {
            //Hide the console window
            //IntPtr hWnd = GetConsoleWindow();
            //if (hWnd != IntPtr.Zero)
            //{
            //    ShowWindow(hWnd, SW_HIDE);
            //}
            Process process = new Process();
            process.StartInfo.FileName = "msiexec";
            process.StartInfo.Arguments = $"/a \"{msiFilePath}\" /qn TARGETDIR=\"{outputDirectory}\"";
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
                Console.WriteLine("An error occurred: " + ex.Message);
            }
            finally
            {
                process.Close();
                
            }
        }
    }
}
