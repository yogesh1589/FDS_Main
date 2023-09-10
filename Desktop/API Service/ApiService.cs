﻿using FDS.Common;
using FDS.DTO.Requests;
using FDS.DTO.Responses;
using Newtonsoft.Json;
using OpenQA.Selenium.DevTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FDS.API_Service
{
    public class ApiService
    {
        private readonly HttpClient client;

        public ApiService()
        {
            client = new HttpClient();
            client = new HttpClient { BaseAddress = AppConstants.EndPoints.BaseAPI };
            // Configure client settings if needed (base URL, headers, etc.).
        }


        public async Task<QRCodeResponse> CheckAuthAsync(string qrCodeToken, string codeVersion)
        {
            try
            {
                var formContent = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("qr_code_token", qrCodeToken),
                new KeyValuePair<string, string>("code_version", codeVersion),
            };

                var response = await client.PostAsync(AppConstants.EndPoints.CheckAuth, new FormUrlEncodedContent(formContent));

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<QRCodeResponse>(responseString);
                     
                    return apiResponse;
                }
                else
                {
                    QRCodeResponse qRCodeResponse = new QRCodeResponse
                    {
                        StatusCode = response.StatusCode
                    };
                    return qRCodeResponse;
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions and log errors if needed.
                return null;
            }
        }

        public async Task<ResponseData> PerformKeyExchangeAsync()
        {
            try
            {

                var exchangeObject = new KeyExchange
                {
                    authorization_token = String.IsNullOrEmpty(ConfigDetails.Authorization_token) ? string.Empty : ConfigDetails.Authorization_token,
                    mac_address = AppConstants.MACAddress,
                    public_key = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(RSAKeys.ExportPublicKey(Generic.RSADevice))),
                    serial_number = AppConstants.SerialNumber,
                    device_uuid = AppConstants.UUId,
                };


                var payload = EncryptDecryptData.Encrypt(Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(exchangeObject))));

                var formContent = new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("authentication_token", String.IsNullOrEmpty(ConfigDetails.Authentication_token) ? string.Empty : ConfigDetails.Authentication_token) ,
                new KeyValuePair<string, string>("payload", payload),
                new KeyValuePair<string, string>("code_version", AppConstants.CodeVersion)
            };

                var response = await client.PostAsync(AppConstants.EndPoints.KeyExchange, new FormUrlEncodedContent(formContent));


                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ResponseData>(responseString);
                    apiResponse.Success = true;
                    return apiResponse;
                }
                else
                {
                    ResponseData qRCodeResponse = new ResponseData
                    {
                        HttpStatusCode = response.StatusCode
                    };
                    return qRCodeResponse;
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions and log errors if needed.
                return null;
            }
        }

        public async Task<ResponseData> CheckDeviceHealthAsync()
        {
            try
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
                var payload = EncryptDecryptData.Encrypt(Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(servicesObject))));


                var formContent = new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("authentication_token", String.IsNullOrEmpty(ConfigDetails.Authentication_token) ? string.Empty : ConfigDetails.Authentication_token) ,
                new KeyValuePair<string, string>("payload", payload),
                new KeyValuePair<string, string>("code_version", AppConstants.CodeVersion),
            };

                var response = await client.PostAsync(AppConstants.EndPoints.DeviceHealth, new FormUrlEncodedContent(formContent));
                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ResponseData>(responseString);
                    apiResponse.Success = true;
                    return apiResponse;
                }
                else
                {
                    ResponseData qRCodeResponse = new ResponseData
                    {
                        HttpStatusCode = response.StatusCode
                    };
                    return qRCodeResponse;
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions and log errors if needed.
                return null;
            }
        }

        public async Task<ResponseData> DeviceConfigurationCheckAsync()
        {
            try
            {
                var servicesObject = new RetriveServices
                {
                    authorization_token = String.IsNullOrEmpty(ConfigDetails.Authorization_token) ? string.Empty : ConfigDetails.Authorization_token,
                    //authorization_token = KeyManager.GetValue("authorization_token"),
                    mac_address = AppConstants.MACAddress,
                    serial_number = AppConstants.SerialNumber,
                    device_uuid = AppConstants.UUId,
                };
                var payload = EncryptDecryptData.Encrypt(Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(servicesObject))));

                var formContent = new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("authentication_token", String.IsNullOrEmpty(ConfigDetails.Authentication_token) ? string.Empty : ConfigDetails.Authentication_token) ,
                new KeyValuePair<string, string>("payload", payload),
                new KeyValuePair<string, string>("code_version", AppConstants.CodeVersion),
            };

                var response = await client.PostAsync(AppConstants.EndPoints.DeviceConfigCheck, new FormUrlEncodedContent(formContent));
                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ResponseData>(responseString);
                    apiResponse.Success = true;
                    return apiResponse;
                }
                else
                {
                    ResponseData qRCodeResponse = new ResponseData
                    {
                        HttpStatusCode = response.StatusCode
                    };
                    return qRCodeResponse;
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions and log errors if needed.
                return null;
            }
        }

        public async Task<ResponseData> DeviceReauthAsync()
        {
            try
            {
                var servicesObject = new RetriveServices
                {
                    authorization_token = String.IsNullOrEmpty(ConfigDetails.Authorization_token) ? string.Empty : ConfigDetails.Authorization_token,
                    mac_address = AppConstants.MACAddress,
                    serial_number = AppConstants.SerialNumber,
                    device_uuid = AppConstants.UUId
                };
                var payload = EncryptDecryptData.Encrypt(Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(servicesObject))));

                var formContent = new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("authentication_token", String.IsNullOrEmpty(ConfigDetails.Authentication_token) ? string.Empty : ConfigDetails.Authentication_token) ,
                new KeyValuePair<string, string>("payload", payload),
                new KeyValuePair<string, string>("code_version", AppConstants.CodeVersion),
            };


                var response = await client.PostAsync(AppConstants.EndPoints.DeviceReauth, new FormUrlEncodedContent(formContent));
                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ResponseData>(responseString);
                    apiResponse.Success = true;
                    return apiResponse;
                }
                else
                {
                    ResponseData qRCodeResponse = new ResponseData
                    {
                        HttpStatusCode = response.StatusCode
                    };
                    return qRCodeResponse;
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions and log errors if needed.
                return null;
            }
        }


        public async Task<DeviceResponse> GenerateQRCodeAsync()
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



                var response = await client.PostAsync(AppConstants.EndPoints.Start, new FormUrlEncodedContent(formContent));
                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<DeviceResponse>(responseString);
                    apiResponse.Success = true;
                    return apiResponse;
                }
                else
                {
                    DeviceResponse qRCodeResponse = new DeviceResponse
                    {
                        httpStatusCode = response.StatusCode
                    };
                    return qRCodeResponse;
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions and log errors if needed.
                return null;
            }
        }


        public async Task<ResponseData> GetDeviceDetailsAsync()
        {
            try
            {
                var exchangeObject = new DeviceDetails
                {
                    serial_number = AppConstants.SerialNumber,
                    mac_address = AppConstants.MACAddress,
                    authorization_token = String.IsNullOrEmpty(ConfigDetails.Authorization_token) ? string.Empty : ConfigDetails.Authorization_token,
                    device_uuid = AppConstants.UUId

                };
                var message = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(exchangeObject)));

                var payload = EncryptDecryptData.Encrypt(message);

                var formContent = new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string>("authentication_token", String.IsNullOrEmpty(ConfigDetails.Authentication_token) ? string.Empty : ConfigDetails.Authentication_token) ,
                        new KeyValuePair<string, string>("payload", payload),
                        new KeyValuePair<string, string>("code_version", AppConstants.CodeVersion)

                    };
                var response = await client.PostAsync(AppConstants.EndPoints.DeviceDetails, new FormUrlEncodedContent(formContent));

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<ResponseData>(responseString);
                    apiResponse.Success = true;
                    return apiResponse;
                }
                else
                {
                    ResponseData qRCodeResponse = new ResponseData
                    {
                        HttpStatusCode = response.StatusCode
                    };
                    return qRCodeResponse;
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions and log errors if needed.
                return null;
            }
        }


        public async Task<ResponseData> RetrieveServicesAsync()
        {
            try
            {
                var servicesObject = new RetriveServices
                {
                    authorization_token = String.IsNullOrEmpty(ConfigDetails.Authorization_token) ? string.Empty : ConfigDetails.Authorization_token,
                    mac_address = AppConstants.MACAddress,
                    serial_number = AppConstants.SerialNumber,
                    device_uuid = AppConstants.UUId
                };
                var payload = EncryptDecryptData.Encrypt(Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(servicesObject))));

                var formContent = new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string>("authentication_token", String.IsNullOrEmpty(ConfigDetails.Authentication_token) ? string.Empty : ConfigDetails.Authentication_token) ,
                        new KeyValuePair<string, string>("payload", payload),
                        new KeyValuePair<string, string>("code_version", AppConstants.CodeVersion),
                    };

                var response = await client.PostAsync(AppConstants.EndPoints.DeviceServices, new FormUrlEncodedContent(formContent));

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var responseData = JsonConvert.DeserializeObject<DTO.Responses.ResponseData>(responseString);
                    responseData.Success = true;
                    return responseData;
                }
                else
                {
                    ResponseData qRCodeResponse = new ResponseData
                    {
                        HttpStatusCode = response.StatusCode
                    };
                    return qRCodeResponse;
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions and log errors if needed.
                return null;
            }
        }

        public async Task<ResponseData> UninstallAsync()
        {
            try
            {
                 
                var formContent = new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("mac_address", AppConstants.MACAddress),
                new KeyValuePair<string, string>("serial_number", AppConstants.SerialNumber),
                new KeyValuePair<string, string>("current_user", Environment.UserName),
                new KeyValuePair<string, string>("device_uuid", AppConstants.UUId)
            };

                var response = await client.PostAsync(AppConstants.EndPoints.UninstallDevice, new FormUrlEncodedContent(formContent));

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var responseData = JsonConvert.DeserializeObject<DTO.Responses.ResponseData>(responseString);
                    responseData.Success = true;
                    return responseData;
                }
                else
                {
                    ResponseData qRCodeResponse = new ResponseData
                    {
                        HttpStatusCode = response.StatusCode
                    };
                    return qRCodeResponse;
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions and log errors if needed.
                return null;
            }
        }


        public async Task<ResponseData> UninstallProgramAsync()
        {
            try
            {

                var formContent = new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("mac_address", AppConstants.MACAddress),
                new KeyValuePair<string, string>("serial_number", AppConstants.SerialNumber),
                new KeyValuePair<string, string>("current_user", Environment.UserName),
                new KeyValuePair<string, string>("device_uuid", AppConstants.UUId)
            };

                var response = await client.PostAsync(AppConstants.EndPoints.UninstallCheck, new FormUrlEncodedContent(formContent));

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var responseData = JsonConvert.DeserializeObject<DTO.Responses.ResponseData>(responseString);
                    responseData.Success = true;
                    return responseData;
                }
                else
                {
                    ResponseData qRCodeResponse = new ResponseData
                    {
                        HttpStatusCode = response.StatusCode
                    };
                    return qRCodeResponse;
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions and log errors if needed.
                return null;
            }
        }


        public async Task<ResponseData> AutoUpdateAsync()
        {
            try
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
                    var responseData = JsonConvert.DeserializeObject<DTO.Responses.ResponseData>(responseString);
                    responseData.Success = true;
                    return responseData;
                }
                else
                {
                    ResponseData qRCodeResponse = new ResponseData
                    {
                        HttpStatusCode = response.StatusCode
                    };
                    return qRCodeResponse;
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions and log errors if needed.
                return null;
            }
        }

        public async Task<ResponseData> SendOTPAsync(string email,string phone,string countryCode)
        {
            try
            {

                var formContent = new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string>("token", email),
                        new KeyValuePair<string, string>("phone_no", phone),
                        new KeyValuePair<string, string>("phone_code", countryCode)
                    };


                var response = await client.PostAsync(AppConstants.EndPoints.Otp, new FormUrlEncodedContent(formContent));

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var responseData = JsonConvert.DeserializeObject<DTO.Responses.ResponseData>(responseString);
                    responseData.Success = true;
                    return responseData;
                }
                else
                {
                    ResponseData qRCodeResponse = new ResponseData
                    {
                        HttpStatusCode = response.StatusCode
                    };
                    return qRCodeResponse;
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions and log errors if needed.
                return null;
            }
        }


        public async Task<ResponseData> QRGeneratortimerAsync(string emailToken, string phone, string countryCode,string varificationCode,string qrCodeToken)
        {
            try
            {

                var formContent = new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string>("code_version", AppConstants.CodeVersion),                         
                        new KeyValuePair<string, string>("phone_no", phone),
                        new KeyValuePair<string, string>("phone_code", countryCode),
                        new KeyValuePair<string, string>("otp", varificationCode),
                        new KeyValuePair<string, string>("token", emailToken),
                        new KeyValuePair<string, string>("qr_code_token", qrCodeToken)
                    };


                var response = await client.PostAsync(AppConstants.EndPoints.DeviceToken, new FormUrlEncodedContent(formContent));

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var responseData = JsonConvert.DeserializeObject<DTO.Responses.ResponseData>(responseString);
                    responseData.Success = true;
                    return responseData;
                }
                else
                {
                    ResponseData qRCodeResponse = new ResponseData
                    {
                        HttpStatusCode = response.StatusCode
                    };
                    return qRCodeResponse;
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions and log errors if needed.
                return null;
            }
        }

        // Define other API methods as needed.
    }

}
