﻿using FDS.Services.AbstractClass;
using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using QRCoder;
using System;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.ApplicationModel.Background;

namespace FDS.Common
{
    static class Generic
    {
        public static string pipName = @"\\.\pipe\AdminPipes";
        public static string certificateData = string.Empty;
        public static string BaseDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        static bool showMessageBoxes = true;
        private const string GoogleHost = "www.google.com";
        public static RSACryptoServiceProvider RSADevice { get; set; }
        public static RSACryptoServiceProvider RSAServer { get; set; }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetInformationJobObject(IntPtr hJob, int infoType, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

        const int JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000;

        public static bool CheckInternetConnection()
        {
            try
            {
                using (var ping = new Ping())
                {
                    PingReply reply = ping.Send(GoogleHost);
                    return reply != null && reply.Status == IPStatus.Success;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static int AlreadyRunningInstance()
        {
            int instanceCount = 0;
            try
            {
                Process currentProcess = Process.GetCurrentProcess();
                Process[] processes = Process.GetProcessesByName("FDS");

                foreach (var p in processes)
                {
                    string username = GetProcessOwner(p.Id);
                    if (p.ProcessName.Equals("fds", StringComparison.OrdinalIgnoreCase) &&
                        username.Equals(Environment.UserName, StringComparison.OrdinalIgnoreCase))
                    {
                        instanceCount++;
                        IntPtr hFound = p.MainWindowHandle;
                        if (User32API.IsIconic(hFound))
                            User32API.ShowWindow(hFound, User32API.SW_RESTORE);
                        User32API.SetForegroundWindow(hFound);
                    }
                }
            }
            catch { }
            return instanceCount;
        }

        public static string GetProcessOwner(int processId)
        {
            string query = "Select * From Win32_Process Where ProcessID = " + processId;
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();
            string username = string.Empty;
            foreach (ManagementObject obj in processList)
            {
                string[] argList = new string[] { string.Empty, string.Empty };
                int returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
                if (returnVal == 0)
                {
                    username = argList[0];
                }
            }
            return username;
        }

        public static bool IsValidTokenNumber(string Token)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(Token, @"^[0-9]$");
        }
        public static bool IsValidEmailTokenNumber(string EmailToken)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(EmailToken, @"^[ A-Za-z0-9_-]*$");
        }

        public static bool IsValidMobileNumber(string mobileNumber)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(mobileNumber, @"^[0-9]{10}$");
        }
        public static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public static ImageSource GetQRCode(string Code)
        {
            // Generate the QR Code
            ImageSource imageSource = null;
            try
            {

                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(Code, QRCodeGenerator.ECCLevel.Q);
                QRCode qrCode = new QRCode(qrCodeData);

                // Convert QR Code to Bitmap
                Bitmap qrBitmap;
                using (var qrCodeImage = qrCode.GetGraphic(20))
                {
                    qrBitmap = new Bitmap(qrCodeImage);
                }

                // Create new Bitmap with transparent background
                System.Drawing.Bitmap newBitmap = new Bitmap(qrBitmap.Width, qrBitmap.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (System.Drawing.Graphics graphics = Graphics.FromImage(newBitmap))
                {
                    graphics.Clear(System.Drawing.Color.Transparent);

                    // Draw QR Code onto new Bitmap
                    graphics.DrawImage(qrBitmap, 0, 0);

                    // Calculate position for logo in center of new Bitmap
                    int logoSize = 300;
                    int logoX = (newBitmap.Width - logoSize) / 2;
                    int logoY = (newBitmap.Height - logoSize) / 2;

                    // Load logo image from file
                    Image logoImage = Image.FromFile(Path.Combine(BaseDir, "Assets/FDSIcon.png"));

                    // Draw logo onto new Bitmap
                    graphics.DrawImage(logoImage, logoX, logoY, logoSize, logoSize);
                }

                // Convert new Bitmap to ImageSource for use in WPF
                imageSource = Imaging.CreateBitmapSourceFromHBitmap(
                    newBitmap.GetHbitmap(),
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());


            }
            catch (Exception ex)
            {
                if (showMessageBoxes == true)
                {
                    MessageBox.Show("An error occurred while creating img for QR: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            return imageSource;
        }


        //public static bool DeleteDirecUninstall()
        //{

        //    string installationPath = string.Empty;
        //    RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
        //    if (registryKey != null)
        //    {
        //        object obj = registryKey.GetValue("FDS");
        //        if (obj != null)
        //            installationPath = Path.GetDirectoryName(obj.ToString());
        //        MessageBox.Show(installationPath);
        //    }
        //    DeleteDirectoryContents(installationPath + "\\");
        //    return true;

        //}

        //public static void DeleteDirectoryContents(string directoryPath)
        //{
        //    if (Directory.Exists(directoryPath))
        //    {

        //        DirectoryInfo directoryInfo = new DirectoryInfo(directoryPath);
        //        foreach (FileInfo file in directoryInfo.GetFiles())
        //        {
        //            Thread.Sleep(10);

        //            if (!file.ToString().Contains("FDS.exe"))
        //            {
        //                file.Delete();
        //            }
        //            //Console.WriteLine(file+ " Files Deleted from installation path");

        //        }
        //        foreach (DirectoryInfo dir in directoryInfo.GetDirectories())
        //        {

        //            dir.Delete(true);

        //        }
        //        //MessageBox.Show(directoryPath);
        //        //// Delete the directory and its contents recursively.
        //        //Directory.Delete(directoryPath, true);               
        //    }
        //}

        public static void CreateBackup()
        {
            string exePath = Process.GetCurrentProcess().MainModule.FileName;
            string exeDirectory = Path.GetDirectoryName(exePath);
            string backupFolderPath = Path.Combine("C:\\Program Files (x86)", "FDS");

            if (!Directory.Exists(backupFolderPath))
            {
                Directory.CreateDirectory(backupFolderPath);
            }
            try
            {
                CopyFilesRecursively(exeDirectory, backupFolderPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating backup: " + ex.Message);
            }
        }

        static void CopyFilesRecursively(string sourceDir, string targetDir)
        {
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(targetDir, fileName);

                if (!File.Exists(destFile))
                {
                    File.Copy(file, destFile);
                }
            }

            foreach (string sourceSubDir in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(sourceSubDir);
                string destSubDir = Path.Combine(targetDir, dirName);

                if (!Directory.Exists(destSubDir))
                {
                    Directory.CreateDirectory(destSubDir);
                }

                CopyFilesRecursively(sourceSubDir, destSubDir);
            }
        }

        public static void AutoRestart()
        {
            #region Auto start on startup done by Installer 

            string applicationPath = "";
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
            if (registryKey != null)
            {
                object obj = registryKey.GetValue("FDS");
                if (obj != null)
                    applicationPath = Path.GetDirectoryName(obj.ToString());
            }
            //if (IsAdmin)
            //{
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                string AutoStartBaseDir = applicationPath;
                if (string.IsNullOrEmpty(AutoStartBaseDir))
                {
                    string currentPath = System.Reflection.Assembly.GetEntryAssembly()?.Location;
                    AutoStartBaseDir = Path.GetDirectoryName(currentPath);
                }
                string exeFile = Path.Combine(AutoStartBaseDir, "LauncherApp.exe");
                key.SetValue("FDS", exeFile + " --opened-at-login --minimize");

            }
            catch
            {
                //MessageBox.Show("Error in AutoRestart");
            }
            //}
            #endregion

            //LoadMenu(Screens.GetStart);

        }


        public static void AutoStartLauncherApp(string appPath)
        {
            try
            {
                //string AutoStartBaseDir = GetApplicationpath();
                //string exeFile = Path.Combine(AutoStartBaseDir, "LauncherApp.exe");


                // Add the executable to the registry for startup
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    key.SetValue("LaunchApp", appPath);
                }


                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    string appPath1 = key.GetValue("LaunchApp") as string;

                    if (!string.IsNullOrEmpty(appPath1))
                    {
                        try
                        {
                            ProcessStartInfo startInfo = new ProcessStartInfo();
                            startInfo.FileName = appPath1; // Replace with your console app's executable path
                            startInfo.UseShellExecute = false;
                            startInfo.RedirectStandardOutput = true;
                            startInfo.RedirectStandardError = true;
                            startInfo.CreateNoWindow = true;
                            startInfo.WindowStyle = ProcessWindowStyle.Hidden;

                            using (Process process = new Process())
                            {
                                process.StartInfo = startInfo;
                                process.OutputDataReceived += (sender, e) => Console.WriteLine("Output: " + e.Data);
                                process.ErrorDataReceived += (sender, e) => Console.WriteLine("Error: " + e.Data);

                                process.Start();
                                process.BeginOutputReadLine();
                                process.BeginErrorReadLine();
                                process.WaitForExit(1000);
                            }


                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error starting the application: " + ex.Message);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Application path not found in registry.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error");
                // MessageBox.Show("Error in AutoLauncher" + ex.ToString());
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

        public static string GetApplicationpath()
        {
            //string applicationPath = "C:\\Fusion Data Secure\\FDS\\";
            string applicationPath = string.Empty;
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
                    }
                }
            }

            if (bCnt > 0)
            {
                result = true;
            }
            return result;
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


        public static void StopRemoveStartupApplication()
        {

            string applicationName = "LauncherApp.exe";
            string applicationPath = GetApplicationpath();
            
            string exeFile = Path.Combine(applicationPath, applicationName);

            if (File.Exists(exeFile))
            {
                if (IsAppRunning(applicationName))
                {
                    MessageBox.Show("Application stopped");
                    StopApplication(applicationName);
                    //Console.WriteLine("Application stopped.");
                }
                else
                {
                    MessageBox.Show("Application is not running.");
                    //Console.WriteLine("Application is not running.");
                }

                RemoveFromStartup(applicationName);
                //Console.WriteLine("Application removed from startup folder.");
            }
            else
            {
                //Console.WriteLine("Application file not found.");
            }

        }


        static void StopApplication(string processName)
        {
            string exeFileName = System.IO.Path.GetFileNameWithoutExtension(processName);
            Process[] chromeProcesses = Process.GetProcessesByName(exeFileName);

            foreach (Process process in chromeProcesses)
            {
                string processOwner = GetProcessOwner2(process.Id);
                if (!string.IsNullOrEmpty(processOwner))
                {
                    if (System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToUpper().ToString().Contains(processOwner.ToUpper().ToString()))
                    {
                        process.Kill();
                        break;
                    }
                }
            }
        }

        static void RemoveFromStartup(string applicationName)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            key.DeleteValue(Path.GetFileNameWithoutExtension(applicationName), false);
        }


        public static void UninstallMSIProduct(string productCode)
        {
            try
            {

                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = "msiexec.exe";
                //startInfo.Arguments = $"/x {productCode} /qn"; // Use /x to uninstall with quiet mode (/qn)
                startInfo.Arguments = $"/x {productCode} /qn /norestart";
                startInfo.WindowStyle = ProcessWindowStyle.Hidden; // Set the window style to hidden
                startInfo.Verb = "runas"; // Run with elevated privileges
                startInfo.UseShellExecute = true;
                using (Process uninstallProcess = Process.Start(startInfo))
                {

                    uninstallProcess.WaitForExit();
                    Console.WriteLine("Uninstallation completed.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        static bool CheckUninstallKey(RegistryKey key, string applicationName)
        {
            try
            {

                if (key != null)
                {
                    foreach (string subKeyName in key.GetSubKeyNames())
                    {
                        try
                        {
                            using (RegistryKey subKey = key.OpenSubKey(subKeyName))
                            {
                                object displayNameValue = subKey.GetValue("DisplayName");

                                if (displayNameValue != null && displayNameValue.ToString().Contains(applicationName))
                                {
                                    string uninstallString = subKey.GetValue("UninstallString").ToString();
                                    string productCode = uninstallString;

                                    try
                                    {

                                        string productCode1 = uninstallString.Replace("/I", "").Replace("MsiExec.exe", "").Trim();
                                        UninstallMSIProduct(productCode1);


                                    }
                                    catch (Exception ex)
                                    {
                                        if (showMessageBoxes == true)
                                        {
                                            MessageBox.Show(ex.ToString());
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {

                        }
                    }
                    return true;
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        public static void UninstallFDS()
        {
            string applicationName = "FDS";
            bool uninstallURL = false;

            RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\");
            uninstallURL = CheckUninstallKey(key, applicationName);

            if (!uninstallURL)
            {
                RegistryKey key1 = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\");
                uninstallURL = CheckUninstallKey(key1, applicationName);
            }

            Process[] processes = Process.GetProcessesByName(applicationName);

            foreach (Process process in processes)
            {
                try
                {
                    process.Kill();
                }
                catch (Exception ex)
                {

                }
            }
            //key.DeleteSubKeyTree(applicationName);
            MessageBox.Show("Application uninstalled successfully", "Info", MessageBoxButton.OK, MessageBoxImage.Information);

        }

        public static bool IsUserAdministrator()
        {

            string username = Environment.UserName;

            using (PrincipalContext context1 = new PrincipalContext(ContextType.Machine))
            {
                UserPrincipal user1 = UserPrincipal.FindByIdentity(context1, username);

                if (user1 != null)
                {
                    GroupPrincipal administratorsGroup = GroupPrincipal.FindByIdentity(context1, "Administrators");

                    if (administratorsGroup != null && user1.IsMemberOf(administratorsGroup))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        public static void SendCommandToService(string command)
        {

            string AutoStartBaseDir = GetApplicationpath();
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


        public static void CreateTaskS()
        {
            try
            {
                string taskName = "WpfSDE";

                using (TaskService taskService = new TaskService())
                {
                    if (taskService.GetTask(taskName) != null)
                    {
                        Console.WriteLine("Task already exists. No need to create.");
                    }
                    else
                    {

                        // Create a new task
                        TaskDefinition taskDefinition = taskService.NewTask();
                        taskDefinition.RegistrationInfo.Description = "Launch WPF App Task";

                        // Set the action to start a program
                        taskDefinition.Actions.Add(new ExecAction("C:\\Fusion Data Secure\\FDS\\LauncherApp.exe", "", null));

                        // Set the trigger to run the task every 1 minute
                        Microsoft.Win32.TaskScheduler.TimeTrigger timeTrigger = new Microsoft.Win32.TaskScheduler.TimeTrigger();
                        timeTrigger.Repetition.Interval = TimeSpan.FromMinutes(1); // Set interval to 1 minute
                        timeTrigger.StartBoundary = DateTime.Now; // Start now
                        timeTrigger.EndBoundary = DateTime.MaxValue; // Runs indefinitely
                        taskDefinition.Triggers.Add(timeTrigger);

                        // Register the task in the Windows Task Scheduler
                        taskService.RootFolder.RegisterTaskDefinition(taskName, taskDefinition);

                        Console.WriteLine("Task created in Task Scheduler to launch the WPF app every 1 minute, starting now.");
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }


    }
}
