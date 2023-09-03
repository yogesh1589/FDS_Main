using FDS.API_Service;
using FDS.Common;
using FDS.DTO.Requests;
using FDS.DTO.Responses;
using FDS.Factories;
using FDS.Logging;
using FDS.Runners;
using FDS.SingleTon;
using Microsoft.Win32;
using NCrontab;
using Newtonsoft.Json;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WpfAnimatedGif;
using Image = System.Drawing.Image;

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
        DispatcherTimer timerEventBasedService;

        int TotalSeconds = Common.AppConstants.TotalKeyActivationSeconds;
        System.Windows.Forms.NotifyIcon icon;
        public DeviceResponse DeviceResponse { get; private set; }
        public Window thisWindow { get; }
        public HttpClient client { get; }
        public QRCodeResponse QRCodeResponse { get; private set; }

        public static RSACryptoServiceProvider RSADevice { get; set; }
        public static RSACryptoServiceProvider RSAServer { get; set; }

        private bool isLoggedIn { get; set; }

        bool IsAdmin => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        public static string BaseDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);


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
        bool deviceDeletedFlag = false;
        bool showMessageBoxes = true;//true for staging and false for production
        ApiService apiService = new ApiService();
        public static byte[] EncKey { get; set; }
        #endregion

        #region Application initialization / Load
        public FDSMain()
        {
            try
            {

                int insCount = Generic.AlreadyRunningInstance();
                if (insCount > 1)
                    App.Current.Shutdown();

                InitializeComponent();
                InitializeTimers();
                InitializeNotifyIcon();
                InitializeFDS();
                DataContext = new ViewModel();
                thisWindow = GetWindow(this);
                client = new HttpClient { BaseAddress = AppConstants.EndPoints.BaseAPI };

            }
            catch (Exception ex)
            {
                if (showMessageBoxes == true)
                {
                    MessageBox.Show(ex.Message);
                }
            }

        }


        private void InitializeFDS()
        {
            cmbCountryCode.DropDownClosed += cmbCountryCode_DropDownClosed;
            txtCodeVersion.Text = AppConstants.CodeVersion;
            imgLoader = SetGIF("Assets\\spinner.gif");
            if (IsAdmin)
            {
                IsUninstallFlagUpdated();
            }

        }

        private void InitializeNotifyIcon()
        {
            icon = new System.Windows.Forms.NotifyIcon();
            icon.Icon = new System.Drawing.Icon(Path.Combine(BaseDir, "Assets/FDSDesktopLogo.ico"));//new System.Drawing.Icon(Path.Combine(Directory.GetParent(System.Environment.CurrentDirectory).Parent.FullName + "\\Assets\\FDSDesktopLogo.ico"));
            icon.Visible = true;
            icon.BalloonTipText = "The app has been minimised. Click the tray icon to show.";
            icon.BalloonTipTitle = "FDS (Scanning & Cleaning)";
            icon.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            icon.Click += Icon_Click;
        }

        private void InitializeTimers()
        {
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

            //TimerEventBasedService_Tick
            timerEventBasedService = new DispatcherTimer();
            timerEventBasedService.Interval = TimeSpan.FromMinutes(1);
            timerEventBasedService.Tick += TimerEventBasedService_Tick;
            timerEventBasedService.IsEnabled = false;
        }

        public void LoadFDS()
        {
            try
            {

                //// -------Actual Code --------------------------------
                encryptOutPutFile = basePathEncryption + @"\Main";

                ConfigDataClear();

                if (File.Exists(encryptOutPutFile))
                {
                    string finalOutPutFile = basePathEncryption + @"\FinalDecrypt";
                    Common.EncryptionDecryption.DecryptFile(encryptOutPutFile, finalOutPutFile);
                    Common.EncryptionDecryption.ReadDecryptFile(finalOutPutFile);
                }



                if (!CheckAllKeys())
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
                if (showMessageBoxes == true)
                {
                    MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        public bool CheckAllKeys()
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
                if (showMessageBoxes == true)
                {
                    MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return false;
            }

        }


        public void FDSMain_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadFDS();
            }
            catch (Exception ex)
            {
                if (showMessageBoxes == true)
                {
                    MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
                        //AuthenticationStep3.Visibility = Visibility.Hidden;
                        lblUserName.Visibility = Visibility.Hidden;
                        AuthenticationProcessing.Visibility = Visibility.Hidden;
                        AuthenticationFailed.Visibility = Visibility.Hidden;
                        AuthenticationSuccessfull.Visibility = Visibility.Hidden;
                        //txtEmail.Text = string.Empty;
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
                        //AuthenticationStep3.Visibility = Visibility.Hidden;
                        header.Visibility = Visibility.Visible;
                        lblUserName.Visibility = Visibility.Hidden;
                        imgDesktop.Visibility = Visibility.Hidden;
                        btnUninstall.Visibility = Visibility.Hidden;
                        break;
                    case Screens.AuthenticationStep3:
                        //AuthenticationStep3.Visibility = Visibility.Visible;
                        AuthenticationStep2.Visibility = Visibility.Hidden;
                        AuthenticationMethods.Visibility = Visibility.Hidden;
                        header.Visibility = Visibility.Visible;
                        lblUserName.Visibility = Visibility.Hidden;
                        imgDesktop.Visibility = Visibility.Hidden;
                        btnUninstall.Visibility = Visibility.Hidden;
                        break;
                    case Screens.AuthenticationProcessing:
                        AuthenticationProcessing.Visibility = Visibility.Visible;
                        AuthenticationStep2.Visibility = Visibility.Hidden;
                        header.Visibility = Visibility.Visible;
                        lblUserName.Visibility = Visibility.Hidden;
                        imgDesktop.Visibility = Visibility.Hidden;
                        System.Windows.Controls.Image imgProcessing = SetGIF("\\Assets\\loader.gif");
                        AuthenticationProcessing.Children.Add(imgProcessing);
                        break;
                    case Screens.AuthSuccessfull:
                        AuthenticationSuccessfull.Visibility = Visibility.Visible;
                        AuthenticationStep2.Visibility = Visibility.Hidden;
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
                        AuthenticationStep2.Visibility = Visibility.Hidden;
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
                if (showMessageBoxes == true)
                {
                    MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private System.Windows.Controls.Image SetGIF(string ImagePath)
        {
            string uriString = string.Empty;
            if (ImagePath.Contains("spinner"))
            {
                uriString = AppDomain.CurrentDomain.BaseDirectory + ImagePath;
            }
            else
            {
                uriString = Directory.GetCurrentDirectory() + ImagePath;
            }
            //string uriString = Directory.GetCurrentDirectory() + ImagePath;
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
                if (showMessageBoxes == true)
                {
                    MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
            //if (ImageContainerToken.Children.Count > 0)
            //{
            //    ImageContainerToken.Children.Remove(imgLoader);
            //}
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
                if (!string.IsNullOrWhiteSpace(txtEmailToken.Text) && !string.IsNullOrWhiteSpace(txtPhoneNubmer.Text) && IsValidEmailTokenNumber(txtEmailToken.Text) && IsValidMobileNumber(txtPhoneNubmer.Text))
                {
                    txtPhoneValidation.Visibility = Visibility.Collapsed;
                    //txtEmailToken.Visibility = Visibility.Collapsed;

                    var formContent = new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string>("token", txtEmailToken.Text),
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
                        //txtEmailVerification.TextAlignment = TextAlignment.Center;
                        txtCodeVerification.TextAlignment = TextAlignment.Center;
                        txtCodeVerification.Text = "A verification code has been sent to \n" + txtPhoneNubmer.Text;
                        //txtEmailVerification.Text = "A 32 digit token has been sent to  \n" + txtEmail.Text;
                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(txtEmailToken.Text))
                    {
                        txtEmailTokenValidation.Text = "Please enter Email Token";
                        txtEmailTokenValidation.Visibility = Visibility.Visible;
                    }
                    else if (!IsValidEmailTokenNumber(txtEmailToken.Text))
                    {
                        txtEmailTokenValidation.Text = "Invalid email token address!";
                        txtEmailTokenValidation.Visibility = Visibility.Visible;
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
                }
            }
            catch (Exception ex)
            {
                if (showMessageBoxes == true)
                {
                    MessageBox.Show("An error occurred while receiving OTP: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
            txtEmailToken.Text = "";
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
                        //new KeyValuePair<string, string>("assing_to_user", txtEmail.Text),
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
            //if (string.IsNullOrWhiteSpace(txtEmailToken.Text))
            //{
            //    txtEmailTokenValidation.Text = "Please enter token";
            //}
            //else if (!IsValidEmailTokenNumber(txtEmailToken.Text))
            //{
            //    txtEmailTokenValidation.Text = "Invalid Token number";
            //    txtEmailTokenValidation.Visibility = Visibility.Visible;
            //}
            //else
            //{
            //QRGeneratortimer.Start();
            GenerateQRCode("Token");
            //}
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

                //if ((vals == "Token") && (ImageContainerToken.Children.Count == 0))
                //{
                //    ImageContainerToken.Children.Add(imgLoader);
                //}
                if ((vals == "QR") && (ImageContainerQR.Children.Count == 0))
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
                {
                    if (showMessageBoxes == true)
                    {
                        MessageBox.Show("API response fails while generating QR code: ", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                }
            }
            catch (Exception ex)
            {
                if (showMessageBoxes == true)
                {
                    MessageBox.Show("An error occurred while generating QR code: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
                if (showMessageBoxes == true)
                {
                    MessageBox.Show("An error occurred while creating img for QR: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
            RSAParameters RSAParam;
            try
            {

                var QRCodeResponse = await apiService.CheckAuthAsync(DeviceResponse.qr_code_token, AppConstants.CodeVersion);

                if ((QRCodeResponse.StatusCode == HttpStatusCode.OK) || (QRCodeResponse.Public_key != null))
                {
                    isLoggedIn = true;

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
                    switch (QRCodeResponse.StatusCode)
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
                if (showMessageBoxes == true)
                {
                    MessageBox.Show("An error occurred while devicelogin: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }




        }
        private async Task KeyExchange()
        {

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

            if(CheckAllKeys())
            {
                 
                bool success = await apiService.PerformKeyExchangeAsync();


                if (success)
                {
                    timerLastUpdate.IsEnabled = true;
                    await GetDeviceDetails();
                }
                else
                {
                    timerLastUpdate.IsEnabled = false;
                    btnGetStarted_Click(btnGetStarted, null);
                    if (showMessageBoxes == true)
                    {
                        MessageBox.Show("An error occurred in KeyExchange: ", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            
        }
        #endregion


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
                if (showMessageBoxes == true)
                {
                    MessageBox.Show("An error occurred while doing encryption: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return "";
            }
        }

        #region device health

        public async Task CheckDeviceHealth()
        {


            isInternetConnected = Generic.CheckInternetConnection();
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
                        if (DeviceResponse != null)
                        {
                            DeviceResponse.qr_code_token = null;
                        }

                    }
                    //btnGetStarted_Click(btnGetStarted, null);
                    LoadMenu(Screens.GetStart);
                    MessageBox.Show("Your device has been deleted", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    deviceDeletedFlag = true;
                    lstCron.Clear();
                    encryptOutPutFile = basePathEncryption + @"\Main";
                    if (File.Exists(encryptOutPutFile))
                    {
                        File.Delete(encryptOutPutFile);
                        ConfigDataClear();
                    }
                    //this.Close();
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
                                break;
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
                if (showMessageBoxes == true)
                {
                    MessageBox.Show("An error occurred in DeviceConfigurationCheck: ", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
                if (showMessageBoxes == true)
                {
                    MessageBox.Show("An error occurred while doing decryption: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return "";
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
                    //DateTime convertedDate = DateTime.Parse(Convert.ToString(deviceDetail.updated_on));
                    //DateTime localDate = convertedDate.ToLocalTime();
                    //txtUpdatedOn.Text = deviceDetail.updated_on != null ? localDate.ToString() : "";
                    DateTime localDate = DateTime.Now.ToLocalTime();
                    txtUpdatedOn.Text = localDate.ToString();

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
                if (showMessageBoxes == true)
                {
                    MessageBox.Show("An error occurred in GetDeviceDetails: ", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
                if (showMessageBoxes == true)
                {
                    MessageBox.Show("An error occurred while doing decryption: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return "";
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
                if (showMessageBoxes == true)
                {
                    MessageBox.Show("An error occurred in RetrieveServices: ", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region Encryption / Decryption



        #endregion


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
                            if (deviceDeletedFlag == true)
                            {
                                //MessageBox.Show("Service Still Running");
                                break;
                            }
                            if (subservice.Sub_service_active)
                            {                                 
                                if (subservice.Execute_now)
                                {
                                    ExecuteSubService(subservice);                                     
                                }
                                else
                                {
                                    if (!string.IsNullOrEmpty(subservice.Execution_period))
                                    {
                                        var schedule = CrontabSchedule.Parse(subservice.Execution_period);
                                        DateTime nextRunTime = schedule.GetNextOccurrence(DateTime.Now);
                                        lstCron.Add(subservice, nextRunTime);
                                    }
                                }
                            }
                        }
                    }

                    CronLastUpdate.Start();
                    timerEventBasedService.Start();
                }
            }
            catch (Exception ex)
            {
                if (showMessageBoxes == true)
                {
                    MessageBox.Show("An error occurred while executing services: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }




        private async void CronLastUpdate_Tick(object sender, EventArgs e)
        {
            try
            {
                Dictionary<SubservicesData, DateTime> serviceToRemove = new Dictionary<SubservicesData, DateTime>();

                Dictionary<SubservicesData, DateTime> clonedDictionary = new Dictionary<SubservicesData, DateTime>();

                if (lstCron.Count > 0)
                {

                    Dictionary<string, SubservicesData> dicEventServices = new Dictionary<string, SubservicesData>();
                    foreach (var key in lstCron)
                    {
                        SubservicesData SubservicesData = key.Key;
                        if (DateTime.Now.Date == key.Value.Date && DateTime.Now.Hour == key.Value.Hour && DateTime.Now.Minute == key.Value.Minute)
                        {

                            var result = RunServices("S", dicEventServices, SubservicesData);

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

        public async Task<bool> RunServices(string serviceTypeFlag, Dictionary<string, SubservicesData> dicEventServices, SubservicesData SubservicesData)
        {
            try
            {

                ScheduleRunner scheduleRunner = new ScheduleRunner();
                string transformed = TransformString(SubservicesData.Sub_service_name);
                dicEventServices.Add(transformed, SubservicesData);

                List<string> whitelistedDomain = new List<string>();
                if (transformed == ServiceTypeName.WebSessionProtection.ToString())
                {
                    whitelistedDomain = await GetWhiteListDomainsList(dicEventServices);
                }
                if (dicEventServices.Count > 0)
                {
                    await scheduleRunner.RunAll(dicEventServices, serviceTypeFlag, whitelistedDomain);
                }
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;

        }

        static string TransformString(string input)
        {
            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            string[] parts = input.Split('_');

            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = textInfo.ToTitleCase(parts[i]);
            }

            return string.Concat(parts);
        }

        private async void TimerEventBasedService_Tick(object sender, EventArgs e)
        {
            if (lstCron.Count > 1)
            {
                EventRunner eventRunner = new EventRunner();
                Dictionary<string, SubservicesData> dicEventServices = new Dictionary<string, SubservicesData>();

                foreach (var key in lstCron)
                {
                    SubservicesData SubservicesData = key.Key;
                    string transformed = TransformString(SubservicesData.Sub_service_name);
                    if ((SubservicesData.Sub_service_active) && ((transformed == ServiceTypeName.WebSessionProtection.ToString()) || (transformed == ServiceTypeName.WebCacheProtection.ToString()) || (transformed == ServiceTypeName.WebTrackingProtecting.ToString())))
                    {
                        dicEventServices.Add(transformed, SubservicesData);
                    }
                }

                ////Get white domain list for web session protection service
                List<string> whitelistedDomain = await GetWhiteListDomainsList(dicEventServices);


                if (dicEventServices.Count > 1)
                {
                    bool result = eventRunner.RunAll(dicEventServices, "E", whitelistedDomain);
                }
            }

        }


        public async Task<List<string>> GetWhiteListDomainsList(Dictionary<string, SubservicesData> dicEventServices)
        {
            List<string> whitelistedDomain = new List<string>();
            if (dicEventServices.TryGetValue(ServiceTypeName.WebSessionProtection.ToString(), out SubservicesData Subservices))
            {
                DatabaseLogger databaseLogger = new DatabaseLogger();
                whitelistedDomain = await databaseLogger.GetWhiteListDomains(Subservices.Id.ToString());

            }
            return whitelistedDomain;
        }


        private async void ExecuteSubService(SubservicesData subservices)
        {
            try
            {
                Dictionary<string, SubservicesData> dicEventServices = new Dictionary<string, SubservicesData>();
                var result = await RunServices("M", dicEventServices, subservices);

                DateTime localDate = DateTime.Now.ToLocalTime();
                txtUpdatedOn.Text = localDate.ToString();

            }
            catch (Exception ex)
            {
                if (showMessageBoxes == true)
                {
                    MessageBox.Show("An error occurred while executing subservices: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }





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

            Application.Current.Dispatcher.Invoke(() =>
            {
                thisWindow.Visibility = Visibility.Visible;
                thisWindow.WindowState = WindowState.Normal;
                thisWindow.ShowInTaskbar = true;
                thisWindow.Focus();
                Activate();
            });

            LoadFDS();
            //await GetDeviceDetails();
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
                if (showMessageBoxes == true)
                {
                    MessageBox.Show(responseData.error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void UninstallResponseTimer_Tick(object sender, EventArgs e)
        {
            UninstallProgram();
        }

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;  // Hides the window
        const int SW_SHOW = 5;  // Shows the window
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


                                        string productCode = uninstallString;


                                        //MessageBox.Show(uninstallString, "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                                        cleanSystem();

                                        try
                                        {
                                            string productCode1 = uninstallString.Replace("/I", "").Replace("MsiExec.exe", "").Trim();

                                            string uninstallCommand = $"MsiExec.exe /x{productCode1} /qn";

                                            ProcessStartInfo uninstallStartInfo = new ProcessStartInfo
                                            {
                                                FileName = "cmd.exe",
                                                Arguments = $"/c {uninstallCommand}",
                                                RedirectStandardOutput = true,
                                                RedirectStandardError = true,
                                                UseShellExecute = false,
                                                CreateNoWindow = true
                                            };

                                            using (Process uninstallProcess = new Process { StartInfo = uninstallStartInfo })
                                            {
                                                uninstallProcess.Start();
                                            }
                                        }
                                        catch (Exception ex)
                                        {

                                            MessageBox.Show(ex.ToString());
                                        }






                                        //MessageBox.Show(productCode.Replace("/I", "/x ") + " /qn");
                                        //Process.Start("cmd.exe", productCode.Replace("/I", "/x ") + " /qn");
                                        //Process.Start("cmd.exe", productCode.Replace("/I", "/x ") + " /qn");



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
                    if (showMessageBoxes == true)
                    {
                        Console.WriteLine(ex.Message);
                        MessageBox.Show("An error while uninstalling application: ", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                if (showMessageBoxes == true)
                {
                    MessageBox.Show(responseData.error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
                    //Process.Start(AutoUpdateExePath);
                    //C:\\web\\Temp\\FDS\\
                    Process.Start(@"C:\web\Temp\FDS\AutoUpdate.exe");
                }
                //MessageBox.Show("Autoupdate start");
            }
            catch (Exception e)
            {
                MessageBox.Show("Error is to open updated exe " + e.Message);
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
                if (showMessageBoxes == true)
                {
                    Console.WriteLine(ex.Message);
                    MessageBox.Show("An error occurred in updateTrayIcon: ", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        #endregion
    }
}