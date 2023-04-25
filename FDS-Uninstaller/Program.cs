using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Net.Http;
using Desktop.DTO.Requests;
using Desktop.Common;
using Newtonsoft.Json;
using Desktop.DTO.Responses;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Input;
using System.Threading.Tasks;

namespace Setup
{
    class Program
    {
        public static HttpClient client { get; }
        //private DispatcherTimer UninstallResponseTimer;
        static RSACryptoServiceProvider RSADevice { get; set; }
        static RSACryptoServiceProvider RSAServer { get; set; }
        public static byte[] Key { get; set; }
        public static void Main(string[] args)
        {
            RemoveControlPanelProgram();

            Console.WriteLine("Application Installed Successfully....");
            Console.ReadLine();
        }
        private void UninstallResponseTimer_Tick(object sender, EventArgs e)
        {
            RemoveControlPanelProgram();
        }
        public static async Task RemoveControlPanelProgram()
        {
            var servicesObject = new RetriveServices
            {
                authorization_token = KeyManager.GetValue("authorization_token"),
                mac_address = AppConstants.MACAddress,
                serial_number = AppConstants.SerialNumber
            };
            var payload = Encrypt(Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(servicesObject))));

            var formContent = new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string>("authentication_token", KeyManager.GetValue("Authentication_token")) ,
                        new KeyValuePair<string, string>("payload", payload)
                    };

            var response = await client.PostAsync(AppConstants.EndPoints.DeviceServices, new FormUrlEncodedContent(formContent));

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var responseData = JsonConvert.DeserializeObject<ResponseData>(responseString);
                var plainText = Decrypt(responseData.Data);

                var deviceDetail = JsonConvert.DeserializeObject<DeviceDetail>(plainText.Split('}')[0] + '}');

                if (deviceDetail != null)
                {

                    string applicationName = "FDS";
                    string InstallerRegLoc = "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
                    RegistryKey homeKey = Registry.LocalMachine.OpenSubKey(InstallerRegLoc, true);
                    RegistryKey appSubKey = homeKey.OpenSubKey(applicationName);
                    if (null != appSubKey)
                    {
                        homeKey.DeleteSubKey(applicationName);
                    }
                }
            }

            //else
            //{
            //    UninstallResponseTimer = new DispatcherTimer();
            //    UninstallResponseTimer.Tick += UninstallResponseTimer_Tick;
            //    UninstallResponseTimer.Interval = TimeSpan.FromMilliseconds(1000 * 5); // in miliseconds
            //    UninstallResponseTimer.Start();
            //}
        }

        public static string Encrypt(string plainText)
        {
            //byte[] Key;
            byte[] AesEncrypted;
            using (var aesAlg = new AesCryptoServiceProvider())
            {
                // Create an encryptor to perform the stream transform.
                Key = aesAlg.Key;
                aesAlg.Mode = CipherMode.ECB;
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
            var RsaEncrypted = RSAServer.Encrypt(Key, true);
            return Convert.ToBase64String(RsaEncrypted.Concat(AesEncrypted).ToArray());
        }
        public static string Decrypt(string Cipher)
        {
            byte[] bArray = Convert.FromBase64String(Cipher);
            byte[] encKey = bArray.Take(256).ToArray();
            byte[] byteKey = RSADevice.Decrypt(encKey, true);

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
                        {
                            plaintext = reader.ReadToEnd();
                        }
                    }
                }
            }
            return plaintext;
        }
    }

}