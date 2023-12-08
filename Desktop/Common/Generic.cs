using FDS.DTO.Responses;
using Microsoft.Win32;
using QRCoder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Management.Automation;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Devices.Usb;
using Windows.Security.Cryptography.Certificates;

namespace FDS.Common
{
    static class Generic
    {
        public static string certificateData = string.Empty;
        public static string BaseDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        static bool showMessageBoxes = true;
        private const string GoogleHost = "www.google.com";
        public static RSACryptoServiceProvider RSADevice { get; set; }
        public static RSACryptoServiceProvider RSAServer { get; set; }

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


        public static bool DeleteDirecUninstall()
        {
            MessageBox.Show("Deleting 1");
            string installationPath = string.Empty;
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
            if (registryKey != null)
            {
                object obj = registryKey.GetValue("FDS");
                if (obj != null)
                    installationPath = Path.GetDirectoryName(obj.ToString());
                MessageBox.Show(installationPath);
            }
            DeleteDirectoryContents(installationPath + "\\");
            return true;

        }

        public static void DeleteDirectoryContents(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {

                DirectoryInfo directoryInfo = new DirectoryInfo(directoryPath);
                foreach (FileInfo file in directoryInfo.GetFiles())
                {
                    Thread.Sleep(10);
                    MessageBox.Show(file.ToString());
                    if (!file.ToString().Contains("FDS.exe"))
                    {
                        file.Delete();
                    }
                    //Console.WriteLine(file+ " Files Deleted from installation path");

                }
                foreach (DirectoryInfo dir in directoryInfo.GetDirectories())
                {

                    dir.Delete(true);

                }
                //MessageBox.Show(directoryPath);
                //// Delete the directory and its contents recursively.
                //Directory.Delete(directoryPath, true);               
            }
        }

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
                string exeFile = Path.Combine(AutoStartBaseDir, "FDS.exe");
                key.SetValue("FDS", exeFile + " --opened-at-login --minimize");

            }
            catch
            {
                MessageBox.Show("Error in AutoRestart");
            }
            //}
            #endregion

            //LoadMenu(Screens.GetStart);

        }


        public static void AutoStartLauncherApp(string applicationPath)
        {
            //string applicationPath = GetApplicationpath();

            //if (IsAdmin)
            //{
            try
            {
                MessageBox.Show("Starting AutoLauncher");

                RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                 
                key.SetValue("LauncherApp", applicationPath + " --opened-at-login --minimize");

                MessageBox.Show("value set for AutoLauncher = " + applicationPath);
                if (File.Exists(applicationPath))
                {
                    MessageBox.Show("File exists for AutoLauncher");

                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = applicationPath;
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden; // Set the window style to hidden                                                                       //startInfo.Verb = "runas";
                    Process.Start(startInfo);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in AutoLauncher" + ex.ToString());
            }
        }

        public static string GetApplicationpath()
        {
            string applicationPath = "";
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
                    StopApplication(applicationName);
                    //Console.WriteLine("Application stopped.");
                }
                else
                {
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


        static void RunPowerShellScriptWithElevation(string scriptPath)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = "powershell.exe";
                startInfo.Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"";
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.UseShellExecute = true;
                startInfo.Verb = "runas"; // Run with elevated privileges

                using (Process process = Process.Start(startInfo))
                {
                    process.WaitForExit();
                    Console.WriteLine("PowerShell script executed.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        public static void UninstallPowerShell()
        {
            using (PowerShell PowerShellInstance = PowerShell.Create())
            {
                // Add the PowerShell command
                PowerShellInstance.AddScript("Start-Process msiexec.exe -ArgumentList '/x {356B70FD-9D52-4400-A5C3-D1C6F6F7FBEE} /qn /norestart' -NoNewWindow -Wait");

                // Execute the PowerShell command
                var result = PowerShellInstance.Invoke();

                // Check for errors
                if (PowerShellInstance.HadErrors)
                {
                    foreach (var errorRecord in PowerShellInstance.Streams.Error)
                    {
                        Console.WriteLine("Error: " + errorRecord.ToString());
                    }
                }
                else
                {
                    Console.WriteLine("Uninstallation completed.");
                }
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

                                        return true;
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

            if(!uninstallURL)
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


        
    }
}
