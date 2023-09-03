using FDS.DTO.Responses;
using FDS.SingleTon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.Devices.Usb;

namespace FDS.Common
{
    static class EncryptDecryptData
    {
        static string basePathEncryption = String.Format("{0}Tempfolder", AppDomain.CurrentDomain.BaseDirectory);
        static string encryptOutPutFile = @"\Main";
        private const int KeySize = 2048;

        public static QRCodeResponse QRCodeResponse { get; private set; }
      
        static bool showMessageBoxes = true;
        public static RSACryptoServiceProvider RSADevice { get; set; }
        public static RSACryptoServiceProvider RSAServer { get; set; }
        public static byte[] EncKey { get; set; }
        //public static bool CheckAllKeys()
        //{
        //    try
        //    {


        //        RSAParameters RSAParam;

        //        RSACryptoServiceProvider RSADevice = new RSACryptoServiceProvider(2048);
        //        RSAParam = RSADevice.ExportParameters(true);

        //        RSADevice = RSADevice;

        //        string filePath = Path.Combine(basePathEncryption, "Main");

        //        if (!File.Exists(filePath))
        //        {
        //            return false;
        //        }

        //        RSAParam = new RSAParameters
        //        {
        //            InverseQ = Convert.FromBase64String(String.IsNullOrEmpty(ConfigDetails.InverseQ) ? string.Empty : ConfigDetails.InverseQ),
        //            DQ = Convert.FromBase64String(String.IsNullOrEmpty(ConfigDetails.DQ) ? string.Empty : ConfigDetails.DQ),
        //            DP = Convert.FromBase64String(String.IsNullOrEmpty(ConfigDetails.DP) ? string.Empty : ConfigDetails.DP),
        //            Q = Convert.FromBase64String(String.IsNullOrEmpty(ConfigDetails.Q) ? string.Empty : ConfigDetails.Q),
        //            P = Convert.FromBase64String(String.IsNullOrEmpty(ConfigDetails.P) ? string.Empty : ConfigDetails.P),
        //            D = Convert.FromBase64String(String.IsNullOrEmpty(ConfigDetails.D) ? string.Empty : ConfigDetails.D),
        //            Exponent = Convert.FromBase64String(String.IsNullOrEmpty(ConfigDetails.Exponent) ? string.Empty : ConfigDetails.Exponent),
        //            Modulus = Convert.FromBase64String(String.IsNullOrEmpty(ConfigDetails.Modulus) ? string.Empty : ConfigDetails.Modulus),
        //        };

        //        RSADevice = new RSACryptoServiceProvider(2048);
        //        RSADevice.ImportParameters(RSAParam);

        //        var key1 = String.IsNullOrEmpty(ConfigDetails.Key1) ? string.Empty : ConfigDetails.Key1;
        //        var key2 = String.IsNullOrEmpty(ConfigDetails.Key2) ? string.Empty : ConfigDetails.Key2;
        //        var Authentication_token = String.IsNullOrEmpty(ConfigDetails.Authentication_token) ? string.Empty : ConfigDetails.Authentication_token;
        //        var Authorization_token = String.IsNullOrEmpty(ConfigDetails.Authorization_token) ? string.Empty : ConfigDetails.Authorization_token;


        //        bool ValidServerKey = !string.IsNullOrEmpty(key1) && !string.IsNullOrEmpty(key2) && !string.IsNullOrEmpty(Authentication_token) && !string.IsNullOrEmpty(Authorization_token);
        //        if (!ValidServerKey)
        //        {
        //            return false;
        //        }
        //        QRCodeResponse = new QRCodeResponse
        //        {
        //            Public_key = key1 + key2,
        //            Authentication_token = Authentication_token,
        //            Authorization_token = Authorization_token
        //        };
        //        RSAServer = new RSACryptoServiceProvider(2048);
        //        RSAServer = RSAKeys.ImportPublicKey(System.Text.ASCIIEncoding.ASCII.GetString(Convert.FromBase64String(QRCodeResponse.Public_key)));                 
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        if (showMessageBoxes == true)
        //        {
        //            MessageBox.Show("An error occurred: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //        }
        //        return false;
        //    }

        //}

        //public static string Encrypt(string plainText)
        //{
        //    try
        //    {
        //        //byte[] Key;
        //        byte[] AesEncrypted;
        //        using (var aesAlg = new AesCryptoServiceProvider())
        //        {
        //            // Create an encryptor to perform the stream transform.
        //            EncKey = aesAlg.Key;
        //            aesAlg.Mode = CipherMode.ECB;
        //            aesAlg.Padding = PaddingMode.PKCS7;
        //            ICryptoTransform encryptor = aesAlg.CreateEncryptor();
        //            // Create the streams used for encryption.
        //            using (MemoryStream msEncrypt = new MemoryStream())
        //            {
        //                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        //                {
        //                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
        //                    {
        //                        //Write all data to the stream.
        //                        swEncrypt.Write(plainText);
        //                    }
        //                    AesEncrypted = msEncrypt.ToArray();
        //                }
        //            }
        //        }         


        //        var RsaEncrypted = RSAServer.Encrypt(EncKey, true);
        //        return Convert.ToBase64String(RsaEncrypted.Concat(AesEncrypted).ToArray());
        //    }
        //    catch (Exception ex)
        //    {
        //        if (showMessageBoxes == true)
        //        {
        //            MessageBox.Show("An error occurred while doing encryption: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //        }
        //        return "";
        //    }
        //}
        //public static string Decrypt(string Cipher)
        //{
        //    try
        //    {
        //        var bArray = Convert.FromBase64String(Cipher);
        //        var encKey = bArray.Take(256).ToArray();
             
        //        var byteKey = RSADevice.Decrypt(encKey, true);
        //        string plaintext = null;
        //        // Create AesManaged    
        //        using (AesManaged aes = new AesManaged())
        //        {
        //            // Create a decryptor    
        //            aes.Mode = CipherMode.ECB;
        //            ICryptoTransform decryptor = aes.CreateDecryptor(byteKey, aes.IV);
        //            // Create the streams used for decryption.    
        //            using (MemoryStream ms = new MemoryStream(bArray.Skip(256).ToArray()))
        //            {
        //                // Create crypto stream    
        //                using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
        //                {
        //                    // Read crypto stream    
        //                    using (StreamReader reader = new StreamReader(cs))
        //                        plaintext = reader.ReadToEnd();
        //                }
        //            }
        //        }
        //        return plaintext;
        //    }
        //    catch (Exception ex)
        //    {
        //        if (showMessageBoxes == true)
        //        {
        //            MessageBox.Show("An error occurred while doing decryption: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //        }
        //        return "";
        //    }

        //}

        //public static string RetriveDecrypt(string Cipher)
        //{
        //    try
        //    {
        //        var bArray = Convert.FromBase64String(Cipher);
        //        var encKey = bArray.Take(256).ToArray();               

        //        var byteKey = RSADevice.Decrypt(encKey, true);
        //        string plaintext = null;
        //        // Create AesManaged    
        //        using (AesManaged aes = new AesManaged())
        //        {
        //            // Create a decryptor    
        //            aes.Mode = CipherMode.ECB;
        //            aes.Padding = PaddingMode.None;
        //            ICryptoTransform decryptor = aes.CreateDecryptor(byteKey, aes.IV);
        //            // Create the streams used for decryption.    
        //            using (MemoryStream ms = new MemoryStream(bArray.Skip(256).ToArray()))
        //            {
        //                // Create crypto stream    
        //                using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
        //                {
        //                    // Read crypto stream    
        //                    using (StreamReader reader = new StreamReader(cs))
        //                        plaintext = reader.ReadToEnd();
        //                }
        //            }
        //        }
        //        return plaintext;
        //    }
        //    catch (Exception ex)
        //    {
        //        if (showMessageBoxes == true)
        //        {
        //            MessageBox.Show("An error occurred while doing decryption: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //        }
        //        return "";
        //    }

        //}
    }
}
