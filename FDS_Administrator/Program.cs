﻿using FDS_Administrator.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FDS_Administrator
{
    internal class Program
    {


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


        public static string pipName = @"\\.\pipe\AdminPipes";
        static void Main(string[] args)
        {
 
            
            // Check for command-line arguments
            if (args.Length > 0)
            {
                //string abc = "Certificates,f769bb0beebd2f03f3ba26157251a657e39a01a5," + StoreLocation.CurrentUser + "," + StoreName.My;
                //string abc = "WindowsRegistryProtection";
                //string[] parameters = abc.Split(',');

                string[] parameters = args[0].Split(',');
                string methodName = parameters[0];
                Console.WriteLine(methodName);
                // Determine which method to call based on the argument
                switch (methodName)
                {
                    case "AutoUpdate":
                        Method1(args); // Call Method1 and pass additional arguments
                        break;
                    case "WindowsRegistryProtection":
                        Method2(args); // Call Method2 and pass additional arguments
                        break;
                    // Add more cases for other methods if needed
                    default:
                        Console.WriteLine("Invalid method name");
                        break;
                }
            }
            else
            {
                Console.WriteLine("No method specified");
            }
        }

        static void Method1(string[] args)
        {
            //DeleteCertificates deleteCertificates = new DeleteCertificates();
            //deleteCertificates.Delete(args);
            string msiFilePath = "C:\\web\\Temp\\FDS\\FDS.msi";

            if (File.Exists(msiFilePath))
            {

                string outputDirectory = "C:\\Fusion Data Secure\\FDS\\";
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
                    Thread.Sleep(10000);
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


        static void Method2(string[] args)
        {
            WindowsRegistryProtection windowsRegistryProtection = new WindowsRegistryProtection();
            windowsRegistryProtection.DeleteRegistriesKey();
        }

        //private static void WriteLog(string logMessage)
        //{
        //    try
        //    {
        //        string path = AppDomain.CurrentDomain.BaseDirectory + "AdminAppLogs";
        //        if (!Directory.Exists(path))
        //        {
        //            Directory.CreateDirectory(path);
        //        }

        //        string filePath = Path.Combine(path, "AdminLogs_" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt");

        //        using (StreamWriter streamWriter = File.AppendText(filePath))
        //        {
        //            streamWriter.WriteLine($"{DateTime.Now} - {logMessage}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        WriteLog("WriteLog " + ex.ToString());
        //    }
        //}
    }
}
