using FDS_Administrator.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tunnel;

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
        private const string LongName = "FDS VPN";
        private const string Description = "Demonstration tunnel for testing WireGuard";

      
        
        static void Main(string[] args)
        {
            if (args.Length >= 2)
            {
                string configFile = args[0];
                WriteLog("config path ye hai " + configFile);
                string methodName = args[1];
                WriteLog("methodName ye hai " + methodName);
                // Execute the desired method based on the methodName
                switch (methodName)
                {
                    case "vpncallAsync":
                        vpncallAsync(configFile);
                        break;
                    // Add other cases for different methods
                    default:
                        Console.WriteLine("Invalid method name");
                        break;
                }
            }
            else
            {
                Console.WriteLine("Insufficient arguments passed.");
            }
        }

    


        static void vpncallAsync(string configFile)
        {
            WriteLog("Running tunnel");
            Tunnel.Service.Run(configFile);
            WriteLog("Running tunnel completed");

            WriteLog("Running tunnel Add");
            Tunnel.Service.Add(configFile, false);

            WriteLog("Running tunnel Add completed");
        }

        private const string PipeName = "AdminTaskPipe";
        static void SendRequest(string request)
        {
            WriteLog("request going");
            try
            {
                using (var client = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut))
                {
                    client.Connect();
                    using (var writer = new StreamWriter(client) { AutoFlush = true })
                    using (var reader = new StreamReader(client))
                    {
                        WriteLog("request connected");
                        writer.WriteLine(request);
                        string response = reader.ReadLine();
                        Console.WriteLine($"Service response: {response}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }


        public static async Task WriteAllBytesAsync(string filePath, byte[] bytes)
        {
            using (FileStream sourceStream = new FileStream(filePath,
                FileMode.Create, FileAccess.Write, FileShare.None,
                bufferSize: 4096, useAsync: true))
            {
                await sourceStream.WriteAsync(bytes, 0, bytes.Length);
            }
        }




        private static void WriteLog(string logMessage)
        {
            try
            {
                string path = AppDomain.CurrentDomain.BaseDirectory + "AdminAppLogs";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string filePath = Path.Combine(path, "AdminLogs_" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt");

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
