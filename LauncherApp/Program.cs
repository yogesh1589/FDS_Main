using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
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

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool GetKernelObjectSecurity(IntPtr Handle, int securityInformation, [Out] byte[] pSecurityDescriptor,
uint nLength, out uint lpnLengthNeeded);

        static void HideConsoleWindow()
        {
            IntPtr hWndConsole = GetConsoleWindow();
            if (hWndConsole != IntPtr.Zero)
            {
                ShowWindow(hWndConsole, SW_HIDE);
            }
        }


        public static RawSecurityDescriptor GetProcessSecurityDescriptor(IntPtr processHandle)
        {
            const int DACL_SECURITY_INFORMATION = 0x00000004;
            byte[] psd = new byte[0];
            uint bufSizeNeeded;
            // Call with 0 size to obtain the actual size needed in bufSizeNeeded
            GetKernelObjectSecurity(processHandle, DACL_SECURITY_INFORMATION, psd, 0, out bufSizeNeeded);
            if (bufSizeNeeded < 0 || bufSizeNeeded > short.MaxValue)
                throw new Win32Exception();
            // Allocate the required bytes and obtain the DACL
            if (!GetKernelObjectSecurity(processHandle, DACL_SECURITY_INFORMATION,
            psd = new byte[bufSizeNeeded], bufSizeNeeded, out bufSizeNeeded))
                throw new Win32Exception();
            // Use the RawSecurityDescriptor class from System.Security.AccessControl to parse the bytes:
            return new RawSecurityDescriptor(psd, 0);
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool SetKernelObjectSecurity(IntPtr Handle, int securityInformation, [In] byte[] pSecurityDescriptor);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentProcess();

        [Flags]
        public enum ProcessAccessRights
        {
            PROCESS_CREATE_PROCESS = 0x0080, //  Required to create a process.
            PROCESS_CREATE_THREAD = 0x0002, //  Required to create a thread.
            PROCESS_DUP_HANDLE = 0x0040, // Required to duplicate a handle using DuplicateHandle.
            PROCESS_QUERY_INFORMATION = 0x0400, //  Required to retrieve certain information about a process, such as its token, exit code, and priority class (see OpenProcessToken, GetExitCodeProcess, GetPriorityClass, and IsProcessInJob).
            PROCESS_QUERY_LIMITED_INFORMATION = 0x1000, //  Required to retrieve certain information about a process (see QueryFullProcessImageName). A handle that has the PROCESS_QUERY_INFORMATION access right is automatically granted PROCESS_QUERY_LIMITED_INFORMATION. Windows Server 2003 and Windows XP/2000:  This access right is not supported.
            PROCESS_SET_INFORMATION = 0x0200, //    Required to set certain information about a process, such as its priority class (see SetPriorityClass).
            PROCESS_SET_QUOTA = 0x0100, //  Required to set memory limits using SetProcessWorkingSetSize.
            PROCESS_SUSPEND_RESUME = 0x0800, // Required to suspend or resume a process.
            PROCESS_TERMINATE = 0x0001, //  Required to terminate a process using TerminateProcess.
            PROCESS_VM_OPERATION = 0x0008, //   Required to perform an operation on the address space of a process (see VirtualProtectEx and WriteProcessMemory).
            PROCESS_VM_READ = 0x0010, //    Required to read memory in a process using ReadProcessMemory.
            PROCESS_VM_WRITE = 0x0020, //   Required to write to memory in a process using WriteProcessMemory.
            DELETE = 0x00010000, // Required to delete the object.
            READ_CONTROL = 0x00020000, //   Required to read information in the security descriptor for the object, not including the information in the SACL. To read or write the SACL, you must request the ACCESS_SYSTEM_SECURITY access right. For more information, see SACL Access Right.
            SYNCHRONIZE = 0x00100000, //    The right to use the object for synchronization. This enables a thread to wait until the object is in the signaled state.
            WRITE_DAC = 0x00040000, //  Required to modify the DACL in the security descriptor for the object.
            WRITE_OWNER = 0x00080000, //    Required to change the owner in the security descriptor for the object.
            STANDARD_RIGHTS_REQUIRED = 0x000f0000,
            PROCESS_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0xFFF),//    All possible access rights for a process object.
        }
        public static void SetProcessSecurityDescriptor(IntPtr processHandle, RawSecurityDescriptor dacl)
        {
            const int DACL_SECURITY_INFORMATION = 0x00000004;
            byte[] rawsd = new byte[dacl.BinaryLength];
            dacl.GetBinaryForm(rawsd, 0);
            if (!SetKernelObjectSecurity(processHandle, DACL_SECURITY_INFORMATION, rawsd))
                throw new Win32Exception();
        }


        static void Main(string[] args)
        {

            IntPtr hProcess = GetCurrentProcess();
            // Read the DACL
            var dacl = GetProcessSecurityDescriptor(hProcess);
            // Insert the new ACE
            dacl.DiscretionaryAcl.InsertAce(
            0,
            new CommonAce(
            AceFlags.None,
            AceQualifier.AccessDenied,
            (int)ProcessAccessRights.PROCESS_ALL_ACCESS,
            new SecurityIdentifier(WellKnownSidType.WorldSid, null),
            false,
            null)
            );
            // Save the DACL
            SetProcessSecurityDescriptor(hProcess, dacl);

            HideConsoleWindow();
            int cnt = 0;

            try
            {


                string basePathEncryption = String.Format("{0}Tempfolder", "C:\\Fusion Data Secure\\FDS\\");

                string encryptOutPutFile = basePathEncryption + @"\Main";

                //string applicationPath = "F:\\Fusion\\FDS\\windowsapp\\Desktop\\bin\\Debug\\";

                string applicationPath = ReturnApplicationPath();

                string exePath = Path.Combine(applicationPath, "FDS.exe");

                if (!string.IsNullOrEmpty(exePath))
                {
                    //DisableStartupEntry("FDS");
                    //DisableStartupEntry("LauncherApp");
                    //SetStartupApp(applicationPath);

                    while (true)
                    {

                        //System.Threading.Thread.Sleep(5000);
                        Thread.Sleep(TimeSpan.FromSeconds(30));

                        if ((!CheckAppRunning(FdsProcessName)) && ((File.Exists(encryptOutPutFile)) || (cnt == 0)))
                        {
                            // Start your WPF application
                            bool runAsAdmin = false;
                            if (IsUserAdministrator())
                            {
                                // If running as administrator, launch FDS.exe with admin privileges
                                runAsAdmin = true;
                            }
                            else
                            {
                                // If running as a regular user, launch FDS.exe without admin privileges
                                runAsAdmin = false;
                            }

                            ProcessStartInfo startInfo = new ProcessStartInfo
                            {
                                //FileName = exePath,
                                //UseShellExecute = false,
                                //CreateNoWindow = true,
                                //Verb = "runas", // Request elevation
                                //WindowStyle = ProcessWindowStyle.Hidden
                                FileName = exePath,
                                UseShellExecute = true,
                                Verb = runAsAdmin ? "runas" : "", // Run as administrator if requested
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

                            Thread.Sleep(TimeSpan.FromSeconds(10));

                            // Monitor the WPF application
                            while (!wpfApp.HasExited)
                            {

                                wpfApp.WaitForExit(1000);
                            }
                            // Restart the application if it's closed

                            // Wait for a moment before restarting
                            wpfApp.Dispose(); // Clean up the process                           
                        }

                    }
                }


            }
            catch (Exception ex)
            {
                WriteLog("Error3 " + ex.ToString());
                ex.ToString();
            }
        }



        public static void SetStartupApp(string applicationPath)
        {
            //WriteLog("Set startup");
            
            DeleteAppIfExists();
            //WriteLog("deleted");

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                if (key != null)
                {
                    string exeFile = Path.Combine(applicationPath, "LauncherApp.exe");

                    key.SetValue("FDS", $"\"{exeFile}\" --opened-at-login --minimize");

                    WriteLog("path set = " + exeFile);

                }
                else
                {
                    WriteLog("Key not found");
                    Console.WriteLine("Registry key not found.");
                }
            }
        }


        public static void DeleteStartupEntry(RegistryKey registryKey, string entryName)
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
        public static string ReadRegistryValue(RegistryKey baseKey, string keyPath, string valueName)
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
        public static void DeleteAppIfExists()
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

        static bool IsUserAdministrator()
        {
            WindowsIdentity user = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(user);

            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        static bool IsAnotherProcessRunning(string processName)
        {
            // Get all running processes with the specified name
            Process[] processes = Process.GetProcessesByName(processName);

            // Exclude the current process from the list
            Process currentProcess = Process.GetCurrentProcess();
            processes = Array.FindAll(processes, p => p.Id != currentProcess.Id);

            // Check if any other processes with the specified name are running
            return processes.Length > 0;
        }

        static string ReturnApplicationPath()
        {
            string applicationPath = "C:\\Fusion Data Secure\\FDS\\";
            try
            {

                string appPath1 = AppDomain.CurrentDomain.BaseDirectory;
                if (!string.IsNullOrEmpty(appPath1))
                {
                    applicationPath = appPath1;
                }


                if ((!string.IsNullOrEmpty(appPath1)) && (appPath1.Contains("LauncherApp")))
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
            catch (Exception ex)
            {
                WriteLog("Error4 " + ex.ToString());
            }
            return applicationPath;
        }


        public static bool CheckAppRunning(string process)
        {
            var currentSessionID = Process.GetCurrentProcess().SessionId;
            return Process.GetProcessesByName(process).Where(p => p.SessionId == currentSessionID).Any();
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
                WriteLog("IsAppRunning2 " + ex.ToString());
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

                string filePath = Path.Combine(path, "LancherLoger_" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt");

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
