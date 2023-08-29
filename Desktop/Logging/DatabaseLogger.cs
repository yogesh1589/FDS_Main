﻿using FDS.Common;
using FDS.DTO.Requests;
using FDS.DTO.Responses;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.Media.Protection.PlayReady;

namespace FDS.Logging
{
    public class DatabaseLogger : ILogger
    {
        public HttpClient client { get; }
        public byte[] EncKey { get; set; }

        RSACryptoServiceProvider RSAServer { get; set; }

        public async void LogInformation(string authorizationCode, string subServiceName, long FileProcessed, string ServiceId, bool IsManualExecution)
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
        }

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

    }
}
