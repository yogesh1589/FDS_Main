using FDS.DTO.Responses;
using Microsoft.Win32;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
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


    }
}
