using FDS.Common;
using FDS.DTO.Requests;
using FDS.DTO.Responses;
using Microsoft.Win32;
using Newtonsoft.Json;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Data.SQLite;
using NCrontab;
using System.Management;
using System.Windows.Interop;
using System.Drawing;
using Image = System.Drawing.Image;
using Shell32;
using WpfAnimatedGif;
using System.Net;
using System.IO.Compression;
using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Windows.Forms.Design;
using System.Collections;
using Windows.Storage;
using Windows.System;
using System.Data;
using System.Windows.Controls;

namespace FDS
{
    /// <summary>+
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class FDSMain : Window
    {
        #region Variable declaration
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, RecycleFlag dwFlags);
        [DllImport("shell32.dll")]
        static extern int SHQueryRecycleBin(string pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);
        [StructLayout(LayoutKind.Sequential)]
        struct SHQUERYRBINFO
        {
            public long cbSize;
            public long i64Size;
            public long i64NumItems;
        }
        [Flags]
        enum RecycleFlag : int
        {
            SHERB_NOCONFIRMATION = 0x00000001,
            SHERB_NOPROGRESSUI = 0x00000001,
            SHERB_NOSOUND = 0x00000004
        }

        DispatcherTimer timerQRCode;
        DispatcherTimer timerDeviceLogin;
        DispatcherTimer timerLastUpdate;
        DispatcherTimer QRGeneratortimer;
        DispatcherTimer CronLastUpdate;
        DispatcherTimer UninstallResponseTimer;

        int TotalSeconds = Common.AppConstants.TotalKeyActivationSeconds;
        System.Windows.Forms.NotifyIcon icon;
        public DeviceResponse DeviceResponse { get; private set; }
        public Window thisWindow { get; }
        public HttpClient client { get; }
        public QRCodeResponse QRCodeResponse { get; private set; }
        RSACryptoServiceProvider RSADevice { get; set; }
        RSACryptoServiceProvider RSAServer { get; set; }
        private bool isLoggedIn { get; set; }
        public byte[] EncKey { get; set; }
        bool IsAdmin => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        public static string BaseDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        List<string> whitelistedDomain = new List<string>();
        bool IsQRGenerated;

        [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredDelete(string target, int type, int reserved);

        public Dictionary<SubservicesData, DateTime> lstCron = new Dictionary<SubservicesData, DateTime>();
        public DateTime RecycleBinCronTime;
        public DateTime FlushDNSCronTime;
        public DateTime DiskCleaningCronTime;
        public DateTime WebCacheCronTime;
        public DateTime WebHistoryCronTime;
        public DateTime WebCookieCronTime;
        public DateTime RegistryCronTime;
        public bool IsUnInstallFlag;
        public bool IsServiceActive = true;
        List<string> userList = new List<string>();
        string applicationName = "FDS";
        string TempPath = @"C:\web\Temp\FDS\";
        string TempMSIPath = @"C:\web\Temp\FDS\FDSMSI\";
        ViewModel VM = new ViewModel();
        bool isUninstallRequestRaised = false;
        bool isInternetConnected = true;
        bool IsAutoUpdated = false;
        bool IsAuthenticationFromQR = false;
        public ObservableCollection<CountryCode> AllCounties { get; }
        public string CodeVersion = "";
        string basePathEncryption = String.Format("{0}Tempfolder", AppDomain.CurrentDomain.BaseDirectory);
        string encryptOutPutFile = @"\Main";
        System.Windows.Controls.Image imgLoader;
        #endregion

        #region Application initialization / Load
        public FDSMain()
        {
            try
            {

                int insCount = AlreadyRunningInstance();
                if (insCount > 1)
                    App.Current.Shutdown();

                InitializeComponent();
                DataContext = new ViewModel();
                QRGeneratortimer = new DispatcherTimer();
                QRGeneratortimer.Interval = TimeSpan.FromMilliseconds(100);
                QRGeneratortimer.Tick += QRGeneratortimer_Tick;

                timerQRCode = new DispatcherTimer();
                timerQRCode.Interval = TimeSpan.FromMilliseconds(1000);
                timerQRCode.Tick += timerQRCode_Tick;

                timerDeviceLogin = new DispatcherTimer();
                timerDeviceLogin.Interval = TimeSpan.FromMilliseconds(1000 * 5);
                timerDeviceLogin.Tick += TimerDeviceLogin_Tick;

                timerLastUpdate = new DispatcherTimer();
                timerLastUpdate.Interval = TimeSpan.FromMilliseconds(10000);
                timerLastUpdate.Tick += TimerLastUpdate_Tick;
                timerLastUpdate.IsEnabled = false;

                CronLastUpdate = new DispatcherTimer();
                CronLastUpdate.Interval = TimeSpan.FromMinutes(1);
                CronLastUpdate.Tick += CronLastUpdate_Tick;
                CronLastUpdate.IsEnabled = false;

                UninstallResponseTimer = new DispatcherTimer();
                UninstallResponseTimer.Tick += UninstallResponseTimer_Tick;
                UninstallResponseTimer.Interval = TimeSpan.FromMilliseconds(1000); // in miliseconds

                icon = new System.Windows.Forms.NotifyIcon();
                icon.Icon = new System.Drawing.Icon(Path.Combine(BaseDir, "Assets/FDSDesktopLogo.ico"));//new System.Drawing.Icon(Path.Combine(Directory.GetParent(System.Environment.CurrentDirectory).Parent.FullName + "\\Assets\\FDSDesktopLogo.ico"));
                icon.Visible = true;
                icon.BalloonTipText = "The app has been minimised. Click the tray icon to show.";
                icon.BalloonTipTitle = "FDS (Scanning & Cleaning)";
                icon.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
                icon.Click += Icon_Click;

                //lblSerialNumber.Text = lblPopSerialNumber.Text = AppConstants.SerialNumber;
                //lblUserName.Text = lblDeviceName.Text = AppConstants.MachineName;
                //string mac = AppConstants.MACAddress;

                thisWindow = GetWindow(this);
                client = new HttpClient { BaseAddress = AppConstants.EndPoints.BaseAPI };
                if (IsAdmin)
                {
                    IsUninstallFlagUpdated();
                }
                //cmbCountryCode.DropDownOpened += cmbCountryCode_DropDownOpened;
                cmbCountryCode.DropDownClosed += cmbCountryCode_DropDownClosed;
                txtCodeVersion.Text = AppConstants.CodeVersion;
                imgLoader = SetGIF("\\Assets\\spinner.gif");

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            //whitelistedDomain.Add("'%.google.com%'");
            //whitelistedDomain.Add("'%.clickup.com%'");
            //whitelistedDomain.Add("'%.slack.com%'");

            //#region Auto start on startup done by Installer
            //RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            //string AutoStartBaseDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            //string exeFile = Path.Combine(BaseDir, "FDS.exe");
            //Assembly curAssembly = Assembly.GetExecutingAssembly();
            //key.SetValue("FDS", exeFile);

            //#endregion
        }
        static bool CheckInternetConnection()
        {
            try
            {
                using (var ping = new Ping())
                {
                    const string host = "www.google.com"; // Use a reliable external host
                    PingReply reply = ping.Send(host);

                    return (reply != null && reply.Status == IPStatus.Success);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
        public static int AlreadyRunningInstance()
        {
            bool running = false;
            int InstanceCount = 0;
            try
            {

                // Getting collection of process  
                Process currentProcess = Process.GetCurrentProcess();

                //MessageBox.Show("username" + username);
                Process[] processes = Process.GetProcessesByName("FDS");
                // Check with other process already running   
                foreach (var p in processes)
                {
                    string username = GetProcessOwner(p.Id);
                    if (p.ProcessName.ToLower() == "fds" && username == Environment.UserName)
                    {
                        InstanceCount++;
                        running = true;
                        IntPtr hFound = p.MainWindowHandle;
                        if (User32API.IsIconic(hFound)) // If application is in ICONIC mode then  
                            User32API.ShowWindow(hFound, User32API.SW_RESTORE);
                        User32API.SetForegroundWindow(hFound); // Activate the window, if process is already running  
                    }
                }
            }
            catch { }
            return InstanceCount;
        }
        public static string GetProcessOwner(int processId)
        {
            string query = "Select * From Win32_Process Where ProcessID = " + processId;
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();
            string username = ";";
            foreach (ManagementObject obj in processList)
            {
                string[] argList = new string[] { string.Empty, string.Empty };
                int returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
                if (returnVal == 0)
                {
                    // return DOMAIN\user 
                    //return argList[1] + "\\" + argList[0];
                    username = argList[0];
                }
            }
            return username;
        }
        public void FDSMain_Loaded(object sender, RoutedEventArgs e)
        {
            //CredDelete("FDS_Key_Key1", 1, 0);
            try
            {
                // -------Actual Code --------------------------------
                encryptOutPutFile = basePathEncryption + @"\Main";

                ConfigDataClear();
                if (File.Exists(encryptOutPutFile))
                {
                    string finalOutPutFile = basePathEncryption + @"\FinalDecrypt";
                    Common.EncryptionDecryption.DecryptFile(encryptOutPutFile, finalOutPutFile);
                    Common.EncryptionDecryption.ReadDecryptFile(finalOutPutFile);
                }

                bool valid = CheckAllKeys();

                //AutoUpdate();
                if (!valid)
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
                    if (IsAdmin)
                    {
                        RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                        string AutoStartBaseDir = applicationPath;
                        string exeFile = Path.Combine(AutoStartBaseDir, "FDS.exe");
                        key.SetValue("FDS", exeFile + " --opened-at-login --minimize");
                    }
                    #endregion

                    LoadMenu(Screens.GetStart);
                    //IsUninstallFlagUpdated();
                }
                else
                {
                    if (File.Exists(TempPath + "AutoUpdate.exe"))
                    {
                        Directory.Delete(TempPath, true);
                    }

                    LoadMenu(Screens.Landing);
                    TimerLastUpdate_Tick(timerLastUpdate, null);
                    //timerLastUpdate.IsEnabled = true;
                    GetDeviceDetails();
                }
                // -------Actual Code --------------------------------
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadMenu(Screens screen)
        {
            try
            {
                cntGetStart.Visibility = Visibility.Hidden;
                cntQRCode.Visibility = Visibility.Hidden;
                cntLanding.Visibility = Visibility.Hidden;
                //cntNavMenu.Visibility = Visibility.Hidden;
                //imgWires.Visibility = Visibility.Hidden;
                //imgPantgone.Visibility = Visibility.Hidden;
                cntServiceSetting.Visibility = Visibility.Hidden;
                cntServiceSettingPart2.Visibility = Visibility.Hidden;
                cntBackdrop.Visibility = Visibility.Hidden;
                cntPopup.Visibility = Visibility.Hidden;
                lblUserName.Visibility = Visibility.Visible;
                switch (screen)
                {
                    case Screens.GetOTP:
                        GetOTP.Visibility = Visibility.Visible;
                        break;
                    case Screens.GetStart:
                        //imgPantgone.Visibility = Visibility.Hidden;
                        header.Visibility = Visibility.Visible;
                        lblUserName.Visibility = Visibility.Hidden;
                        imgDesktop.Visibility = Visibility.Hidden;
                        cntGetStart.Visibility = Visibility.Visible;
                        btnUninstall.Visibility = Visibility.Hidden;
                        break;
                    case Screens.AuthenticationMethods:
                        AuthenticationMethods.Visibility = Visibility.Visible;
                        AuthenticationStep1.Visibility = Visibility.Hidden;
                        AuthenticationStep2.Visibility = Visibility.Hidden;
                        AuthenticationStep3.Visibility = Visibility.Hidden;
                        lblUserName.Visibility = Visibility.Hidden;
                        AuthenticationProcessing.Visibility = Visibility.Hidden;
                        AuthenticationFailed.Visibility = Visibility.Hidden;
                        AuthenticationSuccessfull.Visibility = Visibility.Hidden;
                        txtEmail.Text = string.Empty;
                        txtPhoneNubmer.Text = string.Empty;
                        txtEmailToken.Text = string.Empty;
                        txtDigit1.Text = string.Empty;
                        txtDigit2.Text = string.Empty;
                        txtDigit3.Text = string.Empty;
                        txtDigit4.Text = string.Empty;
                        txtDigit5.Text = string.Empty;
                        txtDigit6.Text = string.Empty;
                        header.Visibility = Visibility.Visible;
                        lblUserName.Visibility = Visibility.Hidden;
                        imgDesktop.Visibility = Visibility.Hidden;
                        btnUninstall.Visibility = Visibility.Hidden;
                        break;
                    case Screens.AuthenticationStep1:
                        AuthenticationStep1.Visibility = Visibility.Visible;
                        AuthenticationMethods.Visibility = Visibility.Hidden;
                        AuthenticationStep2.Visibility = Visibility.Hidden;
                        lblUserName.Visibility = Visibility.Hidden;
                        header.Visibility = Visibility.Visible;
                        lblUserName.Visibility = Visibility.Hidden;
                        imgDesktop.Visibility = Visibility.Hidden;
                        btnUninstall.Visibility = Visibility.Hidden;
                        txtDigit1.Text = string.Empty;
                        txtDigit2.Text = string.Empty;
                        txtDigit3.Text = string.Empty;
                        txtDigit4.Text = string.Empty;
                        txtDigit5.Text = string.Empty;
                        txtDigit6.Text = string.Empty;
                        break;
                    case Screens.AuthenticationStep2:
                        AuthenticationStep2.Visibility = Visibility.Visible;
                        AuthenticationStep1.Visibility = Visibility.Hidden;
                        AuthenticationMethods.Visibility = Visibility.Hidden;
                        AuthenticationStep3.Visibility = Visibility.Hidden;
                        header.Visibility = Visibility.Visible;
                        lblUserName.Visibility = Visibility.Hidden;
                        imgDesktop.Visibility = Visibility.Hidden;
                        btnUninstall.Visibility = Visibility.Hidden;
                        break;
                    case Screens.AuthenticationStep3:
                        AuthenticationStep3.Visibility = Visibility.Visible;
                        AuthenticationStep2.Visibility = Visibility.Hidden;
                        AuthenticationMethods.Visibility = Visibility.Hidden;
                        header.Visibility = Visibility.Visible;
                        lblUserName.Visibility = Visibility.Hidden;
                        imgDesktop.Visibility = Visibility.Hidden;
                        btnUninstall.Visibility = Visibility.Hidden;
                        break;
                    case Screens.AuthenticationProcessing:
                        AuthenticationProcessing.Visibility = Visibility.Visible;
                        AuthenticationStep3.Visibility = Visibility.Hidden;
                        header.Visibility = Visibility.Visible;
                        lblUserName.Visibility = Visibility.Hidden;
                        imgDesktop.Visibility = Visibility.Hidden;
                        System.Windows.Controls.Image imgProcessing = SetGIF("\\Assets\\loader.gif");
                        AuthenticationProcessing.Children.Add(imgProcessing);
                        break;
                    case Screens.AuthSuccessfull:
                        AuthenticationSuccessfull.Visibility = Visibility.Visible;
                        AuthenticationStep3.Visibility = Visibility.Hidden;
                        AuthenticationProcessing.Visibility = Visibility.Hidden;
                        AuthenticationFailed.Visibility = Visibility.Hidden;
                        header.Visibility = Visibility.Visible;
                        lblUserName.Visibility = Visibility.Hidden;
                        imgDesktop.Visibility = Visibility.Hidden;
                        System.Windows.Controls.Image imgSuccess = SetGIF("\\Assets\\success.gif");
                        AuthenticationSuccessfull.Children.Add(imgSuccess);
                        break;
                    case Screens.AuthFailed:
                        AuthenticationMethods.Visibility = Visibility.Hidden;
                        AuthenticationFailed.Visibility = Visibility.Visible;
                        AuthenticationStep3.Visibility = Visibility.Hidden;
                        AuthenticationSuccessfull.Visibility = Visibility.Hidden;
                        AuthenticationProcessing.Visibility = Visibility.Hidden;
                        header.Visibility = Visibility.Visible;
                        lblUserName.Visibility = Visibility.Hidden;
                        imgDesktop.Visibility = Visibility.Hidden;
                        System.Windows.Controls.Image imgfailed = SetGIF("\\Assets\\failed.gif");
                        AuthenticationFailed.Children.Add(imgfailed);
                        break;
                    case Screens.QRCode:
                        cntQRCode.Visibility = Visibility.Visible;
                        //imgPantgone.Visibility = Visibility.Visible;
                        header.Visibility = Visibility.Visible;
                        lblUserName.Visibility = Visibility.Hidden;
                        imgDesktop.Visibility = Visibility.Hidden;
                        btnUninstall.Visibility = Visibility.Hidden;
                        break;
                    case Screens.Landing:
                        //imgWires.Visibility = Visibility.Visible;
                        cntLanding.Visibility = Visibility.Visible;
                        cntDataProtection.Visibility = Visibility.Hidden;
                        txtHome.Visibility = Visibility.Hidden;
                        txtMenuService.Visibility = Visibility.Hidden;
                        //cntNavMenu.Visibility = Visibility.Visible;
                        //btnSettings.Background = System.Windows.Media.Brushes.White;
                        //btnStatus.Background = Brushes.LightBlue;
                        header.Visibility = Visibility.Visible;
                        lblUserName.Visibility = Visibility.Visible;
                        imgDesktop.Visibility = Visibility.Visible;
                        GetOTP.Visibility = Visibility.Hidden;
                        AuthenticationProcessing.Visibility = Visibility.Hidden;
                        AuthenticationFailed.Visibility = Visibility.Hidden;
                        AuthenticationSuccessfull.Visibility = Visibility.Hidden;
                        AuthenticationMethods.Visibility = Visibility.Hidden;
                        btnUninstall.Visibility = Visibility.Visible;
                        break;
                    case Screens.ServiceClear:
                        //imgWires.Visibility = Visibility.Visible;
                        //cntNavMenu.Visibility = Visibility.Visible;
                        cntServiceSetting.Visibility = Visibility.Visible;
                        cntServiceSettingPart2.Visibility = Visibility.Visible;
                        //btnStatus.Background = Brushes.White;
                        //btnSettings.Background = Brushes.LightBlue;
                        break;
                    case Screens.Popup:
                        //imgWires.Visibility = Visibility.Visible;
                        // cntNavMenu.Visibility = Visibility.Visible;
                        cntServiceSetting.Visibility = Visibility.Visible;
                        cntServiceSettingPart2.Visibility = Visibility.Visible;
                        cntBackdrop.Visibility = Visibility.Visible;
                        cntPopup.Visibility = Visibility.Visible;

                        break;
                    case Screens.DataProtection:
                        //imgWires.Visibility = Visibility.Visible;
                        // cntNavMenu.Visibility = Visibility.Visible;
                        txtHome.Visibility = Visibility.Visible;
                        txtMenuService.Visibility = Visibility.Visible;
                        cntDataProtection.Visibility = Visibility.Visible;
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private System.Windows.Controls.Image SetGIF(string ImagePath)
        {
            string uriString = Directory.GetCurrentDirectory() + ImagePath;
            System.Windows.Controls.Image image = new System.Windows.Controls.Image();
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(uriString);
            bitmapImage.EndInit();
            ImageBehavior.SetAnimatedSource(image, (ImageSource)bitmapImage);
            return image;
        }

        private async void TimerLastUpdate_Tick(object sender, EventArgs e)
        {
            //await GetDeviceDetails();

            await CheckDeviceHealth();
        }
        private async void TimerDeviceLogin_Tick(object sender, EventArgs e)
        {
            await devicelogin(true);
        }
        private void timerQRCode_Tick(object sender, EventArgs e)
        {
            try
            {
                TimeSpan t = new TimeSpan(0, 0, TotalSeconds);
                lblTimer.Text = $"{t.Minutes.ToString("00")}:{t.Seconds.ToString("00")} minutes";
                if (TotalSeconds <= 0)
                {
                    timerQRCode.IsEnabled = false;
                    timerDeviceLogin.IsEnabled = false;
                    btnGetStarted_Click(btnGetStarted, null);
                }
                TotalSeconds--;
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CheckAllKeys()
        {
            try
            {
                RSAParameters RSAParam;

                RSADevice = new RSACryptoServiceProvider(2048);
                RSAParam = RSADevice.ExportParameters(true);


                string filePath = Path.Combine(basePathEncryption, "Main");

                if (!File.Exists(filePath))
                {
                    return false;
                }

                RSAParam = new RSAParameters
                {
                    InverseQ = Convert.FromBase64String(String.IsNullOrEmpty(ConfigDetails.InverseQ) ? string.Empty : ConfigDetails.InverseQ),
                    DQ = Convert.FromBase64String(String.IsNullOrEmpty(ConfigDetails.DQ) ? string.Empty : ConfigDetails.DQ),
                    DP = Convert.FromBase64String(String.IsNullOrEmpty(ConfigDetails.DP) ? string.Empty : ConfigDetails.DP),
                    Q = Convert.FromBase64String(String.IsNullOrEmpty(ConfigDetails.Q) ? string.Empty : ConfigDetails.Q),
                    P = Convert.FromBase64String(String.IsNullOrEmpty(ConfigDetails.P) ? string.Empty : ConfigDetails.P),
                    D = Convert.FromBase64String(String.IsNullOrEmpty(ConfigDetails.D) ? string.Empty : ConfigDetails.D),
                    Exponent = Convert.FromBase64String(String.IsNullOrEmpty(ConfigDetails.Exponent) ? string.Empty : ConfigDetails.Exponent),
                    Modulus = Convert.FromBase64String(String.IsNullOrEmpty(ConfigDetails.Modulus) ? string.Empty : ConfigDetails.Modulus),
                };

                RSADevice = new RSACryptoServiceProvider(2048);
                RSADevice.ImportParameters(RSAParam);

                var key1 = String.IsNullOrEmpty(ConfigDetails.Key1) ? string.Empty : ConfigDetails.Key1;
                var key2 = String.IsNullOrEmpty(ConfigDetails.Key2) ? string.Empty : ConfigDetails.Key2;
                var Authentication_token = String.IsNullOrEmpty(ConfigDetails.Authentication_token) ? string.Empty : ConfigDetails.Authentication_token;
                var Authorization_token = String.IsNullOrEmpty(ConfigDetails.Authorization_token) ? string.Empty : ConfigDetails.Authorization_token;


                bool ValidServerKey = !string.IsNullOrEmpty(key1) && !string.IsNullOrEmpty(key2) && !string.IsNullOrEmpty(Authentication_token) && !string.IsNullOrEmpty(Authorization_token);
                if (!ValidServerKey)
                {
                    return false;
                }
                QRCodeResponse = new QRCodeResponse
                {
                    Public_key = key1 + key2,
                    Authentication_token = Authentication_token,
                    Authorization_token = Authorization_token
                };
                RSAServer = new RSACryptoServiceProvider(2048);
                RSAServer = RSAKeys.ImportPublicKey(System.Text.ASCIIEncoding.ASCII.GetString(Convert.FromBase64String(QRCodeResponse.Public_key)));
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

        }


        #endregion

        #region Authentication methods
        private void btnGetStarted_Click(object sender, RoutedEventArgs e)
        {
            ConfigDataClear();
            LoadMenu(Screens.AuthenticationMethods);
        }
        private void btnQR_Click(object sender, RoutedEventArgs e)
        {
            GenerateQRCode("QR");
            Dispatcher.Invoke(() =>
            {
                LoadMenu(Screens.QRCode);
                //timerDeviceLogin.IsEnabled = true;
            });
        }
        private void btnCredential_Click(object sender, RoutedEventArgs e)
        {
            LoadMenu(Screens.AuthenticationStep1);
            GetcountryCode();
        }
        private void cmbCountryCode_KeyUp(object sender, KeyEventArgs e)
        {
            string searchKeyword = cmbCountryCode.Text.ToLower();


            // Filter the Countries collection based on the search text
            var filteredCountries = VM.AllCountries.Where(c => c.DisplayText.ToLower().Contains(searchKeyword));

            // Update the ComboBox items source with the filtered collection
            cmbCountryCode.ItemsSource = filteredCountries;
            cmbCountryCode.IsDropDownOpen = true;
        }

        public void ClearChildrenNode()
        {
            if (ImageContainerCountryCode.Children.Count > 0)
            {
                ImageContainerCountryCode.Children.Remove(imgLoader);
            }
            if (ImageContainerOTP.Children.Count > 0)
            {
                ImageContainerOTP.Children.Remove(imgLoader);
            }
            if (ImageContainerToken.Children.Count > 0)
            {
                ImageContainerToken.Children.Remove(imgLoader);
            }
            if (ImageContainerQR.Children.Count > 0)
            {
                ImageContainerQR.Children.Remove(imgLoader);
            }
        }

        public async void GetcountryCode()
        {
            cmbCountryCode.IsEnabled = false;
            txtPhoneValidation.IsEnabled = false;
            btnSendOTP.IsEnabled = false;


            // Show the spinner
            ClearChildrenNode();
            if (ImageContainerCountryCode.Children.Count == 0)
            {
                ImageContainerCountryCode.Children.Add(imgLoader);
            }
            var response = await client.GetAsync(AppConstants.EndPoints.CountryCode);
            ClearChildrenNode();

            //End

            if (response.IsSuccessStatusCode)
            {
                btnSendOTP.IsEnabled = true;
                txtPhoneValidation.IsEnabled = true;
                cmbCountryCode.IsEnabled = true;
                var responseString = await response.Content.ReadAsStringAsync();
                CountryCodeResponse responseData = JsonConvert.DeserializeObject<CountryCodeResponse>(responseString);
                List<CountryCode> countryList = responseData.data;
                VM.AllCountries = countryList;
                cmbCountryCode.ItemsSource = VM.AllCountries;
            }
        }
        private void cmbCountryCode_DropDownClosed(object sender, EventArgs e)
        {
            if (cmbCountryCode.SelectedItem != null)
            {
                if (cmbCountryCode.SelectedItem is CountryCode selectedCountry)
                {
                    cmbCountryCode.SelectedValue = selectedCountry.Phone_code;
                    VM.SelectedCountryCode = selectedCountry.Phone_code;
                }
            }
        }

        private async void btnSendOTP_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(txtEmail.Text) && !string.IsNullOrWhiteSpace(txtPhoneNubmer.Text) && IsValidEmail(txtEmail.Text) && IsValidMobileNumber(txtPhoneNubmer.Text))
                {
                    txtPhoneValidation.Visibility = Visibility.Collapsed;
                    txtEmailValidation.Visibility = Visibility.Collapsed;

                    var formContent = new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string>("assing_to_user", txtEmail.Text),
                        new KeyValuePair<string, string>("phone_no", txtPhoneNubmer.Text),
                        new KeyValuePair<string, string>("phone_code", txtCountryCode.Text)
                    };


                    // Show the spinner
                    ClearChildrenNode();
                    if (ImageContainerOTP.Children.Count == 0)
                    {
                        ImageContainerOTP.Children.Add(imgLoader);
                    }
                    var response = await client.PostAsync(AppConstants.EndPoints.Otp, new FormUrlEncodedContent(formContent));

                    ClearChildrenNode();
                    //End


                    if (response.IsSuccessStatusCode)
                    {
                        LoadMenu(Screens.AuthenticationStep2);
                        txtEmailVerification.TextAlignment = TextAlignment.Center;
                        txtCodeVerification.TextAlignment = TextAlignment.Center;
                        txtCodeVerification.Text = "A verification code has been sent to \n" + txtPhoneNubmer.Text;
                        txtEmailVerification.Text = "A 32 digit token has been sent to  \n" + txtEmail.Text;
                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(txtEmail.Text))
                    {
                        txtEmailValidation.Text = "Please enter email";
                        txtEmailValidation.Visibility = Visibility.Visible;
                    }
                    else if (!IsValidEmail(txtEmail.Text))
                    {
                        txtEmailValidation.Text = "Invalid email address!";
                        txtEmailValidation.Visibility = Visibility.Visible;
                    }
                    else if (string.IsNullOrWhiteSpace(txtPhoneNubmer.Text))
                    {
                        txtPhoneValidation.Text = "Please enter phone number";
                        txtPhoneValidation.Visibility = Visibility.Visible;
                    }
                    else if (!IsValidMobileNumber(txtPhoneNubmer.Text))
                    {
                        txtPhoneValidation.Text = "Invalid phone number! Phone number should have 10 digit";
                        txtPhoneValidation.Visibility = Visibility.Visible;
                    }
                    //else
                    //{
                    //    txtEmailValidation.Text = "Please enter email";
                    //    txtEmailValidation.Visibility = Visibility.Visible;
                    //    txtPhoneValidation.Text = "Please enter phone number";
                    //    txtPhoneValidation.Visibility = Visibility.Visible;
                    //}
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while receiving OTP: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private bool IsValidMobileNumber(string mobileNumber)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(mobileNumber, @"^[0-9]{10}$");
        }
        private bool IsValidEmail(string email)
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
        private void txtBack_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            LoadMenu(Screens.AuthenticationMethods);
            txtEmail.Text = "";
            txtPhoneNubmer.Text = "";
        }
        private bool IsValidTokenNumber(string Token)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(Token, @"^[0-9]$");
        }
        private bool IsValidEmailTokenNumber(string EmailToken)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(EmailToken, @"^[ A-Za-z0-9_-]*$");
        }
        private void btnStep2Next_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDigit1.Text) || string.IsNullOrWhiteSpace(txtDigit2.Text) || string.IsNullOrWhiteSpace(txtDigit3.Text)
                || string.IsNullOrWhiteSpace(txtDigit4.Text) || string.IsNullOrWhiteSpace(txtDigit5.Text) || string.IsNullOrWhiteSpace(txtDigit6.Text))
            {
                txtTokenValidation.Text = "Please enter Verification code";
            }
            else if (!IsValidTokenNumber(txtDigit1.Text) && !IsValidTokenNumber(txtDigit2.Text) && !IsValidTokenNumber(txtDigit3.Text)
                && !IsValidTokenNumber(txtDigit5.Text) && !IsValidTokenNumber(txtDigit5.Text) && !IsValidTokenNumber(txtDigit6.Text))
            {
                txtTokenValidation.Text = "Invalid Verification code";
                txtTokenValidation.Visibility = Visibility.Visible;
            }
            else
                LoadMenu(Screens.AuthenticationStep3);
        }
        private void TextBox_LostFocus(object sender, KeyEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            string pastedText = Clipboard.GetText();
            if (!string.IsNullOrEmpty(pastedText) && pastedText.Length == 6)
            {
                // Remove any non-digit characters
                string digitsOnly = new string(pastedText.Where(char.IsDigit).ToArray());

                // Distribute the characters across the TextBox controls
                for (int i = 0; i < digitsOnly.Length && i < 6; i++)
                {
                    TextBox targetTextBox = (TextBox)FindName($"txtDigit{i + 1}");
                    targetTextBox.Text = digitsOnly[i].ToString();
                }
                e.Handled = true;
                Clipboard.Clear();
            }
            else
            {
                if (!string.IsNullOrEmpty(textBox.Text))
                {
                    if (char.IsDigit(textBox.Text, textBox.Text.Length - 1))
                    {
                        //e.Handled = true; // Suppress non-numeric input
                        if (!(e.Key == Key.Tab || e.Key == Key.LeftShift || e.Key == Key.RightShift))
                        {
                            TraversalRequest request = new TraversalRequest(FocusNavigationDirection.Next);
                            textBox.MoveFocus(request);
                        }

                    }
                    else
                    {
                        textBox.Text = string.Empty;
                        e.Handled = true;
                    }
                }
            }
        }

        private async void QRGeneratortimer_Tick(object sender, EventArgs e)
        {
            if (!IsAuthenticationFromQR)
            {
                if (IsQRGenerated == true)
                {
                    QRGeneratortimer.Stop();
                    string VerificationCode = txtDigit1.Text + txtDigit2.Text + txtDigit3.Text + txtDigit4.Text + txtDigit5.Text + txtDigit6.Text;
                    var formContent = new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string>("code_version", AppConstants.CodeVersion),
                        new KeyValuePair<string, string>("assing_to_user", txtEmail.Text),
                        new KeyValuePair<string, string>("phone_no", txtPhoneNubmer.Text),
                        new KeyValuePair<string, string>("phone_code", txtCountryCode.Text),
                        new KeyValuePair<string, string>("otp", VerificationCode),
                        new KeyValuePair<string, string>("token", txtEmailToken.Text),
                        new KeyValuePair<string, string>("qr_code_token", DeviceResponse.qr_code_token)
                    };
                    Dispatcher.Invoke(() =>
                    {
                        LoadMenu(Screens.AuthenticationProcessing);
                    });

                    var response = await client.PostAsync(AppConstants.EndPoints.DeviceToken, new FormUrlEncodedContent(formContent));
                    if (response.IsSuccessStatusCode)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            LoadMenu(Screens.AuthSuccessfull);
                            Dispatcher.Invoke(() =>
                            {
                                LoadMenu(Screens.Landing);
                                devicelogin(true);
                                //timerDeviceLogin.IsEnabled = true;
                            });

                        });
                    }
                    else
                    {
                        LoadMenu(Screens.AuthFailed);
                    }
                }
            }
        }

        private void txtTryAgain_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            LoadMenu(Screens.AuthenticationMethods);
        }

        private async void btnStep3Submit_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtEmailToken.Text))
            {
                txtEmailTokenValidation.Text = "Please enter token";
            }
            else if (!IsValidEmailTokenNumber(txtEmailToken.Text))
            {
                txtEmailTokenValidation.Text = "Invalid Token number";
                txtEmailTokenValidation.Visibility = Visibility.Visible;
            }
            else
            {
                //QRGeneratortimer.Start();
                GenerateQRCode("Token");
            }
        }
        private void txtstep2Back_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            LoadMenu(Screens.AuthenticationStep1);
        }
        private void txtstep3Back_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            LoadMenu(Screens.AuthenticationStep2);
        }
        #endregion

        #region generate QR
        public async void GenerateQRCode(string vals)
        {
            try
            {
                
                var formContent = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("serial_number", AppConstants.SerialNumber),
                    new KeyValuePair<string, string>("device_name", AppConstants.MachineName),
                    new KeyValuePair<string, string>("mac_address", AppConstants.MACAddress),
                    new KeyValuePair<string, string>("device_type", AppConstants.DeviceType),
                    new KeyValuePair<string, string>("code_version", AppConstants.CodeVersion),
                    new KeyValuePair<string, string>("os_version", AppConstants.OSVersion),
                    new KeyValuePair<string, string>("device_uuid", AppConstants.UUId),
                };
 
                //Code for Loader
                ClearChildrenNode();

                if ((vals == "Token") && (ImageContainerToken.Children.Count == 0))
                {
                    ImageContainerToken.Children.Add(imgLoader);
                }
                else if ((vals == "QR") && (ImageContainerQR.Children.Count == 0))
                {
                    ImageContainerQR.Children.Add(imgLoader);
                }

                
                var response = await client.PostAsync(AppConstants.EndPoints.Start, new FormUrlEncodedContent(formContent));

 
                ClearChildrenNode();
 
                if (response.IsSuccessStatusCode)
                {
                    TotalSeconds = Common.AppConstants.TotalKeyActivationSeconds;
                    timerQRCode.IsEnabled = true;
                    
                    var responseString = await response.Content.ReadAsStringAsync();
                    DeviceResponse = JsonConvert.DeserializeObject<DeviceResponse>(responseString);
               
                    IsQRGenerated = DeviceResponse != null ? true : false;
                    //timerDeviceLogin.IsEnabled = true;
                    imgQR.Source = GetQRCode(DeviceResponse.qr_code_token);
                  
                    IsAuthenticationFromQR = string.IsNullOrEmpty(txtEmailToken.Text) ? true : false;
                  
                    QRGeneratortimer.Start();
              
                    timerDeviceLogin.Start();
                  
                }
                else
                    MessageBox.Show("API response fails while generating QR code: ", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while generating QR code: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private ImageSource GetQRCode(string Code)
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
                MessageBox.Show("An error occurred while creating img for QR: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return imageSource;


        }
        BitmapImage BitmapToImageSource(System.Drawing.Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();
                return bitmapimage;
            }
        }
        #endregion

        #region device login/Key Exchage
        private async Task devicelogin(bool MenuChange)
        {
            try
            {
                RSAParameters RSAParam;
                var formContent = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("qr_code_token", DeviceResponse.qr_code_token),
                                                                        new KeyValuePair<string, string>("code_version", AppConstants.CodeVersion),};
                var response = await client.PostAsync(AppConstants.EndPoints.CheckAuth, new FormUrlEncodedContent(formContent));
                if (response.IsSuccessStatusCode)
                {
                    isLoggedIn = true;
                    var responseString = await response.Content.ReadAsStringAsync();
                    QRCodeResponse = JsonConvert.DeserializeObject<QRCodeResponse>(responseString);
                    int LengthAllowed = 512;

                    RSAServer = RSAKeys.ImportPublicKey(System.Text.ASCIIEncoding.ASCII.GetString(Convert.FromBase64String(QRCodeResponse.Public_key)));
                    timerQRCode.IsEnabled = false;

                    RSADevice = new RSACryptoServiceProvider(2048);
                    RSAParam = RSADevice.ExportParameters(true);

                    //New Code--
                    string filePath = Path.Combine(basePathEncryption, "TempFile");
                    encryptOutPutFile = basePathEncryption + @"\Main";

                    if (!Directory.Exists(basePathEncryption))
                    {
                        DirectoryInfo di = Directory.CreateDirectory(basePathEncryption);
                    }

                    if (!File.Exists(filePath))
                    {

                        List<string> lstConfig = new List<string>();
                        lstConfig.Add(AppConstants.KeyPrfix + "Key1 : " + (QRCodeResponse.Public_key.Length > LengthAllowed ? QRCodeResponse.Public_key.Substring(0, LengthAllowed) : QRCodeResponse.Public_key));
                        lstConfig.Add(AppConstants.KeyPrfix + "Key2 : " + (QRCodeResponse.Public_key.Length > LengthAllowed ? QRCodeResponse.Public_key.Substring(LengthAllowed, QRCodeResponse.Public_key.Length - LengthAllowed) : ""));
                        lstConfig.Add(AppConstants.KeyPrfix + "Authentication_token : " + QRCodeResponse.Authentication_token);
                        lstConfig.Add(AppConstants.KeyPrfix + "Authorization_token : " + QRCodeResponse.Authorization_token);
                        lstConfig.Add(AppConstants.KeyPrfix + "Modulus : " + Convert.ToBase64String(RSAParam.Modulus));
                        lstConfig.Add(AppConstants.KeyPrfix + "Exponent : " + Convert.ToBase64String(RSAParam.Exponent));
                        lstConfig.Add(AppConstants.KeyPrfix + "D : " + Convert.ToBase64String(RSAParam.D));
                        lstConfig.Add(AppConstants.KeyPrfix + "P :" + Convert.ToBase64String(RSAParam.P));
                        lstConfig.Add(AppConstants.KeyPrfix + "Q : " + Convert.ToBase64String(RSAParam.Q));
                        lstConfig.Add(AppConstants.KeyPrfix + "DP : " + Convert.ToBase64String(RSAParam.DP));
                        lstConfig.Add(AppConstants.KeyPrfix + "DQ : " + Convert.ToBase64String(RSAParam.DQ));
                        lstConfig.Add(AppConstants.KeyPrfix + "InverseQ : " + Convert.ToBase64String(RSAParam.InverseQ));

                        string[] myTokens = lstConfig.ToArray();
                        File.WriteAllLines(filePath, myTokens);

                        Common.EncryptionDecryption.EncryptFile(filePath, encryptOutPutFile);
                        if (File.Exists(encryptOutPutFile))
                        {
                            string finalOutPutFile = basePathEncryption + @"\FinalDecrypt";
                            Common.EncryptionDecryption.DecryptFile(encryptOutPutFile, finalOutPutFile);
                            Common.EncryptionDecryption.ReadDecryptFile(finalOutPutFile);
                        }
                    }

                    if (MenuChange)
                    {
                        LoadMenu(Screens.Landing);
                        timerDeviceLogin.IsEnabled = false;
                        await KeyExchange().ConfigureAwait(false);
                        //timerLastUpdate.IsEnabled = true;
                    }
                }
                else
                {
                    switch (response.StatusCode)
                    {
                        //case System.Net.HttpStatusCode.Unauthorized:
                        //    break;
                        case System.Net.HttpStatusCode.NotFound:
                            timerDeviceLogin.IsEnabled = false;
                            timerQRCode.IsEnabled = false;
                            LoadMenu(Screens.GetStart);
                            break;
                        case System.Net.HttpStatusCode.NotAcceptable:
                            timerDeviceLogin.IsEnabled = false;
                            timerQRCode.IsEnabled = false;
                            btnGetStarted_Click(btnGetStarted, null);
                            break;
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while devicelogin: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async Task KeyExchange()
        {
            //QRCodeResponse = new QRCodeResponse
            //{
            //    Public_key = KeyManager.GetValue("Key1") + KeyManager.GetValue("Key2"),
            //    Authentication_token = KeyManager.GetValue("Authentication_token"),
            //    Authorization_token = KeyManager.GetValue("Authorization_token")
            //};

            if (String.IsNullOrEmpty(ConfigDetails.Authorization_token))
            {
                encryptOutPutFile = basePathEncryption + @"\Main";
                if (File.Exists(encryptOutPutFile))
                {
                    string finalOutPutFile = basePathEncryption + @"\FinalDecrypt";
                    Common.EncryptionDecryption.DecryptFile(encryptOutPutFile, finalOutPutFile);
                    Common.EncryptionDecryption.ReadDecryptFile(finalOutPutFile);
                }
            }

            var exchangeObject = new KeyExchange
            {
                authorization_token = String.IsNullOrEmpty(ConfigDetails.Authorization_token) ? string.Empty : ConfigDetails.Authorization_token,
                //authorization_token = string.Empty,
                //authorization_token = KeyManager.GetValue("authorization_token"),
                mac_address = AppConstants.MACAddress,
                public_key = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(RSAKeys.ExportPublicKey(RSADevice))),
                serial_number = AppConstants.SerialNumber,
                device_uuid = AppConstants.UUId,
            };

            var payload = Encrypt(Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(exchangeObject))));

            var formContent = new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string>("authentication_token", String.IsNullOrEmpty(ConfigDetails.Authentication_token) ? string.Empty : ConfigDetails.Authentication_token) ,
                        new KeyValuePair<string, string>("payload", payload),
                        new KeyValuePair<string, string>("code_version", AppConstants.CodeVersion)
                    };

            var response = await client.PostAsync(AppConstants.EndPoints.KeyExchange, new FormUrlEncodedContent(formContent));
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var responseData = JsonConvert.DeserializeObject<DTO.Responses.ResponseData>(responseString);
                var plainText = Decrypt(responseData.Data);
                var finalData = JsonConvert.DeserializeObject<DTO.Responses.ResponseData>(plainText);
                timerLastUpdate.IsEnabled = true;
                await GetDeviceDetails();
            }
            else
            {
                timerLastUpdate.IsEnabled = false;
                btnGetStarted_Click(btnGetStarted, null);
                MessageBox.Show("An error occurred in KeyExchange: ", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region device health

        public async Task CheckDeviceHealth()
        {


            isInternetConnected = CheckInternetConnection();
            if (isInternetConnected)
            {
                var servicesObject = new RetriveServices
                {
                    authorization_token = String.IsNullOrEmpty(ConfigDetails.Authorization_token) ? string.Empty : ConfigDetails.Authorization_token,
                    //authorization_token = KeyManager.GetValue("authorization_token"),
                    mac_address = AppConstants.MACAddress,
                    serial_number = AppConstants.SerialNumber,
                    current_user = Environment.UserName,
                    device_uuid = AppConstants.UUId,
                    //app_version
                    //os_version
                };
                var payload = Encrypt(Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(servicesObject))));
                //var payload = JsonConvert.SerializeObject(servicesObject).ToString();

                var formContent = new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("authentication_token", String.IsNullOrEmpty(ConfigDetails.Authentication_token) ? string.Empty : ConfigDetails.Authentication_token) ,
                new KeyValuePair<string, string>("payload", payload),
                new KeyValuePair<string, string>("code_version", AppConstants.CodeVersion),
            };

                var response = await client.PostAsync(AppConstants.EndPoints.DeviceHealth, new FormUrlEncodedContent(formContent));

                if (response.IsSuccessStatusCode)
                {

                    var responseString = await response.Content.ReadAsStringAsync();
                    var responseData = JsonConvert.DeserializeObject<DTO.Responses.ResponseData>(responseString);
                    var plainText = RetriveDecrypt(responseData.Data);
                    int idx = plainText.LastIndexOf('}');
                    var result = idx != -1 ? plainText.Substring(0, idx + 1) : plainText;
                    var HealthData = JsonConvert.DeserializeObject<HealthCheckResponse>(result);
                    if (HealthData.call_config)
                    {
                        await DeviceConfigurationCheck();
                    }
                    //else
                    //    await RetrieveCronServices();
                    if (IsServiceActive)
                    {
                        lblCompliant.Text = "Your system is Compliant";
                        string ImagePath = Path.Combine(BaseDir, "Assets/DeviceActive.png");
                        BitmapImage DeviceDeactive = new BitmapImage();
                        DeviceDeactive.BeginInit();
                        DeviceDeactive.UriSource = new Uri(ImagePath);
                        DeviceDeactive.EndInit();
                        imgCompliant.Source = DeviceDeactive;
                        LoadMenu(Screens.Landing);
                    }
                }
                else if (response.StatusCode == HttpStatusCode.BadGateway)
                {
                    timerLastUpdate.IsEnabled = true;
                    lblCompliant.Text = "Ooops! Will be back soon.";
                    string ImagePath = Path.Combine(BaseDir, "Assets/DeviceDisable.png");
                    BitmapImage DeviceDeactive = new BitmapImage();
                    DeviceDeactive.BeginInit();
                    DeviceDeactive.UriSource = new Uri(ImagePath);
                    DeviceDeactive.EndInit();
                    imgCompliant.Source = DeviceDeactive;
                    LoadMenu(Screens.Landing);
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    timerLastUpdate.IsEnabled = false;
                    if (isUninstallRequestRaised)
                    {
                        UninstallProgram();
                    }
                    else
                    {
                        cleanSystem();
                        DeviceResponse.qr_code_token = null;
                    }
                    //btnGetStarted_Click(btnGetStarted, null);
                    LoadMenu(Screens.GetStart);
                    ConfigDataClear();
                    MessageBox.Show("Your device has been deleted", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                timerLastUpdate.IsEnabled = true;
                lblCompliant.Text = "No Internet Connection!";
                string ImagePath = Path.Combine(BaseDir, "Assets/DeviceDisable.png");
                BitmapImage DeviceDeactive = new BitmapImage();
                DeviceDeactive.BeginInit();
                DeviceDeactive.UriSource = new Uri(ImagePath);
                DeviceDeactive.EndInit();
                imgCompliant.Source = DeviceDeactive;

            }
        }
        private async Task DeviceConfigurationCheck()
        {
            var servicesObject = new RetriveServices
            {
                authorization_token = String.IsNullOrEmpty(ConfigDetails.Authorization_token) ? string.Empty : ConfigDetails.Authorization_token,
                //authorization_token = KeyManager.GetValue("authorization_token"),
                mac_address = AppConstants.MACAddress,
                serial_number = AppConstants.SerialNumber,
                device_uuid = AppConstants.UUId,
            };
            var payload = Encrypt(Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(servicesObject))));

            var formContent = new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("authentication_token", String.IsNullOrEmpty(ConfigDetails.Authentication_token) ? string.Empty : ConfigDetails.Authentication_token) ,
                new KeyValuePair<string, string>("payload", payload),
                new KeyValuePair<string, string>("code_version", AppConstants.CodeVersion),
            };

            var response = await client.PostAsync(AppConstants.EndPoints.DeviceConfigCheck, new FormUrlEncodedContent(formContent));

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var responseData = JsonConvert.DeserializeObject<DTO.Responses.ResponseData>(responseString);
                var plainText = RetriveDecrypt(responseData.Data);
                int idx = plainText.LastIndexOf('}');
                var result = idx != -1 ? plainText.Substring(0, idx + 1) : plainText;
                var DeviceConfigData = JsonConvert.DeserializeObject<DeviceConfigCheckResponse>(result);

                if (DeviceConfigData.config_change)
                {
                    if (DeviceConfigData.call_api.Count() > 0)
                    {
                        //DeviceConfigData.call_api.Sort();
                        foreach (var api in DeviceConfigData.call_api)
                        {
                            if (api.Equals("1") || api.Equals("4"))
                            {
                                //await GetDeviceDetails();
                                //MessageBox.Show("Start");
                                await RetrieveServices();
                            }
                            else if (api.Equals("2"))
                            {
                                await GetDeviceDetails();
                            }
                            else if (api.Equals("3"))
                            {
                                await DeviceReauth();
                            }
                            else if (api.Equals("5"))
                            {
                                IsServiceActive = false;
                                await GetDeviceDetails();
                            }
                            else if (api.Equals("6"))
                            {
                                IsServiceActive = true;
                                await GetDeviceDetails();
                            }
                            else if (api.Equals("7"))
                            {
                                AutoUpdate();
                            }
                        }
                    }
                }
            }
            else
            {
                timerLastUpdate.IsEnabled = false;
                btnGetStarted_Click(btnGetStarted, null);
                MessageBox.Show("An error occurred in DeviceConfigurationCheck: ", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async Task DeviceReauth()
        {
            var servicesObject = new RetriveServices
            {
                authorization_token = String.IsNullOrEmpty(ConfigDetails.Authorization_token) ? string.Empty : ConfigDetails.Authorization_token,
                mac_address = AppConstants.MACAddress,
                serial_number = AppConstants.SerialNumber,
                device_uuid = AppConstants.UUId
            };
            var payload = Encrypt(Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(servicesObject))));

            var formContent = new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("authentication_token", String.IsNullOrEmpty(ConfigDetails.Authentication_token) ? string.Empty : ConfigDetails.Authentication_token) ,
                new KeyValuePair<string, string>("payload", payload),
                new KeyValuePair<string, string>("code_version", AppConstants.CodeVersion),
            };

            var response = await client.PostAsync(AppConstants.EndPoints.DeviceReauth, new FormUrlEncodedContent(formContent));
            if (response.IsSuccessStatusCode)
            {
                timerLastUpdate.IsEnabled = false;
                btnGetStarted_Click(btnGetStarted, null);
                //MessageBox.Show("An error occurred in DeviceReauth: ", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                /// need delete api for flushing 
            }
        }
        #endregion

        #region Device detail / Retrive Services
        private async Task GetDeviceDetails()
        {
            var exchangeObject = new DeviceDetails
            {
                serial_number = AppConstants.SerialNumber,
                mac_address = AppConstants.MACAddress,
                authorization_token = String.IsNullOrEmpty(ConfigDetails.Authorization_token) ? string.Empty : ConfigDetails.Authorization_token,
                device_uuid = AppConstants.UUId

            };
            var message = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(exchangeObject)));

            var payload = Encrypt(message);

            var formContent = new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string>("authentication_token", String.IsNullOrEmpty(ConfigDetails.Authentication_token) ? string.Empty : ConfigDetails.Authentication_token) ,
                        new KeyValuePair<string, string>("payload", payload),
                        new KeyValuePair<string, string>("code_version", AppConstants.CodeVersion)
                        //new KeyValuePair<string, string>("os_version", AppConstants.OSVersion)
                    };
            var response = await client.PostAsync(AppConstants.EndPoints.DeviceDetails, new FormUrlEncodedContent(formContent));
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var responseData = JsonConvert.DeserializeObject<DTO.Responses.ResponseData>(responseString);
                var plainText = RetriveDecrypt(responseData.Data);
                int idx = plainText.LastIndexOf('}');
                var result = idx != -1 ? plainText.Substring(0, idx + 1) : plainText;
                var deviceDetail = JsonConvert.DeserializeObject<DeviceDetail>(result);  //

                if (deviceDetail != null)
                {
                    lblSerialNumber.Text = lblPopSerialNumber.Text = deviceDetail.serial_number;
                    lblUserName.Text = lblDeviceName.Text = deviceDetail.device_name;
                    lblLocation.Text = deviceDetail.device_location != null ? deviceDetail.device_location.ToString() : "";
                    txtOrganization.Text = deviceDetail.org_name != null ? deviceDetail.org_name.ToString() : txtOrganization.Text;
                    DateTime convertedDate = DateTime.Parse(Convert.ToString(deviceDetail.updated_on));
                    DateTime localDate = convertedDate.ToLocalTime();
                    txtUpdatedOn.Text = deviceDetail.updated_on != null ? localDate.ToString() : "";

                    //timerLastUpdate.IsEnabled = false;
                    if (deviceDetail.is_active)
                    {
                        await RetrieveServices();
                        lblCompliant.Text = "Your system is Compliant";
                        string ImagePath = Path.Combine(BaseDir, "Assets/DeviceActive.png");
                        BitmapImage DeviceActive = new BitmapImage();
                        DeviceActive.BeginInit();
                        DeviceActive.UriSource = new Uri(ImagePath);
                        DeviceActive.EndInit();
                        imgCompliant.Source = DeviceActive;
                        LoadMenu(Screens.Landing);
                    }
                    else
                    {
                        lblCompliant.Text = "Check With Administrator";
                        string ImagePath = Path.Combine(BaseDir, "Assets/DeviceDisable.png");
                        BitmapImage DeviceDeactive = new BitmapImage();
                        DeviceDeactive.BeginInit();
                        DeviceDeactive.UriSource = new Uri(ImagePath);
                        DeviceDeactive.EndInit();
                        imgCompliant.Source = DeviceDeactive;
                        LoadMenu(Screens.Landing);
                    }
                }
            }
            else
            {
                //timerLastUpdate.IsEnabled = false;
                //btnGetStarted_Click(btnGetStarted, null);
                //MessageBox.Show("An error occurred in GetDeviceDetails: ", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task RetrieveServices()
        {
            var servicesObject = new RetriveServices
            {
                authorization_token = String.IsNullOrEmpty(ConfigDetails.Authorization_token) ? string.Empty : ConfigDetails.Authorization_token,
                mac_address = AppConstants.MACAddress,
                serial_number = AppConstants.SerialNumber,
                device_uuid = AppConstants.UUId
            };
            var payload = Encrypt(Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(servicesObject))));

            var formContent = new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string>("authentication_token", String.IsNullOrEmpty(ConfigDetails.Authentication_token) ? string.Empty : ConfigDetails.Authentication_token) ,
                        new KeyValuePair<string, string>("payload", payload),
                        new KeyValuePair<string, string>("code_version", AppConstants.CodeVersion),
                    };

            var response = await client.PostAsync(AppConstants.EndPoints.DeviceServices, new FormUrlEncodedContent(formContent));

            if (response.IsSuccessStatusCode)
            {
                //MessageBox.Show("Current User: " + WindowsIdentity.GetCurrent().Name, "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                var responseString = await response.Content.ReadAsStringAsync();
                var responseData = JsonConvert.DeserializeObject<DTO.Responses.ResponseData>(responseString);
                var plainText = RetriveDecrypt(responseData.Data);
                int idx = plainText.LastIndexOf('}');
                var result = idx != -1 ? plainText.Substring(0, idx + 1) : plainText;
                var servicesResponse = JsonConvert.DeserializeObject<ServicesResponse>(result);//.Replace("false", "true"));// replace used to test services
                                                                                               //var servicesResponse = JsonConvert.DeserializeObject<ServicesResponse>(plainText);

                DateTime localDate = DateTime.Now.ToLocalTime();
                txtUpdatedOn.Text = localDate.ToString();
                ExecuteServices(servicesResponse);
                timerLastUpdate.IsEnabled = true;
            }
            else
            {
                //timerLastUpdate.IsEnabled = false;
                //btnGetStarted_Click(btnGetStarted, null);
                //MessageBox.Show("An error occurred in RetrieveServices: ", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Encryption / Decryption
        public string Encrypt(string plainText)
        {
            try
            {
                //byte[] Key;
                byte[] AesEncrypted;
                using (var aesAlg = new AesCryptoServiceProvider())
                {
                    // Create an encryptor to perform the stream transform.
                    EncKey = aesAlg.Key;
                    aesAlg.Mode = CipherMode.ECB;
                    aesAlg.Padding = PaddingMode.PKCS7;
                    ICryptoTransform encryptor = aesAlg.CreateEncryptor();
                    // Create the streams used for encryption.
                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                            {
                                //Write all data to the stream.
                                swEncrypt.Write(plainText);
                            }
                            AesEncrypted = msEncrypt.ToArray();
                        }
                    }
                }
                var RsaEncrypted = RSAServer.Encrypt(EncKey, true);
                return Convert.ToBase64String(RsaEncrypted.Concat(AesEncrypted).ToArray());
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while doing encryption: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return "";
            }
        }
        public string Decrypt(string Cipher)
        {
            try
            {
                var bArray = Convert.FromBase64String(Cipher);
                var encKey = bArray.Take(256).ToArray();
                var byteKey = RSADevice.Decrypt(encKey, true);
                string plaintext = null;
                // Create AesManaged    
                using (AesManaged aes = new AesManaged())
                {
                    // Create a decryptor    
                    aes.Mode = CipherMode.ECB;
                    ICryptoTransform decryptor = aes.CreateDecryptor(byteKey, aes.IV);
                    // Create the streams used for decryption.    
                    using (MemoryStream ms = new MemoryStream(bArray.Skip(256).ToArray()))
                    {
                        // Create crypto stream    
                        using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            // Read crypto stream    
                            using (StreamReader reader = new StreamReader(cs))
                                plaintext = reader.ReadToEnd();
                        }
                    }
                }
                return plaintext;
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while doing decryption: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return "";
            }

        }
        public string RetriveDecrypt(string Cipher)
        {
            try
            {
                var bArray = Convert.FromBase64String(Cipher);
                var encKey = bArray.Take(256).ToArray();
                var byteKey = RSADevice.Decrypt(encKey, true);
                string plaintext = null;
                // Create AesManaged    
                using (AesManaged aes = new AesManaged())
                {
                    // Create a decryptor    
                    aes.Mode = CipherMode.ECB;
                    aes.Padding = PaddingMode.None;
                    ICryptoTransform decryptor = aes.CreateDecryptor(byteKey, aes.IV);
                    // Create the streams used for decryption.    
                    using (MemoryStream ms = new MemoryStream(bArray.Skip(256).ToArray()))
                    {
                        // Create crypto stream    
                        using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            // Read crypto stream    
                            using (StreamReader reader = new StreamReader(cs))
                                plaintext = reader.ReadToEnd();
                        }
                    }
                }
                return plaintext;
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while doing decryption: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return "";
            }

        }

        #endregion

        #region log / execute service
        private async Task LogServicesData(string authorizationCode, string subServiceName, long FileProcessed, string ServiceId, bool IsManualExecution)
        {

            LogServiceRequest logServiceRequest = new LogServiceRequest
            {
                authorization_token = String.IsNullOrEmpty(ConfigDetails.Authorization_token) ? string.Empty : ConfigDetails.Authorization_token,
                mac_address = AppConstants.MACAddress,
                serial_number = AppConstants.SerialNumber,
                device_uuid = AppConstants.UUId,
                sub_service_authorization_code = authorizationCode,
                sub_service_name = subServiceName,
                current_user = Environment.UserName,
                executed = true,
                file_deleted = Convert.ToString(FileProcessed),
                IsManualExecution = IsManualExecution
            };

            var payload = Encrypt(Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(logServiceRequest))));

            var formContent = new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string>("authentication_token", String.IsNullOrEmpty(ConfigDetails.Authentication_token) ? string.Empty : ConfigDetails.Authentication_token) ,
                        new KeyValuePair<string, string>("payload", payload),
                        new KeyValuePair<string, string>("code_version", AppConstants.CodeVersion),
                    };

            var response = await client.PostAsync(AppConstants.EndPoints.LogServicesData, new FormUrlEncodedContent(formContent));
            if (response.IsSuccessStatusCode)
            {
                //timerLastUpdate.IsEnabled = false;
                var ExecuteNowContent = new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string>("execute_now", "false") ,
                    };
                var ExecuteNowResponse = await client.PutAsync(AppConstants.EndPoints.ExecuteNow + ServiceId + "/", new FormUrlEncodedContent(ExecuteNowContent));
                if (ExecuteNowResponse.IsSuccessStatusCode)
                {

                }
            }
            else
            {
                //timerLastUpdate.IsEnabled = false;
                //btnGetStarted_Click(btnGetStarted, null);
                MessageBox.Show("An error occurred in LogServicesData: ", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ExecuteServices(ServicesResponse servicesResponse)
        {
            try
            {

                if (IsServiceActive)
                {
                    lstCron.Clear();
                    foreach (var services in servicesResponse.Services)
                    {
                        foreach (var subservice in services.Subservices)
                        {
                            if (subservice.Sub_service_active)
                            {
                                //if(subservice.Sub_service_name == "web_session_protection")
                                //{
                                //    subservice.Execute_now = true;
                                //}
                                //var schedule = CrontabSchedule.Parse(subservice.Execution_period);
                                //var nextRunTime = schedule.GetNextOccurrence(DateTime.Now);
                                //lstCron.Add(subservice, nextRunTime);
                                if (subservice.Execute_now)
                                {
                                    ExecuteSubService(subservice);
                                    //MessageBox.Show("Executed Service: " + subservice.Name + " for user " + WindowsIdentity.GetCurrent().Name, "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                else
                                {
                                    var schedule = CrontabSchedule.Parse(subservice.Execution_period);
                                    DateTime nextRunTime = schedule.GetNextOccurrence(DateTime.Now);
                                    lstCron.Add(subservice, nextRunTime);
                                }
                            }
                        }
                    }
                    CronLastUpdate.Start();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while executing services: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void CronLastUpdate_Tick(object sender, EventArgs e)
        {
            try
            {
                Dictionary<SubservicesData, DateTime> serviceToRemove = new Dictionary<SubservicesData, DateTime>();
                if (lstCron.Count > 0)
                {
                    foreach (var key in lstCron)
                    {

                        SubservicesData SubservicesData = key.Key;
                        //MessageBox.Show(SubservicesData.Name.ToString() + " = " + key.Value.ToString());

                        //bool testCheck = false;
                        ////if (SubservicesData.Name.ToString() == "Web Session Protection")
                        //if (SubservicesData.Name.ToString() == "Web Tracking Protecting")
                        //{
                        //    testCheck = true;
                        //}
                        //if ((DateTime.Now.Date == key.Value.Date && DateTime.Now.Hour == key.Value.Hour && DateTime.Now.Minute == key.Value.Minute) || (testCheck == true))

                        if (DateTime.Now.Date == key.Value.Date && DateTime.Now.Hour == key.Value.Hour && DateTime.Now.Minute == key.Value.Minute)
                        {
                            ExecuteSubService(SubservicesData);
                            DateTime localDate = DateTime.Now.ToLocalTime();
                            txtUpdatedOn.Text = localDate.ToString();

                            var schedule = CrontabSchedule.Parse(SubservicesData.Execution_period);
                            DateTime nextRunTime = schedule.GetNextOccurrence(DateTime.Now);
                            serviceToRemove.Add(SubservicesData, nextRunTime);
                        }
                    }
                    foreach (var key in serviceToRemove)
                    {
                        lstCron[key.Key] = key.Value;
                    }
                    serviceToRemove.Clear();
                }
            }
            catch (Exception ex) { }
        }
        private void ExecuteSubService(SubservicesData subservices)
        {
            try
            {
                //MessageBox.Show(subservices.Sub_service_name);
                switch (subservices.Sub_service_name)
                {
                    case "dns_cache_protection":
                        FlushDNS(subservices);
                        break;
                    case "trash_data_protection":
                        ClearRecycleBin(subservices);
                        //MessageBox.Show("Trash Service Executed Successfully with V" + CodeVersion);
                        break;
                    case "windows_registry_protection":
                        if (IsAdmin)
                        { ClearWindowsRegistry(subservices); }
                        break;
                    case "free_storage_protection":
                        DiskCleaning(subservices);
                        break;
                    case "web_session_protection":
                        WebCookieCleaning(subservices);
                        break;
                    case "web_cache_protection":
                        WebCacheCleaning(subservices);
                        break;
                    case "web_tracking_protecting":
                        WebHistoryCleaning(subservices);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while executing subservices: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Services Implementation
        private void FlushDNS(SubservicesData subservices)
        {
            string flushDnsCmd = @"/C ipconfig /flushdns";
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo("cmd.exe", flushDnsCmd)

                };
                process.Start();

                //process.WaitForExit();
                KillCmd();
                Console.WriteLine(String.Format("Successfully Flushed DNS:'{0}'", flushDnsCmd), EventLogEntryType.Information);

                LogServicesData(subservices.Sub_service_authorization_code, subservices.Sub_service_name, 0, Convert.ToString(subservices.Id), subservices.Execute_now);

            }
            catch (Exception exp)
            {
                Console.WriteLine(String.Format("Failed to Flush DNS:'{0}' . Error:{1}", flushDnsCmd, exp.Message), EventLogEntryType.Error);
            }

        }
        private void ClearWindowsRegistry(SubservicesData subservices)
        {
            string user = Environment.UserDomainName + "\\" + Environment.UserName;
            RegistrySecurity rs = new RegistrySecurity();
            int CUCount = 0;
            int LMCount = 0;

            // Allow the current user to read and delete the key.
            rs.AddAccessRule(new RegistryAccessRule(user,
                RegistryRights.ReadKey | RegistryRights.WriteKey | RegistryRights.Delete,
                InheritanceFlags.None,
                PropagationFlags.None,
                AccessControlType.Allow));

            RegistryKey localMachine = Environment.Is64BitProcess == true ? RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64) : Registry.LocalMachine;//Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Mozilla\Mozilla Firefox\", true);

            var key = localMachine.OpenSubKey(@"SOFTWARE", true);
            key.SetAccessControl(rs);
            // Scan all subkeys under the defined key
            foreach (string subkeyName in key.GetSubKeyNames())
            {
                RegistryKey subkey = key.OpenSubKey(subkeyName);

                //Check if the subkey contains any values
                if (subkey.ValueCount == 0 && subkey.SubKeyCount == 0)
                {
                    // If the subkey does not contain any values, delete it
                    key.DeleteSubKeyTree(subkeyName);
                    Console.WriteLine("Deleted empty subkey: " + subkeyName);
                    LMCount++;
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
                            // If the value is invalid or obsolete, delete it
                            subkey.DeleteValue(valueName);
                            Console.WriteLine("Deleted invalid value: " + valueName);
                            LMCount++;
                        }
                    }
                }
            }

            RegistryKey CUkey = Registry.CurrentUser.OpenSubKey("SOFTWARE", true);
            CUkey.SetAccessControl(rs);
            // Scan all subkeys under the defined key
            foreach (string subkeyName in CUkey.GetSubKeyNames())
            {
                RegistryKey subkey = CUkey.OpenSubKey(subkeyName);

                // Check if the subkey contains any values
                if (subkey.ValueCount == 0 && subkey.SubKeyCount == 0)
                {
                    // If the subkey does not contain any values, delete it
                    CUkey.DeleteSubKeyTree(subkeyName);
                    Console.WriteLine("Deleted empty subkey: " + subkeyName);
                    CUCount++;
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
                            // If the value is invalid or obsolete, delete it
                            subkey.DeleteValue(valueName);
                            Console.WriteLine("Deleted invalid value: " + valueName);
                            CUCount++;
                        }
                    }
                }
            }

            Console.WriteLine("Total Regitry cleaned from current user", CUCount);
            Console.WriteLine("Total Regitry cleaned from current user", LMCount);
            int TotalCount = CUCount + LMCount;
            LogServicesData(subservices.Sub_service_authorization_code, subservices.Sub_service_name, TotalCount, Convert.ToString(subservices.Id), subservices.Execute_now);
        }
        public void KillCmd()
        {
            Array.ForEach(Process.GetProcessesByName("cmd"), x => x.Kill());
        }
        private void ClearRecycleBin(SubservicesData subservices)
        {
            long size = 0;
            int count = 0;

            Shell shell = new Shell();
            Folder recycleBin = shell.NameSpace(10);

            foreach (FolderItem2 item in recycleBin.Items())
            {
                //item.InvokeVerb("Delete");
                count++; // Increment counter for each deleted file
            }

            SHEmptyRecycleBin(IntPtr.Zero, null, RecycleFlag.SHERB_NOCONFIRMATION | RecycleFlag.SHERB_NOPROGRESSUI | RecycleFlag.SHERB_NOSOUND);
            KillCmd();

            LogServicesData(subservices.Sub_service_authorization_code, subservices.Sub_service_name, count, Convert.ToString(subservices.Id), subservices.Execute_now);
        }
        private void DiskCleaning(SubservicesData subservices)
        {
            string memoryCleaning = @"cipher /w:c:\";
            var process = new Process
            {
                StartInfo = new ProcessStartInfo("cmd.exe", memoryCleaning)
            };
            process.Start();
            KillCmd();

            LogServicesData(subservices.Sub_service_authorization_code, subservices.Sub_service_name, 0, Convert.ToString(subservices.Id), subservices.Execute_now);
        }
        private void WebCookieCleaning(SubservicesData subservices) // eventbased - all browser - Chrome, Mozilla, Edge, IE, BraveBrowser.
        {
            string SubServiceId = Convert.ToString(subservices.Id);

            CheckWhiteListDomains(SubServiceId, subservices.Sub_service_authorization_code, subservices.Sub_service_name, subservices.Execute_now);
        }
        private async void CheckWhiteListDomains(string SubServiceId, string Sub_service_authorization_code, string Sub_service_name, bool ExecuteNow)
        {
            try
            {
                whitelistedDomain.Clear();
                var response = await client.GetAsync(AppConstants.EndPoints.WhiteListDomains + SubServiceId + "/");
                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var responseData = JsonConvert.DeserializeObject<WhiteListDomainResponse>(responseString);
                    if (responseData.device_domains.Count > 0)
                    {
                        foreach (var domain in responseData.device_domains)
                        {
                            whitelistedDomain.Add("'%" + domain.domain_name + "%'");
                        }

                    }
                    if (responseData.org_domains.Count > 0)
                    {
                        foreach (var domain in responseData.org_domains)
                        {
                            whitelistedDomain.Add("'%" + domain.domain_name + "%'");
                        }
                    }

                    //  MessageBox.Show("Total " + whitelistedDomain.Count.ToString() + " whitelistedDomain");
                    int ChromeCount = ClearChromeCookie();
                    int FireFoxCount = ClearFirefoxCookies();
                    int EdgeCount = ClearEdgeCookies();
                    int OperaCount = ClearOperaCookies();

                    int TotalCount = ChromeCount + FireFoxCount + EdgeCount + OperaCount;

                    LogServicesData(Sub_service_authorization_code, Sub_service_name, TotalCount, SubServiceId, ExecuteNow);
                }
                else
                    MessageBox.Show("An error occurred while fatching whitelist domains: ", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            }
            catch
            {
                MessageBox.Show("An error occurred while fatching whitelist domains: ", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private int ClearChromeCookie()
        {

            int TotalCount = 0;
            int bCount = IsBrowserOpen("chrome");
            //Process[] chromeInstances = Process.GetProcessesByName("chrome");            
            if (bCount == 0)
            {
                string chromeProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Google\Chrome\User Data\";

                List<string> profiles = new List<string>();
                string defaultProfilePath = Path.Combine(chromeProfilePath, "Default");
                if (Directory.Exists(defaultProfilePath))
                {
                    profiles.Add(defaultProfilePath);
                }
                if (Directory.Exists(chromeProfilePath))
                {
                    string[] profileDirectories = Directory.GetDirectories(chromeProfilePath, "Profile *");

                    foreach (string profileDir in profileDirectories)
                    {
                        string profilePath = Path.Combine(chromeProfilePath, profileDir);
                        profiles.Add(profilePath);
                    }
                }

                foreach (var profile in profiles)
                {
                    if (Directory.Exists(profile))
                    {
                        string CookiesPath = Path.Combine(profile, "Network\\Cookies");
                        if (File.Exists(CookiesPath))
                        {
                            using (SQLiteConnection connection = new SQLiteConnection("Data Source=" + CookiesPath))
                            {
                                ///MessageBox.Show("Chrome path : " + chromeProfilePath.ToString() + " || Cookies Path " + CookiesPath.ToString());
                                connection.Open();
                                using (SQLiteCommand command = connection.CreateCommand())
                                {
                                    string query = "DELETE FROM Cookies";
                                    if (whitelistedDomain.Count > 0)
                                    {
                                        query += " WHERE ";
                                        foreach (string domain in whitelistedDomain)
                                        {
                                            query += " host_key not like " + domain + " And";
                                        }
                                        query = query.Remove(query.Length - 4);
                                    }
                                    command.CommandText = query;
                                    command.Prepare();
                                    TotalCount += command.ExecuteNonQuery();
                                }
                                connection.Close();
                                //MessageBox.Show("Cookies done from Chrome");
                            }
                        }
                    }
                }

                // Display the count of items deleted
                Console.WriteLine("Total number of cookies deleted: " + TotalCount);
            }
            return TotalCount;
        }
        public int ClearFirefoxCookies()
        {
            int TotalCount = 0;
            int bCount = IsBrowserOpen("firefox");
            //Process[] firefoxInstances = Process.GetProcessesByName("firefox");
            if (bCount == 0)
            {
                string firefoxProfilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mozilla", "Firefox", "Profiles");
                if (Directory.Exists(firefoxProfilePath))
                {
                    string[] profileDirectories = Directory.GetDirectories(firefoxProfilePath);

                    foreach (string profileDir in profileDirectories)
                    {
                        string cookiesFilePath = Path.Combine(profileDir, "cookies.sqlite");
                        if (File.Exists(cookiesFilePath))
                        {
                            using (SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};", cookiesFilePath)))
                            {
                                connection.Open();
                                using (SQLiteCommand command = connection.CreateCommand())
                                {
                                    string query = "DELETE FROM moz_cookies";
                                    if (whitelistedDomain.Count > 0)
                                    {
                                        query += " WHERE ";
                                        foreach (string domain in whitelistedDomain)
                                        {
                                            query += "  host not like " + domain + " And";
                                        }
                                        query = query.Remove(query.Length - 4);
                                    }
                                    command.CommandText = query;
                                    TotalCount += command.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
            }
            Console.WriteLine("{0} Firefox cookies deleted", TotalCount);

            return TotalCount;
        }
        public void ClearIECookies()
        {
            //MessageBox.Show("Second");
            foreach (string domain in whitelistedDomain)
            {
                // Add domains to the PerSite privacy settings
                Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Internet Settings\PerSiteCookieDecision", domain, 1, RegistryValueKind.DWord);
            }

            // Clear cookies of Internet Explorer
            Process.Start("rundll32.exe", "InetCpl.cpl,ClearMyTracksByProcess 2");

            foreach (string domain in whitelistedDomain)
            {
                // Add domains to the PerSite privacy settings
                Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Internet Settings\PerSiteCookieDecision", domain, 0, RegistryValueKind.DWord);
            }
        }
        public int ClearEdgeCookies()
        {

            int TotalCount = 0;
            int bCount = IsBrowserOpen("msedge");
            //Process[] msedgeInstances = Process.GetProcessesByName("msedge");
            if (bCount == 0)
            {
                List<string> profiles = new List<string>();
                string edgeProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Microsoft\Edge\User Data\";
                string defaultProfilePath = Path.Combine(edgeProfilePath, "Default");
                if (Directory.Exists(defaultProfilePath))
                {
                    profiles.Add(defaultProfilePath);
                }

                if (Directory.Exists(edgeProfilePath))
                {
                    string[] profileDirectories = Directory.GetDirectories(edgeProfilePath, "Profile *");

                    foreach (string profileDir in profileDirectories)
                    {
                        string profilePath = Path.Combine(edgeProfilePath, profileDir);
                        profiles.Add(profilePath);
                    }
                }

                foreach (var profile in profiles)
                {
                    if (Directory.Exists(profile))
                    {
                        string cookiePath = Path.Combine(profile, "Network\\Cookies");
                        if (File.Exists(cookiePath))
                        {
                            using (SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};", cookiePath)))
                            {
                                // Clear the cookies by deleting all records from the cookies table
                                connection.Open();
                                using (SQLiteCommand cmd = connection.CreateCommand())
                                {
                                    string query = "DELETE FROM Cookies";
                                    if (whitelistedDomain.Count > 0)
                                    {
                                        query += " WHERE ";
                                        foreach (string domain in whitelistedDomain)
                                        {
                                            query += " host_key not like " + domain + " And";
                                        }
                                        query = query.Remove(query.Length - 4);
                                    }
                                    cmd.CommandText = query;
                                    cmd.Prepare();
                                    TotalCount += cmd.ExecuteNonQuery();

                                }
                                connection.Close();
                            }
                        }
                    }
                }
                Console.WriteLine($"Deleted {TotalCount} cookies.");
            }
            return TotalCount;
        }
        public int ClearOperaCookies()
        {
            int TotalCount = 0;
            int bCount = IsBrowserOpen("opera");
            //Process[] msedgeInstances = Process.GetProcessesByName("opera");
            if (bCount == 0)
            {
                var str = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var cookiePath = str + "\\Opera Software\\Opera Stable\\Network\\Cookies";
                if (File.Exists(cookiePath))
                {
                    using (SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};", cookiePath)))
                    {
                        // Clear the cookies by deleting all records from the cookies table
                        connection.Open();
                        using (SQLiteCommand cmd = connection.CreateCommand())
                        {
                            string query = "DELETE FROM Cookies";
                            if (whitelistedDomain.Count > 0)
                            {
                                query += " WHERE ";
                                foreach (string domain in whitelistedDomain)
                                {
                                    query += " host_key not like " + domain + " And";
                                }
                                query = query.Remove(query.Length - 4);
                            }
                            cmd.CommandText = query;
                            cmd.Prepare();
                            TotalCount = cmd.ExecuteNonQuery();
                            Console.WriteLine($"Deleted {TotalCount} cookies.");
                        }
                        connection.Close();
                    }
                }
            }
            return TotalCount;
        }
        private void WebHistoryCleaning(SubservicesData subservices)
        {
            int ChromeCount = ClearChromeHistory();
            int FireFoxCount = ClearFireFoxHistory();
            int EdgeCount = ClearEdgeHistory();
            int OperaCount = ClearOperaHistory();

            //int TotalCount = EdgeCount + ChromeCount;
            int TotalCount = ChromeCount + FireFoxCount + EdgeCount + OperaCount;

            LogServicesData(subservices.Sub_service_authorization_code, subservices.Sub_service_name, TotalCount, Convert.ToString(subservices.Id), subservices.Execute_now);
        }



        static int IsBrowserOpen(string browser)
        {
            int bCnt = 0;
            Process[] chromeProcesses = Process.GetProcessesByName(browser);
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
            // MessageBox.Show("User1 " + WindowsIdentity.GetCurrent().Name.ToUpper().ToString() + " User2 " + test + " Count = " + bCnt + " For Browser " + browser);
            return bCnt;
        }

        static string GetProcessOwner2(int processId)
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

        public int ClearChromeHistory()
        {
            int TotalCount = 0;


            int bCount = IsBrowserOpen("chrome");
            //MessageBox.Show(bCount.ToString());
            //Process[] chromeInstances = Process.GetProcesses("chrome");
            if (bCount == 0)
            {
                //MessageBox.Show("Chrome History Deletion Started");
                List<string> profiles = new List<string>();
                string chromeProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Google\Chrome\User Data\";
                string defaultProfilePath = Path.Combine(chromeProfilePath, "Default");
                if (Directory.Exists(defaultProfilePath))
                {
                    profiles.Add(defaultProfilePath);
                }
                if (Directory.Exists(chromeProfilePath))
                {
                    string[] profileDirectories = Directory.GetDirectories(chromeProfilePath, "Profile *");

                    foreach (string profileDir in profileDirectories)
                    {
                        string profilePath = Path.Combine(chromeProfilePath, profileDir);
                        profiles.Add(profilePath);
                    }
                }
                foreach (var profile in profiles)
                {
                    if (Directory.Exists(profile))
                    {
                        string historyPath = Path.Combine(profile, "History");
                        if (File.Exists(historyPath))
                        {
                            using (SQLiteConnection connection = new SQLiteConnection("Data Source=" + historyPath + ";Version=3;New=False;Compress=True;"))
                            {
                                connection.Open();
                                using (SQLiteCommand command = new SQLiteCommand("DELETE FROM urls", connection))
                                {
                                    TotalCount += command.ExecuteNonQuery();
                                }
                                connection.Close();
                                //MessageBox.Show("Chrome History Deleted Sucessfully");
                            }
                        }
                    }
                }

                Console.WriteLine("Total number of history deleted: " + TotalCount);
            }
            return TotalCount;
        }
        public int ClearFireFoxHistory()
        {
            int TotalCount = 0;


            int bCount = IsBrowserOpen("firefox");


            //Process[] firefoxInstances = Process.GetProcessesByName("firefox");
            if (bCount == 0)
            {
                try
                {
                    string firefoxProfilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mozilla", "Firefox", "Profiles");
                    if (Directory.Exists(firefoxProfilePath))
                    {
                        string[] profileDirectories = Directory.GetDirectories(firefoxProfilePath);

                        foreach (string profileDir in profileDirectories)
                        {
                            string placesFilePath = Path.Combine(profileDir, "places.sqlite");
                            if (File.Exists(placesFilePath))
                            {
                                using (SQLiteConnection connection = new SQLiteConnection($"Data Source={placesFilePath};Version=3;"))
                                {
                                    connection.Open();

                                    using (SQLiteCommand command = connection.CreateCommand())
                                    {
                                        command.CommandText = "DELETE FROM moz_places";
                                        TotalCount += command.ExecuteNonQuery();
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle any exceptions that occur while clearing Firefox history
                }
            }
            return TotalCount;
        }
        public void ClearIEHitory()
        {
            // MessageBox.Show("Third");
            // Get current number of history items
            int currentCount = 0;
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Internet Explorer\TypedURLs"))
            {
                if (key != null)
                {
                    currentCount = key.ValueCount;
                }
            }

            // Clear browsing history of Internet Explorer
            Process.Start("rundll32.exe", "InetCpl.cpl,ClearMyTracksByProcess 1");

            // Get new number of history items
            int newCount = 0;
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Internet Explorer\TypedURLs"))
            {
                if (key != null)
                {
                    newCount = key.ValueCount;
                }
            }

            // Calculate number of items cleared
            int countCleared = currentCount - newCount;

        }
        public int ClearEdgeHistory()
        {
            //MessageBox.Show("History Start 1");
            int TotalCount = 0;


            int bCount = IsBrowserOpen("msedge");


            //Process[] msedgeInstances = Process.GetProcessesByName("msedge");
            if (bCount == 0)
            {
                // Connect to the Edge History database
                //string historyPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Microsoft\Edge\User Data\Default\History";
                List<string> profiles = new List<string>();
                string edgeProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Microsoft\Edge\User Data\";
                string defaultProfilePath = Path.Combine(edgeProfilePath, "Default");
                if (Directory.Exists(defaultProfilePath))
                {
                    profiles.Add(defaultProfilePath);
                }
                //MessageBox.Show("History Start 2 - " + edgeProfilePath);
                if (Directory.Exists(edgeProfilePath))
                {
                    string[] profileDirectories = Directory.GetDirectories(edgeProfilePath, "Profile *");

                    foreach (string profileDir in profileDirectories)
                    {
                        string profilePath = Path.Combine(edgeProfilePath, profileDir);
                        profiles.Add(profilePath);
                    }
                }



                //MessageBox.Show("History Start 3 - " + profiles.Count.ToString());
                foreach (var profile in profiles)
                {
                    if (Directory.Exists(profile))
                    {
                        string historyPath = Path.Combine(profile, "History");

                        if (File.Exists(historyPath))
                        {
                            string connectionString = "Data Source=" + historyPath + ";Version=3;New=False;Compress=True;";
                            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                            {
                                connection.Open();
                                try
                                {
                                    // Perform your database operations here
                                    // Delete all browsing history records
                                    using (SQLiteCommand command = new SQLiteCommand("DELETE FROM urls;", connection))
                                    {
                                        TotalCount += command.ExecuteNonQuery();

                                    }
                                }
                                catch (Exception ex)
                                {
                                    // Handle any exceptions that occur during the transaction
                                    Console.WriteLine("An error occurred: " + ex.Message);

                                }
                                finally
                                {
                                    connection.Close();
                                }
                            }
                        }
                    }
                }


            }
            return TotalCount;
            //MessageBox.Show("History Start 3 - Done");
            Console.WriteLine($"Deleted {TotalCount} browsing history items.");
        }


        public int ClearOperaHistory()
        {
            int TotalCount = 0;


            int bCount = IsBrowserOpen("opera");

            //Process[] OperaInstances = Process.GetProcessesByName("opera");
            if (bCount == 0)
            {
                // Set the path to the Opera profile directory
                string historyPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Opera Software\Opera Stable\";
                if (Directory.Exists(historyPath))
                {
                    // Connect to the history database file
                    using (SQLiteConnection connection = new SQLiteConnection("Data Source=" + historyPath + "History"))
                    {
                        connection.Open();

                        // Execute the SQL command to delete the browsing history
                        using (SQLiteCommand command = new SQLiteCommand("DELETE FROM urls", connection))
                        {
                            TotalCount = command.ExecuteNonQuery();
                        }
                    }
                }

                Console.WriteLine("Deleted {0} history records.", TotalCount);
            }
            return TotalCount;
        }
        private void WebCacheCleaning(SubservicesData subservices)
        {
            long ChromeCount = ClearChromeCache();
            long FireFoxCount = ClearFirefoxCache();
            long EdgeCount = ClearEdgeCache();
            long OperaCount = ClearOperaCache();

            long TotalSize = ChromeCount + FireFoxCount + EdgeCount + OperaCount;
            LogServicesData(subservices.Sub_service_authorization_code, subservices.Sub_service_name, TotalSize, Convert.ToString(subservices.Id), subservices.Execute_now);
        }
        private long ClearChromeCache()
        {
            int TotalCount = 0;
            long TotalSize = 0;


            int bCount = IsBrowserOpen("chrome");


            //Process[] chromeInstances = Process.GetProcessesByName("chrome");
            if (bCount == 0)
            {
                List<string> profiles = new List<string>();
                string chromeProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Google\Chrome\User Data\";
                string defaultProfilePath = Path.Combine(chromeProfilePath, "Default");
                if (Directory.Exists(defaultProfilePath))
                {
                    profiles.Add(defaultProfilePath);
                }
                if (Directory.Exists(chromeProfilePath))
                {
                    string[] profileDirectories = Directory.GetDirectories(chromeProfilePath, "Profile *");

                    foreach (string profileDir in profileDirectories)
                    {
                        string profilePath = Path.Combine(chromeProfilePath, profileDir);
                        profiles.Add(profilePath);
                    }
                }
                foreach (var profile in profiles)
                {
                    if (Directory.Exists(profile))
                    {
                        string CachePath = Path.Combine(profile, "Cache\\Cache_Data");
                        if (Directory.Exists(CachePath))
                        {
                            // Clear the cache folder
                            foreach (string file in Directory.GetFiles(CachePath))
                            {
                                try
                                {
                                    TotalSize += file.Length;
                                    File.Delete(file);
                                    TotalCount++;
                                }
                                catch (IOException) { } // handle any exceptions here
                            }

                        }
                    }
                    else
                    {
                        Console.WriteLine("Chrome Cache file not found.");
                    }
                }
                Console.WriteLine($"Total {TotalSize} files cleared from Chrome cache.");
            }
            return TotalSize;
        }
        static long ClearFirefoxCache()
        {
            int TotalCount = 0;
            long TotalSize = 0;
            int bCount = IsBrowserOpen("firefox");
            //Process[] firefoxInstances = Process.GetProcessesByName("firefox");
            if (bCount == 0)
            {
                string firefoxProfilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mozilla", "Firefox", "Profiles");
                if (Directory.Exists(firefoxProfilePath))
                {
                    string[] profileDirectories = Directory.GetDirectories(firefoxProfilePath);

                    foreach (string profileDir in profileDirectories)
                    {
                        string[] cacheFolders = { "cache2", "shader-cache", "browser-extension-data", "startupCache", "thumbnails" };
                        foreach (string folder in cacheFolders)
                        {
                            string cachePath = Path.Combine(profileDir, folder);
                            if (Directory.Exists(cachePath))
                            {
                                foreach (string file in Directory.GetFiles(cachePath))
                                {
                                    try
                                    {
                                        FileInfo info = new FileInfo(file);
                                        TotalCount++;
                                        TotalSize += info.Length;
                                        File.Delete(file);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error deleting file: {ex.Message}");
                                    }
                                }
                                Console.WriteLine($"Deleted {TotalCount} file of total {TotalSize} bytes of {folder} cache");
                            }
                            else
                            {
                                Console.WriteLine($"{folder} cache folder not found");
                            }
                        }
                    }
                }
                Console.WriteLine("{0} Firefox cache items deleted", TotalSize);
            }
            return TotalSize;
        }
        public long ClearEdgeCache()
        {
            int TotalCount = 0;
            long TotalSize = 0;
            int bCount = IsBrowserOpen("msedge");
            //Process[] msedgeInstances = Process.GetProcessesByName("msedge");
            if (bCount == 0)
            {
                // Connect to the Edge cache database
                //string cachePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Microsoft\Edge\User Data\Default\Cache\Cache_Data";
                List<string> profiles = new List<string>();
                string edgeProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Microsoft\Edge\User Data\";
                string defaultProfilePath = Path.Combine(edgeProfilePath, "Default");
                if (Directory.Exists(defaultProfilePath))
                {
                    profiles.Add(defaultProfilePath);
                }
                if (Directory.Exists(edgeProfilePath))
                {
                    string[] profileDirectories = Directory.GetDirectories(edgeProfilePath, "Profile *");

                    foreach (string profileDir in profileDirectories)
                    {
                        string profilePath = Path.Combine(edgeProfilePath, profileDir);
                        profiles.Add(profilePath);
                    }
                }
                foreach (var profile in profiles)
                {
                    if (File.Exists(profile))
                    {
                        string cachePath = Path.Combine(profile, "Cache\\Cache_Data");
                        if (File.Exists(cachePath))
                        {
                            // Clear the cache folder
                            foreach (string file in Directory.GetFiles(cachePath))
                            {
                                try
                                {
                                    TotalSize += file.Length;
                                    File.Delete(file);
                                    TotalCount++;
                                }
                                catch (IOException) { } // handle any exceptions here
                            }
                        }
                    }
                }
                Console.WriteLine($"Total {TotalSize} files cleared from Chrome cache.");
            }
            return TotalSize;
        }
        public void ClearIECache()
        {
            // MessageBox.Show("4");
            // Get current number of cache items
            int currentCount = 0;
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings\Cache"))
            {
                if (key != null)
                {
                    currentCount = key.ValueCount;
                }
            }

            // Clear cache of Internet Explorer
            Process.Start("rundll32.exe", "InetCpl.cpl,ClearMyTracksByProcess 8");

            // Get new number of cache items
            int newCount = 0;
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings\Cache"))
            {
                if (key != null)
                {
                    newCount = key.ValueCount;
                }
            }

            // Calculate number of items cleared
            int countCleared = currentCount - newCount;
            Console.WriteLine($"Deleted {countCleared} cache cleared");
        }
        public long ClearOperaCache()
        {
            int TotalCount = 0;
            long TotalSize = 0;
            int bCount = IsBrowserOpen("opera");
            // Process[] OperaInstances = Process.GetProcessesByName("opera");
            if (bCount == 0)
            {
                // Set the path to the Opera profile directory
                string cachePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Opera Software\Opera Stable\Cache\Cache_Data";
                if (Directory.Exists(cachePath))
                {
                    // Delete all files in the cache directory
                    foreach (string file in Directory.GetFiles(cachePath))
                    {
                        TotalSize += file.Length;
                        File.Delete(file);
                        TotalCount++;
                    }
                }
                Console.WriteLine("Deleted {0} files from the cache.", TotalSize);
            }
            return TotalSize;
        }

        #endregion

        #region Application close / minimize
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            //Application.Current.Shutdown();
            thisWindow.WindowState = WindowState.Minimized;
            thisWindow.ShowInTaskbar = false;
            thisWindow.Visibility = Visibility.Hidden;
            icon.ShowBalloonTip(2000);
        }
        private async void Icon_Click(object sender, EventArgs e)
        {

            thisWindow.Visibility = Visibility.Visible;
            thisWindow.WindowState = WindowState.Normal;
            thisWindow.ShowInTaskbar = true;
            thisWindow.Focus();
            Activate();
            await GetDeviceDetails();
        }
        #endregion

        #region uninstall
        public void IsUninstallFlagUpdated()
        {
            if (!IsUnInstallFlag)
            {
                string displayName = "FDS";
                string uninstallKeyPath = null;

                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"))
                {
                    if (key != null)
                    {

                        foreach (string subKeyName in key.GetSubKeyNames())
                        {
                            //MessageBox.Show("Uninstall subKeyName " + subKeyName);
                            using (RegistryKey subKey = key.OpenSubKey(subKeyName))
                            {
                                object displayNameValue = subKey.GetValue("DisplayName");
                                if (displayNameValue != null && displayNameValue.ToString() == displayName)
                                {
                                    uninstallKeyPath = subKey.Name;
                                    //MessageBox.Show("ProductKey unistall" + uninstallKeyPath);
                                    IsUnInstallFlag = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (uninstallKeyPath != null)
                {
                    uninstallKeyPath = uninstallKeyPath.Replace("HKEY_LOCAL_MACHINE\\", "");
                    // Modify the registry key to prevent uninstallation
                    using (RegistryKey uninstallKey = Registry.LocalMachine.OpenSubKey(uninstallKeyPath, true))
                    {
                        if (uninstallKey != null)
                        {
                            //MessageBox.Show("Modified unistall" + uninstallKey);
                            uninstallKey.SetValue("SystemComponent", 1, RegistryValueKind.DWord);
                        }
                    }
                }
            }
        }
        private async void btnUninstall_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAdmin)
            {
                MessageBox.Show("You Can't Uninstall, Please Contact Admin to Uninstall.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var formContent = new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("mac_address", AppConstants.MACAddress),
                new KeyValuePair<string, string>("serial_number", AppConstants.SerialNumber),
                new KeyValuePair<string, string>("current_user", Environment.UserName),
                new KeyValuePair<string, string>("device_uuid", AppConstants.UUId)
            };
            var response = await client.PostAsync(AppConstants.EndPoints.UninstallDevice, new FormUrlEncodedContent(formContent));
            var responseString = await response.Content.ReadAsStringAsync();
            var responseData = JsonConvert.DeserializeObject<DTO.Responses.ResponseData>(responseString);
            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show("Uninstall request has been raised successfully!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                isUninstallRequestRaised = true;
                btnUninstall.ToolTip = "Your uninstall request is pending.";
                btnUninstall.Foreground = System.Windows.Media.Brushes.Gold;
                UninstallResponseTimer.Start();
            }
            else
            {
                MessageBox.Show(responseData.error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void UninstallResponseTimer_Tick(object sender, EventArgs e)
        {
            UninstallProgram();
        }
        private async Task UninstallProgram()
        {
            var formContent = new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("mac_address", AppConstants.MACAddress),
                new KeyValuePair<string, string>("serial_number", AppConstants.SerialNumber),
                new KeyValuePair<string, string>("current_user", Environment.UserName),
                new KeyValuePair<string, string>("device_uuid", AppConstants.UUId)
            };
            var response = await client.PostAsync(AppConstants.EndPoints.UninstallCheck, new FormUrlEncodedContent(formContent));
            var responseString = await response.Content.ReadAsStringAsync();
            var responseData = JsonConvert.DeserializeObject<DTO.Responses.ResponseData>(responseString);
            if (response.IsSuccessStatusCode)
            {
                /// 1- Pending, 2 - Approved, 3 - Rejected
                try
                {
                    if (responseData.Data == "1")
                    {
                        btnUninstall.ToolTip = "Your uninstall request is pending.";
                        btnUninstall.Foreground = System.Windows.Media.Brushes.Gold;

                    }
                    else if (responseData.Data == "3")
                    {
                        UninstallResponseTimer.Stop();
                        btnUninstall.ToolTip = "Your uninstall request has been declined!";
                        //MessageBox.Show("Uninstall request has been rejected! Please contact your Administrator", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                        btnUninstall.Foreground = System.Windows.Media.Brushes.Red;
                    }
                    else
                    {

                        encryptOutPutFile = basePathEncryption + @"\Main";
                        if (File.Exists(encryptOutPutFile))
                        {
                            File.Delete(encryptOutPutFile);
                            ConfigDataClear();
                        }
                        btnUninstall.ToolTip = "Your uninstall request has been approved! ";
                        btnUninstall.Foreground = System.Windows.Media.Brushes.DarkGreen;
                        //MessageBox.Show("Uninstall request has been approved! Will process your request soon", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                        string applicationName = "FDS";

                        // Get the uninstall registry key for the application
                        RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\");
                        if (key != null)
                        {
                            foreach (string subKeyName in key.GetSubKeyNames())
                            {
                                using (RegistryKey subKey = key.OpenSubKey(subKeyName))
                                {
                                    object displayNameValue = subKey.GetValue("DisplayName");
                                    if (displayNameValue != null && displayNameValue.ToString() == applicationName)
                                    {
                                        // Get the uninstall string from the registry key
                                        string uninstallString = subKey.GetValue("UninstallString").ToString();

                                        //MessageBox.Show(uninstallString, "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                                        cleanSystem();
                                        Process.Start("cmd.exe", "/C " + uninstallString);
                                        Process[] processes = Process.GetProcessesByName(applicationName);

                                        foreach (Process process in processes)
                                        {
                                            try
                                            {
                                                process.Kill();
                                                //Console.WriteLine($"Process with ID {process.Id} killed successfully.");
                                            }
                                            catch (Exception ex)
                                            {
                                                //Console.WriteLine($"Error killing process: {ex.Message}");
                                            }
                                        }
                                        // Delete the registry key for the application
                                        key.DeleteSubKeyTree(applicationName);

                                        MessageBox.Show("Application uninstalled successfully", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                                        //KillCmd();
                                    }
                                }
                            }

                        }

                        else
                        {
                            // The application was not found in the registry
                            Console.WriteLine("Application not found.");
                            MessageBox.Show("Application not found", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    MessageBox.Show("An error while uninstalling application: ", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show(responseData.error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void cleanSystem()
        {
            UninstallResponseTimer.Stop();
            timerLastUpdate.Stop();
            timerDeviceLogin.Stop();
            CredDelete(AppConstants.KeyPrfix + "Key1", 1, 0);
            CredDelete(AppConstants.KeyPrfix + "Key2", 1, 0);
            CredDelete(AppConstants.KeyPrfix + "D", 1, 0);
            CredDelete(AppConstants.KeyPrfix + "P", 1, 0);
            CredDelete(AppConstants.KeyPrfix + "Q", 1, 0);
            CredDelete(AppConstants.KeyPrfix + "DP", 1, 0);
            CredDelete(AppConstants.KeyPrfix + "DQ", 1, 0);
            CredDelete(AppConstants.KeyPrfix + "Exponent", 1, 0);
            CredDelete(AppConstants.KeyPrfix + "InverseQ", 1, 0);
            CredDelete(AppConstants.KeyPrfix + "Modulus", 1, 0);
            CredDelete(AppConstants.KeyPrfix + "InverseQ", 1, 0);
            CredDelete(AppConstants.KeyPrfix + "Authentication_token", 1, 0);
            CredDelete(AppConstants.KeyPrfix + "Authorization_token", 1, 0);

            ///string keyPath = @"SOFTWARE\FDS";
            //DeleteRegistryKey(keyPath);

            encryptOutPutFile = basePathEncryption + @"\Main";
            if (File.Exists(encryptOutPutFile))
            {
                File.Delete(encryptOutPutFile);
            }

        }
        //public static void DeleteRegistryKey(string keyPath)
        //{
        //    Registry.LocalMachine.DeleteSubKeyTree(keyPath);
        //}
        #endregion

        #region AutoUpdate
        private async void AutoUpdate()
        {
            var formContent = new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string>("serial_number", AppConstants.SerialNumber),
                        new KeyValuePair<string, string>("mac_address",AppConstants.MACAddress),
                        new KeyValuePair<string, string>("device_uuid", AppConstants.UUId),
                        new KeyValuePair<string, string>("code_version", AppConstants.CodeVersion)
                    };
            var response = await client.PostAsync(AppConstants.EndPoints.AutoUpdate, new FormUrlEncodedContent(formContent));
            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                AutoUpdateResponse UpdateResponse = JsonConvert.DeserializeObject<AutoUpdateResponse>(responseString);

                var getresponse = await client.GetAsync(AppConstants.EndPoints.AutoUpdate + "?token=" + UpdateResponse.msg);
                if (getresponse.IsSuccessStatusCode)
                {
                    var getresponseString = await getresponse.Content.ReadAsStringAsync();
                    AutoUpdateResponse UpdateGetResponse = JsonConvert.DeserializeObject<AutoUpdateResponse>(getresponseString);
                    string Url = UpdateGetResponse.msg;

                    if (!Directory.Exists(TempPath))
                        Directory.CreateDirectory(TempPath);

                    this.DownloadFile(Url, TempPath + "FDS.msi");
                }
            }
        }
        private void DownloadFile(string url, string temporaryMSIPath)
        {
            using (var client = new WebClient())
            {
                try
                {
                    client.DownloadFile(url, temporaryMSIPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error downloading file: " + ex.Message);
                }
            }
            try
            {
                if (File.Exists(TempPath + "FDS.msi"))
                {
                    File.Copy(Directory.GetCurrentDirectory() + "\\AutoUpdate.exe", TempPath + "AutoUpdate.exe", true);
                    string AutoUpdateExePath = TempPath + "AutoUpdate.exe";
                    Process.Start(AutoUpdateExePath);
                }
                //MessageBox.Show("Autoupdate start");
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

        }
        #endregion

        #region unwanted code for now
        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            //imgServiceClearUnCheck.Visibility = true ? Visibility.Hidden : Visibility.Visible;
            //imgServiceClearCheck.Visibility = imgServiceClearUnCheck.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
            LoadMenu(Screens.ServiceClear);
        }
        private void btnStatus_Click(object sender, RoutedEventArgs e)
        {
            LoadMenu(Screens.Landing);
        }
        private void lblReauthenticate_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            LoadMenu(Screens.Popup);
        }
        private void btnDeLink_Click(object sender, RoutedEventArgs e)
        {
            DeviceReauth();
            LoadMenu(Screens.ServiceClear);
        }
        private void frmFDSMain_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            btnClose_Click(btnClose, null);
        }

        private void header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void btnGetOTP_Click(object sender, RoutedEventArgs e)
        {
        }
        private void txtOTP_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[0-9]+");
            if (!regex.IsMatch(e.Text))
            {
                e.Handled = true;
            }
        }

        private void btnViewServices_Click(object sender, RoutedEventArgs e)
        {
            LoadMenu(Screens.DataProtection);
        }
        private void txtHome_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            LoadMenu(Screens.Landing);
        }

        public void ConfigDataClear()
        {
            ConfigDetails.Key1 = string.Empty;
            ConfigDetails.Key2 = string.Empty;
            ConfigDetails.Authentication_token = string.Empty;
            ConfigDetails.Authorization_token = string.Empty;
            ConfigDetails.Modulus = string.Empty;
            ConfigDetails.Exponent = string.Empty;
            ConfigDetails.D = string.Empty;
            ConfigDetails.DP = string.Empty;
            ConfigDetails.DQ = string.Empty;
            ConfigDetails.Q = string.Empty;
            ConfigDetails.InverseQ = string.Empty;
        }

        public void updateTrayIcon()
        {
            try
            {
                icon.Icon = new System.Drawing.Icon(Directory.GetParent(System.Environment.CurrentDirectory).Parent.Parent.FullName + "\\Assets\\LogoFDSXL_disable.ico");
                icon.Visible = true;
                icon.BalloonTipText = "The app has been disabled. Click the tray icon to show.";
                icon.BalloonTipTitle = "FDS (Scanning & Cleaning)";
                icon.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
                icon.ShowBalloonTip(2000);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                MessageBox.Show("An error occurred in updateTrayIcon: ", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion
    }
}