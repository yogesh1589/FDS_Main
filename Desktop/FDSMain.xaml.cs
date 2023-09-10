﻿using FDS.API_Service;
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
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Reflection;
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
using Windows.Devices.Usb;
using Windows.Media.Core;
using WpfAnimatedGif;
using Image = System.Drawing.Image;

namespace FDS
{
    /// <summary>+
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class FDSMain : Window
    {

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



        private bool isLoggedIn { get; set; }

        bool IsAdmin => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        public static string BaseDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);


        bool IsQRGenerated;

        [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredDelete(string target, int type, int reserved);

        public Dictionary<SubservicesData, DateTime> lstCron = new Dictionary<SubservicesData, DateTime>();
        public Dictionary<SubservicesData, DateTime> lstCronEvent = new Dictionary<SubservicesData, DateTime>();
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



        public RSACryptoServiceProvider RSADevice { get; set; }
        public RSACryptoServiceProvider RSAServer { get; set; }


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

                //Generic.DeleteDirecUninstall();

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
                    try
                    {
                        if (File.Exists(TempPath + "AutoUpdate.exe"))
                        {
                            if (TryCloseRunningProcess("AutoUpdate"))
                            {
                                Directory.Delete(TempPath, true);
                            }
                        }
                    }
                    catch
                    {
                        MessageBox.Show("error");
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
                if (!string.IsNullOrWhiteSpace(txtEmailToken.Text) && !string.IsNullOrWhiteSpace(txtPhoneNubmer.Text) && Generic.IsValidEmailTokenNumber(txtEmailToken.Text) && Generic.IsValidMobileNumber(txtPhoneNubmer.Text))
                {
                    txtPhoneValidation.Visibility = Visibility.Collapsed;

                    // Show the spinner
                    ClearChildrenNode();

                    if (ImageContainerOTP.Children.Count == 0)
                    {
                        ImageContainerOTP.Children.Add(imgLoader);
                    }

                    var apiResponse = await apiService.SendOTPAsync(txtEmailToken.Text, txtPhoneNubmer.Text, txtCountryCode.Text);

                    ClearChildrenNode();

                    if ((apiResponse.HttpStatusCode == 0) || (apiResponse.Success == true))
                    {
                        LoadMenu(Screens.AuthenticationStep2);
                        txtCodeVerification.TextAlignment = TextAlignment.Center;
                        txtCodeVerification.Text = "A verification code has been sent to \n" + txtPhoneNubmer.Text;
                        txtEmailTokenValidation.Visibility = Visibility.Hidden;
                        txtPhoneValidation.Visibility = Visibility.Hidden;
                    }
                    else if (apiResponse.HttpStatusCode == HttpStatusCode.BadRequest)
                    {
                        txtEmailTokenValidation.Text = "Check email token !";
                        txtEmailTokenValidation.Visibility = Visibility.Visible;
                        txtPhoneValidation.Text = "Check phone number!";
                        txtPhoneValidation.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(txtEmailToken.Text))
                    {
                        txtEmailTokenValidation.Text = "Please enter Email Token";
                        txtEmailTokenValidation.Visibility = Visibility.Visible;
                    }
                    else if (!Generic.IsValidEmailTokenNumber(txtEmailToken.Text))
                    {
                        txtEmailTokenValidation.Text = "Invalid email token!";
                        txtEmailTokenValidation.Visibility = Visibility.Visible;
                    }
                    else if (string.IsNullOrWhiteSpace(txtPhoneNubmer.Text))
                    {
                        txtPhoneValidation.Text = "Please enter phone number";
                        txtPhoneValidation.Visibility = Visibility.Visible;
                    }
                    else if (!Generic.IsValidMobileNumber(txtPhoneNubmer.Text))
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

        private void txtBack_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            LoadMenu(Screens.AuthenticationMethods);
            txtEmailToken.Text = "";
            txtPhoneNubmer.Text = "";
        }

        private void btnStep2Next_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDigit1.Text) || string.IsNullOrWhiteSpace(txtDigit2.Text) || string.IsNullOrWhiteSpace(txtDigit3.Text)
                || string.IsNullOrWhiteSpace(txtDigit4.Text) || string.IsNullOrWhiteSpace(txtDigit5.Text) || string.IsNullOrWhiteSpace(txtDigit6.Text))
            {
                txtTokenValidation.Text = "Please enter Verification code";
            }
            else if (!Generic.IsValidTokenNumber(txtDigit1.Text) && !Generic.IsValidTokenNumber(txtDigit2.Text) && !Generic.IsValidTokenNumber(txtDigit3.Text)
                && !Generic.IsValidTokenNumber(txtDigit5.Text) && !Generic.IsValidTokenNumber(txtDigit5.Text) && !Generic.IsValidTokenNumber(txtDigit6.Text))
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

                    Dispatcher.Invoke(() =>
                    {
                        LoadMenu(Screens.AuthenticationProcessing);
                    });

                    var apiResponse = await apiService.QRGeneratortimerAsync(txtEmailToken.Text, txtPhoneNubmer.Text, txtCountryCode.Text, VerificationCode, DeviceResponse.qr_code_token);
                    if ((apiResponse.HttpStatusCode == HttpStatusCode.OK) || (apiResponse.Success = true))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            LoadMenu(Screens.AuthSuccessfull);
                            Dispatcher.Invoke(() =>
                            {
                                LoadMenu(Screens.Landing);
                                devicelogin(true);
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
            GenerateQRCode("Token");

        }
        private void txtstep2Back_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            LoadMenu(Screens.AuthenticationStep1);
        }
        private void txtstep3Back_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            LoadMenu(Screens.AuthenticationStep2);
        }



        public async void GenerateQRCode(string vals)
        {
            try
            {

                //Code for Loader
                ClearChildrenNode();
                if ((vals == "QR") && (ImageContainerQR.Children.Count == 0))
                {
                    ImageContainerQR.Children.Add(imgLoader);
                }

                DeviceResponse = await apiService.GenerateQRCodeAsync();

                ClearChildrenNode();


                if ((DeviceResponse.httpStatusCode == HttpStatusCode.OK) || (DeviceResponse.Success = true))
                {

                    TotalSeconds = Common.AppConstants.TotalKeyActivationSeconds;
                    timerQRCode.IsEnabled = true;


                    IsQRGenerated = DeviceResponse != null ? true : false;
                    //timerDeviceLogin.IsEnabled = true;
                    imgQR.Source = Generic.GetQRCode(DeviceResponse.qr_code_token);

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

                    Generic.RSAServer = RSAKeys.ImportPublicKey(System.Text.ASCIIEncoding.ASCII.GetString(Convert.FromBase64String(QRCodeResponse.Public_key)));
                    timerQRCode.IsEnabled = false;

                    Generic.RSADevice = new RSACryptoServiceProvider(2048);
                    RSAParam = Generic.RSADevice.ExportParameters(true);

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

            //if (CheckAllKeys())
            //{

                var apiResponse = await apiService.PerformKeyExchangeAsync();

                if ((apiResponse.HttpStatusCode == HttpStatusCode.OK) || (apiResponse.Success == true))
                {

                    //var plainText = EncryptDecryptData.Decrypt(apiResponse.Data, RSADevice);
                    //var finalData = JsonConvert.DeserializeObject<DTO.Responses.ResponseData>(plainText);

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
               // }
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

                Generic.RSAServer = RSAServer;
                Generic.RSADevice = RSADevice;

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

        public void loadMenuItems(string ImageTxt, string txtValue)
        {
            lblCompliant.Text = txtValue;
            string ImagePath = Path.Combine(BaseDir, ImageTxt);
            BitmapImage DeviceDeactive = new BitmapImage();
            DeviceDeactive.BeginInit();
            DeviceDeactive.UriSource = new Uri(ImagePath);
            DeviceDeactive.EndInit();
            imgCompliant.Source = DeviceDeactive;
            LoadMenu(Screens.Landing);
        }

        public async Task CheckDeviceHealth()
        {


            isInternetConnected = Generic.CheckInternetConnection();
            if (isInternetConnected)
            {
                var apiResponse = await apiService.CheckDeviceHealthAsync();

                if ((apiResponse.HttpStatusCode == HttpStatusCode.OK) || (apiResponse.Success = true) && (apiResponse.HttpStatusCode != HttpStatusCode.Unauthorized))
                {
                    var plainText = EncryptDecryptData.RetriveDecrypt(apiResponse.Data);
                    int idx = plainText.LastIndexOf('}');
                    var result = idx != -1 ? plainText.Substring(0, idx + 1) : plainText;
                    var HealthData = JsonConvert.DeserializeObject<HealthCheckResponse>(result);
                    if (HealthData.call_config)
                    {
                        await DeviceConfigurationCheck();
                    }

                    if (IsServiceActive)
                    {

                        loadMenuItems("Assets/DeviceActive.png", "Your system is Compliant");
                    }
                    else
                    {
                        loadMenuItems("Assets/DeviceDisable.png", "Check With Administrator");

                    }
                }
                else if (apiResponse.HttpStatusCode == HttpStatusCode.BadGateway)
                {
                    timerLastUpdate.IsEnabled = true;

                    loadMenuItems("Assets/DeviceDisable.png", "Ooops! Will be back soon.");
                }
                else if (apiResponse.HttpStatusCode == HttpStatusCode.Unauthorized)
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
                    lstCronEvent.Clear();
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
            var apiResponse = await apiService.DeviceConfigurationCheckAsync();

            if ((apiResponse.HttpStatusCode == HttpStatusCode.OK) || (apiResponse.Success = true))
            {

                var plainText = EncryptDecryptData.RetriveDecrypt(apiResponse.Data);
                int idx = plainText.LastIndexOf('}');
                var result = idx != -1 ? plainText.Substring(0, idx + 1) : plainText;
                var DeviceConfigData = JsonConvert.DeserializeObject<DeviceConfigCheckResponse>(result);

                if (DeviceConfigData.config_change)
                {
                    if (DeviceConfigData.call_api.Count() > 0)
                    {

                        foreach (var api in DeviceConfigData.call_api)
                        {
                            if (api.Equals("1") || api.Equals("4"))
                            {
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

        private async Task DeviceReauth()
        {
            var apiResponse = await apiService.DeviceReauthAsync();
            if ((apiResponse.HttpStatusCode == HttpStatusCode.OK) || (apiResponse.Success = true))
            {
                timerLastUpdate.IsEnabled = false;
                btnGetStarted_Click(btnGetStarted, null);
                //MessageBox.Show("An error occurred in DeviceReauth: ", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                /// need delete api for flushing 
            }
        }


        private async Task GetDeviceDetails()
        {
            var apiResponse = await apiService.GetDeviceDetailsAsync();

            if ((apiResponse.HttpStatusCode == HttpStatusCode.OK) || (apiResponse.Success = true))
            {
                var plainText = EncryptDecryptData.RetriveDecrypt(apiResponse.Data);
                var deviceDetail = (dynamic)null;
                if (!string.IsNullOrEmpty(plainText))
                {
                    int idx = plainText.LastIndexOf('}');
                    var result = idx != -1 ? plainText.Substring(0, idx + 1) : plainText;
                    deviceDetail = JsonConvert.DeserializeObject<DeviceDetail>(result);  //
                }


                if (deviceDetail != null)
                {
                    lblSerialNumber.Text = lblPopSerialNumber.Text = deviceDetail.serial_number;
                    lblUserName.Text = lblDeviceName.Text = deviceDetail.device_name;
                    lblLocation.Text = deviceDetail.device_location != null ? deviceDetail.device_location.ToString() : "";
                    txtOrganization.Text = deviceDetail.org_name != null ? deviceDetail.org_name.ToString() : txtOrganization.Text;

                    DateTime localDate = DateTime.Now.ToLocalTime();
                    txtUpdatedOn.Text = localDate.ToString();

                    //timerLastUpdate.IsEnabled = false;
                    if (deviceDetail.is_active)
                    {
                        await RetrieveServices();
                        loadMenuItems("Assets/DeviceActive.png", "Your system is Compliant");
                    }
                    else
                    {
                        loadMenuItems("Assets/DeviceDisable.png", "Check With Administrator");
                    }
                }
            }
            else
            {
                if (showMessageBoxes == true)
                {
                    MessageBox.Show("An error occurred in GetDeviceDetails: ", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        public async Task RetrieveServices()
        {

            var apiResponse = await apiService.RetrieveServicesAsync();

            if ((apiResponse.HttpStatusCode == HttpStatusCode.OK) || (apiResponse.Success = true))
            {
                var plainText = EncryptDecryptData.RetriveDecrypt(apiResponse.Data);

                if (!string.IsNullOrEmpty(plainText))
                {
                    int idx = plainText.LastIndexOf('}');
                    var result = idx != -1 ? plainText.Substring(0, idx + 1) : plainText;
                    var servicesResponse = JsonConvert.DeserializeObject<ServicesResponse>(result);//.Replace("false", "true"));// replace used to test services
                                                                                                   //var servicesResponse = JsonConvert.DeserializeObject<ServicesResponse>(plainText);

                    DateTime localDate = DateTime.Now.ToLocalTime();
                    txtUpdatedOn.Text = localDate.ToString();
                    ExecuteServices(servicesResponse);
                }


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


        private void ExecuteServices(ServicesResponse servicesResponse)
        {
            try
            {

                if (IsServiceActive)
                {
                    lstCron.Clear();
                    lstCronEvent.Clear();
                    foreach (var services in servicesResponse.Services)
                    {
                        foreach (var subservice in services.Subservices)
                        {
                            if (deviceDeletedFlag == true)
                            {
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
                                    var schedule = CrontabSchedule.Parse(subservice.Execution_period);
                                    DateTime nextRunTime = schedule.GetNextOccurrence(DateTime.Now);
                                    if (!string.IsNullOrEmpty(subservice.Execution_period))
                                    {
                                        lstCron.Add(subservice, nextRunTime);
                                    }
                                    lstCronEvent.Add(subservice, nextRunTime);
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


                    foreach (var key in lstCron)
                    {
                        SubservicesData SubservicesData = key.Key;


                        //bool testCheck = false;
                        ////if ((SubservicesData.Name.ToString() == "Web Session Protection") || (SubservicesData.Name.ToString() == "Web Cache Protection") || (SubservicesData.Name.ToString() == "Trash Data Protection") || (SubservicesData.Name.ToString() == "Web Tracking Protecting"))
                        //if (SubservicesData.Name.ToString() == "DNS Cache Protection")
                        //{
                        //    testCheck = true;
                        //}
                        //if ((DateTime.Now.Date == key.Value.Date && DateTime.Now.Hour == key.Value.Hour && DateTime.Now.Minute == key.Value.Minute) || (testCheck == true))


                        if (DateTime.Now.Date == key.Value.Date && DateTime.Now.Hour == key.Value.Hour && (DateTime.Now.Minute == key.Value.Minute || DateTime.Now.Minute - 1 == key.Value.Minute))
                        {

                            var result = RunServices("S", SubservicesData);

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

        public async Task<bool> RunServices(string serviceTypeFlag, SubservicesData SubservicesData)
        {
            try
            {

                ScheduleRunner scheduleRunner = new ScheduleRunner();
                string transformed = TransformString(SubservicesData.Sub_service_name);
                Dictionary<string, SubservicesData> dicEventServices = new Dictionary<string, SubservicesData>();
                dicEventServices.Add(transformed, SubservicesData);

                List<string> whitelistedDomain = new List<string>();
                if (transformed == ServiceTypeName.WebSessionProtection.ToString())
                {
                    whitelistedDomain = await GetWhiteListDomainsList(dicEventServices);
                }
                if (dicEventServices.Count > 0)
                {
                    await scheduleRunner.RunAll(dicEventServices, serviceTypeFlag, whitelistedDomain);
                    dicEventServices.Clear();
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
            if (lstCronEvent.Count > 1)
            {
                EventRunner eventRunner = new EventRunner();
                Dictionary<string, SubservicesData> dicEventServices = new Dictionary<string, SubservicesData>();



                foreach (var key in lstCronEvent)
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
                    if (result)
                    {
                        DateTime localDate = DateTime.Now.ToLocalTime();
                        txtUpdatedOn.Text = localDate.ToString();
                    }
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

                var result = await RunServices("M", subservices);

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


        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
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

            var apiResponse = await apiService.UninstallAsync();

            if ((apiResponse.HttpStatusCode == HttpStatusCode.OK) || (apiResponse.Success = true))
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
                    MessageBox.Show(apiResponse.error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

            var apiResponse = await apiService.UninstallProgramAsync();

            if ((apiResponse.HttpStatusCode == HttpStatusCode.OK) || (apiResponse.Success = true))
            {
                /// 1- Pending, 2 - Approved, 3 - Rejected
                try
                {
                    if (apiResponse.Data == "1")
                    {
                        btnUninstall.ToolTip = "Your uninstall request is pending.";
                        btnUninstall.Foreground = System.Windows.Media.Brushes.Gold;

                    }
                    else if (apiResponse.Data == "3")
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
                        Generic.DeleteDirecUninstall();
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
                    MessageBox.Show(apiResponse.error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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



        private async void AutoUpdate()
        {
            var apiResponse = await apiService.AutoUpdateAsync();

            if ((apiResponse.HttpStatusCode == HttpStatusCode.OK) || (apiResponse.Success = true))
            {
                var getresponse = await client.GetAsync(AppConstants.EndPoints.AutoUpdate + "?token=" + apiResponse.msg);
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
        private async void DownloadFile(string url, string temporaryMSIPath)
        {
            try
            {
                await DownloadEXEAsync(url, temporaryMSIPath);

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error downloading file: " + ex.Message);
            }
        }


        private async Task<bool> DownloadEXEAsync(string downloadUrl, string temporaryMSIPath)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var response = await client.GetAsync(downloadUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        using (var fileStream = System.IO.File.Create(temporaryMSIPath))
                        {
                            await response.Content.CopyToAsync(fileStream);
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error downloading file: " + ex.Message);
                }

                try
                {

                    if (File.Exists(TempPath + "FDS.msi"))
                    {
                        //string sourcePath = Directory.GetCurrentDirectory() + "\\AutoUpdate.exe";
                        string tempPath1 = "C:\\Fusion Data Secure\\FDS\\AutoUpdate.exe";

                        try
                        {
                             
                            if (File.Exists(tempPath1))
                            {
                                if (TryCloseRunningProcess("AutoUpdate"))
                                {
                                    File.Copy(tempPath1, TempPath + "AutoUpdate.exe", true);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show("Path not found for updated exe " + e.Message);
                        }
                        string AutoUpdateExePath = TempPath + "AutoUpdate.exe";
                        Process.Start(AutoUpdateExePath);
                    }

                }
                catch (Exception e)
                {
                    MessageBox.Show("Error is to open updated exe " + e.Message);
                }

            }
            return true;
        }


        private bool TryCloseRunningProcess(string processName)
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(processName);

                foreach (Process process in processes)
                {
                    process.CloseMainWindow(); // Attempt to close the main window gracefully
                    process.WaitForExit(5000); // Wait for the process to exit for up to 5 seconds

                    if (!process.HasExited)
                    {
                        // If the process did not exit, kill it forcibly
                        process.Kill();
                    }
                }

                return true;
            }
            catch (Exception)
            {
                MessageBox.Show("Error in TryCloseRunningProcess");
                return false;
            }
        }


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

    }
}