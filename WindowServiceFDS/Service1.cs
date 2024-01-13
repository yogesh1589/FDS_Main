using Microsoft.Win32;
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
        private NamedPipeServerStream pipeServer;
        private bool isRunning = false;
        public string pipName = @"\\.\pipe\AdminPipes";
        private string processName = "C:\\Fusion Data Secure\\FDS\\LauncherApp.exe";
        private System.Timers.Timer timer;

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            //WriteLog("Service Started at " + DateTime.Now);

            //System.Threading.Tasks.Task.Run(() => StartPipeServer());

            //System.Threading.Tasks.Task.Run(() => InitializeTimer());

            System.Threading.Tasks.Task.Run(() => SetStartupApp());
            System.Threading.Tasks.Task.Run(() => ModifyUACSettings());

        }

        public void SetStartupApp()
        {
            //WriteLog("Set startup");
            string applicationPath = "C:\\Fusion Data Secure\\FDS\\";
            DeleteAppIfExists();
            //WriteLog("deleted");

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                if (key != null)
                {                     
                    string exeFile = Path.Combine(applicationPath, "LauncherApp.exe");

                    key.SetValue("FDS", $"\"{exeFile}\" --opened-at-login --minimize");
                    //Console.WriteLine("Application added to startup successfully.");
                    //string value = ReadRegistryValue(Registry.LocalMachine, "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", "FDS");
                    //WriteLog("path value = " + value);
                    //WriteLog("Application added to startup successfully");
                    // Start the application
                    //Process.Start(exeFile);
                    //Console.WriteLine("Application started.");
                }
                else
                {
                    WriteLog("Key not found");
                    Console.WriteLine("Registry key not found.");
                }
            }
        }


        public void DeleteStartupEntry(RegistryKey registryKey, string entryName)
        {
            try
            {
                registryKey.DeleteValue(entryName);
                Console.WriteLine($"Deleted startup entry: {entryName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting startup entry: {ex.Message}");
            }
        }
        public string ReadRegistryValue(RegistryKey baseKey, string keyPath, string valueName)
        {
            try
            {
                using (RegistryKey key = baseKey.OpenSubKey(keyPath))
                {
                    if (key != null)
                    {
                        // Read the value from the registry
                        object value = key.GetValue(valueName);

                        // Check if the value exists
                        if (value != null)
                        {
                            return value.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading registry value: {ex.Message}");
            }

            return null;
        }
        public void DeleteAppIfExists()
        {
            RegistryKey currentUserKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);

            if (currentUserKey != null)
            {
                Console.WriteLine("Current User Startup Entries:");

                // Iterate through the startup entries and display them
                foreach (string valueName in currentUserKey.GetValueNames())
                {
                    Console.WriteLine($"{valueName}: {currentUserKey.GetValue(valueName)}");
                    if ((valueName == "FDS") || (valueName.Contains("LauncherApp")))
                    {
                        DeleteStartupEntry(currentUserKey, valueName);
                    }
                }

                // Example: Delete a startup entry by name
                // DeleteStartupEntry(currentUserKey, "EntryNameToDelete");

                currentUserKey.Close();
            }
            else
            {
                Console.WriteLine("Unable to access current user's startup entries.");
            }

            // Specify the registry key for all users' startup entries
            RegistryKey localMachineKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

            if (localMachineKey != null)
            {
                Console.WriteLine("\nAll Users Startup Entries:");

                // Iterate through the startup entries and display them
                foreach (string valueName in localMachineKey.GetValueNames())
                {
                    Console.WriteLine($"{valueName}: {localMachineKey.GetValue(valueName)}");
                    if ((valueName == "FDS") || (valueName.Contains("LauncherApp")))
                    {
                        DeleteStartupEntry(localMachineKey, valueName);
                    }
                }

                // Example: Delete a startup entry by name
                // DeleteStartupEntry(localMachineKey, "EntryNameToDelete");

                localMachineKey.Close();
            }
            else
            {
                Console.WriteLine("Unable to access all users' startup entries.");
            }
        }

        private void InitializeTimer()
        {
            timer = new System.Timers.Timer();
            timer.Elapsed += new ElapsedEventHandler(OnTimerElapsed);
            timer.Interval = 10000; // 10 seconds in milliseconds
            timer.Enabled = true;
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


        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            CheckAndRunProcessLauncher();
        }

        private void CheckAndRunProcessLauncher()
        {
            WriteLog("Checking Launcher App");
            if (!IsProcessRunningForCurrentUser(processName))
            {
                WriteLog($"{processName} is not running for the current user. Starting {processName}...");

                // Start the process
                StartProcess(processName);
            }
            else
            {
                //WriteLog($"{processName} is already running for the current user.");
            }
        }

        private bool IsProcessRunningForCurrentUser(string processName)
        {
            int bCnt = 0;
            WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent();
            string currentUser = currentIdentity.Name.Split('\\')[1]; // Get current user name

            string exeFileName = System.IO.Path.GetFileNameWithoutExtension(processName);
            Process[] processes = Process.GetProcessesByName(exeFileName);

            foreach (Process process in processes)
            {
                try
                {
                    string processOwner = GetProcessOwner(process.Id);
                    if (!string.IsNullOrEmpty(processOwner))
                    {

                        if (System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToUpper().ToString().Contains(processOwner.ToUpper().ToString()))
                        {
                            bCnt++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteLog($"Error getting process owner: {ex.Message}");
                }
            }
            WriteLog("Count = " + bCnt);
            if (bCnt > 0)
            {
                return true;
            }

            return false; // Process is not running for the current user
        }

        private string GetProcessOwner(int processId)
        {
            string query = $"SELECT * FROM Win32_Process WHERE ProcessId = {processId}";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();

            foreach (ManagementObject obj in processList)
            {
                string[] argList = new string[] { string.Empty, string.Empty };
                int returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));

                if (returnVal == 0)
                {
                    return $"{argList[1]}\\{argList[0]}";
                }
            }

            throw new Exception("Unable to determine process owner.");
        }

        private void StartProcess(string processName)
        {
            try
            {
                WriteLog(processName);
                Process process = new Process();
                process.StartInfo.FileName = processName;
                // Set environment variables if necessary
                process.StartInfo.Verb = "runas";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden; // Set the window style to hidden                                                                       //startInfo.Verb = "runas";
                process.Start();
            }
            catch (Exception ex)
            {
                WriteLog($"Error starting process: {ex.Message}");
            }
        }

        private void StartPipeServer()
        {

            isRunning = true;

            //WriteLog("Start pipe server singh " + DateTime.Now);
            try
            {
                using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(pipName, PipeDirection.In))
                {
                    //WriteLog("Checking pipe connection 2");
                    while (true)
                    {
                        //WriteLog("Waiting for connection");
                        pipeServer.WaitForConnection();
                        //WriteLog("connection set");
                        // Read command from the service
                        StreamReader reader = new StreamReader(pipeServer);
                        //WriteLog("connection reader");
                        string command = reader.ReadToEnd();

                        //WriteLog("command " + command);

                        if (command == "WindowsRegistryProtection")
                        {
                            int totalCount = 0;
                            totalCount = DeleteRegistriesKey();
                            //WriteLog("Total = " + totalCount);
                        }
                        else if (command == "StopSignal")
                        {

                        }

                        pipeServer.Disconnect();
                    }
                }
            }
            catch (Exception ex)
            {
                //WriteLog("Error pipeline = " + ex.ToString());
            }
        }

        public int DeleteRegistriesKey()
        {
            //WriteLog("Starts Method try new");
            int totalCount = 0;
            try
            {
                //WriteLog("Starts Method New");

                int localMachineCount = DeleteLocalMachineCount();
                //WriteLog("localMachineCount " + localMachineCount);

                string AutoStartBaseDir = GetApplicationpath();

                //WriteLog("path = " + AutoStartBaseDir);

                string resultFilePath = Path.Combine(AutoStartBaseDir, "result.txt");

                totalCount = localMachineCount;

                File.WriteAllText(resultFilePath, totalCount.ToString());
            }
            catch (Exception)
            {
                //WriteLog("Starts Method Error");
                return totalCount;
            }
            return totalCount;
        }


        public static string GetApplicationpath()
        {
            string applicationPath = "C:\\Fusion Data Secure\\FDS\\";
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
            if (registryKey != null)
            {
                object obj = registryKey.GetValue("FDS");
                if (obj != null)
                    return Path.GetDirectoryName(obj.ToString());
            }

            if (string.IsNullOrEmpty(applicationPath))
            {
                string currentPath = System.Reflection.Assembly.GetEntryAssembly()?.Location;
                applicationPath = Path.GetDirectoryName(currentPath);
            }
            return applicationPath;
        }
        public int DeleteLocalMachineCount()
        {
            int localMachineCount = 0;

            //WriteLog("DeleteLocalMachineCount");

            try
            {
                RegistryKey localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey("SOFTWARE", true);

                if (localMachine != null)
                {
                    //WriteLog("Access rule set");

                    localMachineCount = TotalCountDeleted(localMachine);
                }
                else
                {
                    //WriteLog("Registry key not found");
                    Console.WriteLine("Registry key not found.");
                }

                Console.WriteLine("Total LM Count: " + localMachineCount);

            }
            catch (Exception ex)
            {
                //WriteLog("error " + ex.ToString());
                Console.WriteLine("Error: " + ex.Message);
            }

            return localMachineCount;
        }

        public int TotalCountDeleted(RegistryKey CUkey)
        {
            int cntDeleted = 0;

            try
            {
                foreach (string subkeyName in CUkey.GetSubKeyNames())
                {


                    RegistryKey subkey = CUkey.OpenSubKey(subkeyName);
                    if (subkey.ValueCount == 0 && subkey.SubKeyCount == 0)
                    {
                        // If the subkey does not contain any values, delete it
                        CUkey.DeleteSubKeyTree(subkeyName);
                        cntDeleted++;
                    }
                    else
                    {
                        // If the subkey contains values, check if they are valid
                        foreach (string valueName in subkey.GetValueNames())
                        {
                            object value = subkey.GetValue(valueName);
                            // Check if the value is invalid or obsolete
                            if (value == null || value.ToString().Contains("[obsolete]"))
                            {
                                subkey.DeleteValue(valueName);
                                cntDeleted++;
                            }
                        }
                    }
                }
            }
            catch
            { }

            return cntDeleted;
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
