using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows;

namespace FDS.Common
{
    public class AppConstants
    {
        internal const string InvalidKeyErrorMsg = "Invalid Key";
        internal const int TotalKeyActivationSeconds = 600;
        public const string KeyPrfix = "FDS_Key_";
        //public const string AuthKey = SerialNumber + MACAddress + CodeVersion;
        public static string MachineName => System.Environment.MachineName;

        public class EndPoints
        {
            public static Uri BaseAPI => new Uri(ConfigurationManager.AppSettings["BaseUrl"]); //new Uri("https://f73f-43-230-42-140.in.ngrok.io");
            public const string Otp = "otp/";
            public const string DeviceToken = "device/token/activate/";
            public const string Start = "device/auth/start/";
            public const string CheckAuth = "device/auth/check/";
            public const string KeyExchange = "device/key-exchange/";
            public const string DeviceDetails = "device/details/";
            public const string DeviceServices = "device/services/retrieve/";
            public const string LogServicesData = "device/services/log/";
            public const string DeviceHealth = "device/health/";
            public const string DeviceConfigCheck = "device/configuration/check/";
            public const string DeviceReauth = "device/auth/reauth/";
            public const string WhiteListDomains = "device/whitelisted-domains/";
            public const string ExecuteNow = "subservice/executenow/";
            public const string UninstallDevice = "uninstall/device/";
            public const string UninstallCheck = "check/uninstall/device/";
            public const string CountryCode = "location/";
            public const string AutoUpdate = "update/";
            public const string PostCertificate = "certificate/";
            public const string PostProxy = "proxy/";
            public const string CertificateLog = "certificatelog/";
        }
        public const string DeviceType = "1";
        public static string CodeVersion = ConfigurationManager.AppSettings["CodeVersion"];

        public static string OSVersion
        {
            get
            {
                var name = (from x in new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem").Get().Cast<ManagementObject>()
                            select x.GetPropertyValue("Caption")).FirstOrDefault();
                return name != null ? name.ToString() : "Unknown";
            }
        }
        public static string SerialNumber
        {
            get
            {
                string SerialNumber = string.Empty;
                //ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");
                //ManagementObjectCollection information = searcher.Get();
                //foreach (ManagementObject obj in information)
                //    foreach (PropertyData data in obj.Properties)
                //        SerialNumber = Convert.ToString(data.Value);
                try
                {
                    ManagementObjectSearcher mos = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BIOS");
                    foreach (ManagementObject mo in mos.Get())
                    {
                        SerialNumber = mo["SerialNumber"].ToString();
                        break;
                    }
                }
                catch (Exception ex) { Console.WriteLine(ex.Message); }
                return SerialNumber;
            }
        }
        public static string MACAddress
        {
            get
            {
                string mac = string.Empty;
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapterConfiguration where IPEnabled=true");
                foreach (ManagementObject mo in searcher.Get())
                {
                    mac = mo["MACAddress"].ToString();
                    break;
                }

                ///mac = (from o in objects orderby o["IPConnectionMetric"] select o["MACAddress"].ToString()).FirstOrDefault();

                //MessageBox.Show("Mac 1");

                //NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

                ////MessageBox.Show("Mac 2");

                //foreach (NetworkInterface adapter in interfaces)
                //{
                //    //MessageBox.Show(adapter.NetworkInterfaceType.ToString() + " " + NetworkInterfaceType.Ethernet + " " + adapter.Name);
                //    if (adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet && adapter.Name == "Ethernet")
                //    {
                //        //MessageBox.Show("Mac 3");
                //        mac = adapter.GetPhysicalAddress().ToString();
                //        //MessageBox.Show("Mac 4 -" + mac);
                //    }
                //}
                //string formattedMacAddress = string.Join(":", Enumerable.Range(0, 6).Select(i => mac.Substring(i * 2, 2)));
                ////MessageBox.Show("Mac 5");
                return mac;

            }
        }
        public static string UUId
        {
            get
            {
                string uuid = string.Empty;
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select UUID from Win32_ComputerSystemProduct");
                foreach (ManagementObject mo in searcher.Get())
                {
                    uuid = mo["UUId"].ToString();
                    break;
                }
                //string mac = (from o in objects orderby o["IPConnectionMetric"] select o["MACAddress"].ToString()).FirstOrDefault();
                return uuid;
            }
        }
    }
}
