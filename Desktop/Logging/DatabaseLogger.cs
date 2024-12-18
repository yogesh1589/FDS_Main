﻿using FDS.Common;
using FDS.DTO.Requests;
using FDS.DTO.Responses;
using FDS.SingleTon;
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
using Windows.UI.Xaml;

namespace FDS.Logging
{
    public class DatabaseLogger : ILogger
    {
        //public HttpClient client { get; }
        public byte[] EncKey { get; set; }

        RSACryptoServiceProvider RSAServer { get; set; }

        //public DatabaseLogger() { client = new HttpClient { BaseAddress = AppConstants.EndPoints.BaseAPI }; }

        public async void LogInformation(string authorizationCode, string subServiceName, long FileProcessed, string ServiceId, bool IsManualExecution, string serviceTypeDetails)
        {
            try
            {

                bool IsEventExecution = false;
                if (serviceTypeDetails == "E")
                {
                    IsManualExecution = false;
                    IsEventExecution = true;
                }
                bool isSkipFlag = false;
                if (serviceTypeDetails == "SK")
                {
                    isSkipFlag = true;
                }

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
                    IsManualExecution = IsManualExecution,
                    IsEventExecution = IsEventExecution,
                    Skipped_Service_Executed = isSkipFlag
                };

                var payload = EncryptDecryptData.Encrypt(Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(logServiceRequest))));

                var formContent = new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string>("authentication_token", String.IsNullOrEmpty(ConfigDetails.Authentication_token) ? string.Empty : ConfigDetails.Authentication_token) ,
                        new KeyValuePair<string, string>("payload", payload),
                new KeyValuePair<string, string>("code_version", AppConstants.CodeVersion),
                    };

                var handler = new HttpClientHandler
                {
                    UseProxy = false // Disable using the system proxy
                };

                using (var client1 = new HttpClient(handler))
                {
                    client1.BaseAddress = new Uri(AppConstants.EndPoints.BaseAPI.ToString());
                    var response = await client1.PostAsync(AppConstants.EndPoints.LogServicesData, new FormUrlEncodedContent(formContent));
                    if (response.IsSuccessStatusCode)
                    {
                        //timerLastUpdate.IsEnabled = false;
                        var ExecuteNowContent = new List<KeyValuePair<string, string>> {
                    new KeyValuePair<string, string>("execute_now", "false") ,
                    };
                        var ExecuteNowResponse = await client1.PutAsync(AppConstants.EndPoints.ExecuteNow + ServiceId + "/", new FormUrlEncodedContent(ExecuteNowContent));
                        if (ExecuteNowResponse.IsSuccessStatusCode)
                        {

                        }
                    }
                }
            }
            catch
            {
                
            }
        }


        public async Task<List<string>> GetWhiteListDomains(string SubServiceId)
        {

            List<string> whitelistedDomain = new List<string>();

            try
            {
                var handler = new HttpClientHandler
                {
                    UseProxy = false // Disable using the system proxy
                };

                using (var client1 = new HttpClient(handler))
                {
                    client1.BaseAddress = new Uri(AppConstants.EndPoints.BaseAPI.ToString());
                    var response = await client1.GetAsync(AppConstants.EndPoints.WhiteListDomains + SubServiceId + "/");
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
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
            return whitelistedDomain;
        }
    }
}
