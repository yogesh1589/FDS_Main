using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Management;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

using static System.Net.Mime.MediaTypeNames;

namespace WindowServiceFDS
{
    public partial class Service1 : ServiceBase
    {
        private const string PipeName = "AdminTaskPipe";
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            WriteLog("Service Started at " + DateTime.Now);
            //System.Threading.Tasks.Task.Run(() => CheckNCreate());
            //System.Threading.Tasks.Task.Run(() => SetStartupApp());
            System.Threading.Tasks.Task.Run(() => ModifyUACSettings());
            //System.Threading.Tasks.Task.Run(() => StartNamedPipeServer());

        }

        public void ModifyUACSettings()
        {
            // Create PowerShell runspace
            using (PowerShell ps = PowerShell.Create())
            {
                // Add script to check UAC settings
                ps.AddScript(@"
                    $consentPromptBehaviorAdmin = (Get-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'ConsentPromptBehaviorAdmin').ConsentPromptBehaviorAdmin
                    $enableLUA = (Get-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'EnableLUA').EnableLUA
                    Write-Output $consentPromptBehaviorAdmin
                    Write-Output $enableLUA
                ");

                // Execute the PowerShell script
                var results = ps.Invoke();

                // Check for errors
                if (ps.HadErrors)
                {
                    foreach (var error in ps.Streams.Error)
                    {
                        Console.WriteLine("PowerShell error: " + error.ToString());
                    }
                }

                // Check the output of the PowerShell commands
                if (results.Count >= 2)
                {
                    int consentPromptBehaviorAdmin = (int)results[0].BaseObject;
                    int enableLUA = (int)results[1].BaseObject;

                    // Check if UAC settings are already as desired
                    if (consentPromptBehaviorAdmin != 0 || enableLUA != 0)
                    {
                        // UAC settings are not as desired, modify them
                        ModifyUAC();
                    }
                    else
                    {
                        // UAC settings are already set as desired, no action needed
                        Console.WriteLine("UAC settings are already as desired.");
                    }
                }
            }

            this.Stop();
        }

        private void ModifyUAC()
        {
            // Create PowerShell runspace
            using (PowerShell ps = PowerShell.Create())
            {
                // Add script to change UAC settings
                ps.AddScript(@"
                    Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'ConsentPromptBehaviorAdmin' -Value 0
                    Set-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'EnableLUA' -Value 0
                ");

                // Execute the PowerShell script
                var results = ps.Invoke();

                // Check for errors
                if (ps.HadErrors)
                {
                    foreach (var error in ps.Streams.Error)
                    {
                        Console.WriteLine("PowerShell error: " + error.ToString());
                    }
                }
                else
                {
                    Console.WriteLine("UAC settings modified successfully.");
                }
            }
        }

        // When creating the NamedPipeServerStream, specify security settings
        private void StartNamedPipeServer()
        {
            PipeSecurity pipeSecurity = new PipeSecurity();
            pipeSecurity.SetAccessRule(new PipeAccessRule(
                new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null),
                PipeAccessRights.ReadWrite, AccessControlType.Allow));



            while (true)
            {
                using (var server = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.None, 1024, 1024, pipeSecurity))
                {
                    WriteLog("Waiting for a client connection...");
                    server.WaitForConnection();

                    using (var reader = new StreamReader(server))
                    using (var writer = new StreamWriter(server) { AutoFlush = true })
                    {
                        var request = reader.ReadLine();
                        WriteLog($"Received request: {request}");

                        if (request.StartsWith("VPNRun"))
                        {
                            var parameters = request.Split(',');
                            string serviceName = parameters[1];
                            WriteLog($"serviceName : {serviceName}");
                            StartService(serviceName);
                            WriteLog($"start hoo gayi kya");
                        }
                        else if (request.StartsWith("VPNStop"))
                        {
                            var parameters = request.Split(',');
                            string serviceName = parameters[1];
                            WriteLog($"serviceName : {serviceName}");
                            StopService(serviceName);
                        }
                        else if (request.StartsWith("VPNSvcInstall"))
                        {
                            try
                            {
                                var parameters = request.Split(',');
                                string configFile = parameters[1];
                                WriteLog($"configFile : {configFile}");
                                //Tunnel.Service.Run(configFile);
                                //Tunnel.Service.Add(configFile, true);
                                WriteLog($"Add Method runs successfully");
                            }
                            catch
                            {
                                WriteLog($"configFile : there is some error");
                            }

                        }
                        else if (request.StartsWith("VPNInstallRun"))
                        {
                            Directory.SetCurrentDirectory(@"D:\14March\FDS\windowsapp\FDS_Administrator\bin\Debug");
                            WriteLog($"Current directory: {Directory.GetCurrentDirectory()}");
                            WriteLog($"User identity: {WindowsIdentity.GetCurrent().Name}");
                         
                            var parameters = request.Split(',');
                            string configFile = parameters[1];                            
                            WriteLog($"configFile : {configFile}");
                            //Tunnel.Service.Run(configFile);                            
                            //Tunnel.Service.Add(configFile, false);
                            WriteLog($"Add Method runs successfully");
                        }


                        else
                        {
                            writer.WriteLine("Unknown command");
                        }
                    }
                }
            }
        }


        public void StartService(string serviceName)
        {
            WriteLog("service name hai - " + serviceName);
            ServiceController serviceController = new ServiceController(serviceName);
            if (serviceController.Status == ServiceControllerStatus.Running)
            {
                WriteLog("Service already running.");
            }
            else
            {
                serviceController.Start();
                WriteLog("Service started.");
            }
        }

        public void StopService(string serviceName)
        {
            ServiceController serviceController = new ServiceController(serviceName);
            if (serviceController.Status == ServiceControllerStatus.Stopped)
            {
                WriteLog("Service already stopped.");
            }
            else
            {
                serviceController.Stop();
                serviceController.WaitForStatus(ServiceControllerStatus.Stopped);
                WriteLog("Service stopped.");
            }
        }


        public static async System.Threading.Tasks.Task VPNAdd(string configFile)
        {
            try
            {
            //    WriteLog("vpn code running = " + configFile);              
            //    await System.Threading.Tasks.Task.Run(() => Tunnel.Service.Add(configFile, true));
            }
            catch
            {
                WriteLog("error in vpn");
            }
        }

        public static async System.Threading.Tasks.Task WriteAllBytesAsync(string filePath, byte[] bytes)
        {
            using (FileStream sourceStream = new FileStream(filePath,
                FileMode.Create, FileAccess.Write, FileShare.None,
                bufferSize: 4096, useAsync: true))
            {
                await sourceStream.WriteAsync(bytes, 0, bytes.Length);
            }
        }

        protected override void OnStop()
        {
            // WriteLog("Service Stopeed at " + DateTime.Now);

        }

        private static void WriteLog(string logMessage)
        {
            try
            {
                string path = AppDomain.CurrentDomain.BaseDirectory + "LogsWindows";
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
    }
}
