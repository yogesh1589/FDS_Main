using FDS.DTO.Responses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FDS.Common
{
    public class EncryptionDecryption
    {
        private const string key = "b14ca5898a4e4133bbce2ea2315a1916";
        private static byte[] Key = { };
        private static byte[] IV = { };

        public static void EncryptFile(string inputFile, string outputFile)
        {
            try
            {
                using (var aes = Aes.Create())
                {

                    //aes.GenerateKey();
                    //Key = aes.Key;
                    //aes.GenerateIV();
                    //IV = aes.IV;

                    byte[] iv = new byte[16];
                    aes.Key = Encoding.UTF8.GetBytes(key);
                    aes.IV = iv;

                    using (var inputFileStream = File.OpenRead(inputFile))
                    using (var outputFileStream = File.Create(outputFile))
                    using (var cryptoStream = new CryptoStream(outputFileStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        inputFileStream.CopyTo(cryptoStream);
                    }
                    aes.Padding = PaddingMode.None;
                }
                File.Delete(inputFile);
            }
            catch (CryptographicException ex)
            {
                ex.ToString();
            }
        }

        public static void DecryptFile(string inputFile, string outputFile)
        {
            try
            {
                using (var aes = Aes.Create())
                {
                    byte[] iv = new byte[16];
                    aes.Key = Encoding.UTF8.GetBytes(key);
                    aes.IV = iv;

                    // Create a decryptor
                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    using (var inputFileStream = File.OpenRead(inputFile))
                    using (var outputFileStream = File.Create(outputFile))
                    using (var cryptoStream = new CryptoStream(inputFileStream, decryptor, CryptoStreamMode.Read))
                    {
                        cryptoStream.CopyTo(outputFileStream);
                    }
                }
            }
            catch (CryptographicException ex)
            {
                throw ex;
            }
        }

        public static void ReadDecryptFile(string outputFile)
        {
            try
            {
                string[] lines = File.ReadAllLines(outputFile);
                DTO.Responses.ConfigDetails configDetails = new DTO.Responses.ConfigDetails();
                foreach (string str in lines)
                {
                    if ((str.Contains(AppConstants.KeyPrfix + "Key1")) && (String.IsNullOrEmpty(ConfigDetails.Key1)))
                    {
                        int colonIndex = str.IndexOf(":");
                        ConfigDetails.Key1 = str.Substring(colonIndex + 1).Trim();
                    }
                    else if ((str.Contains(AppConstants.KeyPrfix + "Key2")) && (String.IsNullOrEmpty(ConfigDetails.Key2)))
                    {
                        int colonIndex = str.IndexOf(":");
                        ConfigDetails.Key2 = str.Substring(colonIndex + 1).Trim();
                    }
                    else if ((str.Contains(AppConstants.KeyPrfix + "Authentication_token")) && (String.IsNullOrEmpty(ConfigDetails.Authentication_token)))
                    {
                        int colonIndex = str.IndexOf(":");
                        ConfigDetails.Authentication_token = str.Substring(colonIndex + 1).Trim();
                    }
                    else if ((str.Contains(AppConstants.KeyPrfix + "Authorization_token")) && (String.IsNullOrEmpty(ConfigDetails.Authorization_token)))
                    {
                        int colonIndex = str.IndexOf(":");
                        ConfigDetails.Authorization_token = str.Substring(colonIndex + 1).Trim();
                    }
                    else if ((str.Contains(AppConstants.KeyPrfix + "Modulus")) && (String.IsNullOrEmpty(ConfigDetails.Modulus)))
                    {
                        int colonIndex = str.IndexOf(":");
                        ConfigDetails.Modulus = str.Substring(colonIndex + 1).Trim();
                    }
                    else if ((str.Contains(AppConstants.KeyPrfix + "Exponent")) && (String.IsNullOrEmpty(ConfigDetails.Exponent)))
                    {
                        int colonIndex = str.IndexOf(":");
                        ConfigDetails.Exponent = str.Substring(colonIndex + 1).Trim();
                    }
                    else if ((str.Contains(AppConstants.KeyPrfix + "DP")) && (String.IsNullOrEmpty(ConfigDetails.DP)))
                    {
                        int colonIndex = str.IndexOf(":");
                        ConfigDetails.DP = str.Substring(colonIndex + 1).Trim();
                    }
                    else if ((str.Contains(AppConstants.KeyPrfix + "DQ")) && (String.IsNullOrEmpty(ConfigDetails.DQ)))
                    {
                        int colonIndex = str.IndexOf(":");
                        ConfigDetails.DQ = str.Substring(colonIndex + 1).Trim();
                    }
                    else if ((str.Contains(AppConstants.KeyPrfix + "D")) && (String.IsNullOrEmpty(ConfigDetails.D)))
                    {
                        int colonIndex = str.IndexOf(":");
                        ConfigDetails.D = str.Substring(colonIndex + 1).Trim();
                    }
                    else if ((str.Contains(AppConstants.KeyPrfix + "P")) && (String.IsNullOrEmpty(ConfigDetails.P)))
                    {
                        int colonIndex = str.IndexOf(":");
                        ConfigDetails.P = str.Substring(colonIndex + 1).Trim();
                    }
                    else if ((str.Contains(AppConstants.KeyPrfix + "Q")) && (String.IsNullOrEmpty(ConfigDetails.Q)))
                    {
                        int colonIndex = str.IndexOf(":");
                        ConfigDetails.Q = str.Substring(colonIndex + 1).Trim();
                    }
                    else if ((str.Contains(AppConstants.KeyPrfix + "InverseQ")) && (String.IsNullOrEmpty(ConfigDetails.InverseQ)))
                    {
                        int colonIndex = str.IndexOf(":");
                        ConfigDetails.InverseQ = str.Substring(colonIndex + 1).Trim();
                    }
                }
                File.Delete(outputFile);
            }
            catch (Exception ex)
            {
                ex.ToString();
            }

            //return lines;
        }

    }
}
