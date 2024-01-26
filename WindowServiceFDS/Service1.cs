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
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            //WriteLog("Service Started at " + DateTime.Now);
            //System.Threading.Tasks.Task.Run(() => CheckNCreate());
            //System.Threading.Tasks.Task.Run(() => SetStartupApp());
            System.Threading.Tasks.Task.Run(() => ModifyUACSettings());

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

        protected override void OnStop()
        {
            // WriteLog("Service Stopeed at " + DateTime.Now);

        }

        private void WriteLog(string logMessage)
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
