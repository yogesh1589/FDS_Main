﻿using FDS.Common;
using FDS.DTO.Requests;
using FDS.DTO.Responses;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace FDS.API_Service
{
    public class ApiService
    {
        private readonly HttpClient client;
        public string proxyAddress = string.Empty;
        public string proxyPort = string.Empty;
        bool proxyEnabled = false;

        public ApiService()
        {
            // WebProxy proxy = new WebProxy();
            //HttpClientHandler handler = new HttpClientHandler
            //{
            //    Proxy = proxy,
            //    UseProxy = false // Set UseProxy to false to bypass the proxy.
            //};

            //client = new HttpClient(handler);
            //client = new HttpClient { BaseAddress = AppConstants.EndPoints.BaseAPI };


            // Configure client settings if needed (base URL, headers, etc.).
        }


        public QRCodeResponse CheckAuth(string qrCodeToken, string codeVersion)
        {
            try
            {
                var formContent = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("qr_code_token", qrCodeToken),
            new KeyValuePair<string, string>("code_version", codeVersion),
        };

                var handler = new HttpClientHandler
                {
                    UseProxy = false // Disable using the system proxy
                };

                using (var client1 = new HttpClient(handler))
                {
                    client1.BaseAddress = new Uri(AppConstants.EndPoints.BaseAPI.ToString());
                    var response = client1.PostAsync(AppConstants.EndPoints.CheckAuth, new FormUrlEncodedContent(formContent)).Result; // Use .Result to block until completion

                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = response.Content.ReadAsStringAsync().Result; // Use .Result to block until completion
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
            }
            catch (Exception ex)
            {
                // Handle exceptions and log errors if needed.
                return null;
            }
        }


        public async Task<ResponseData> CheckLicenseAsync(string qrCodeToken, string licenseToken)
        {
            try
            {
                var formContent = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("token", licenseToken),
                new KeyValuePair<string, string>("qr_code_token", qrCodeToken),
            };


                var handler = new HttpClientHandler
                {
                    UseProxy = false // Disable using the system proxy
                };

                using (var client1 = new HttpClient(handler))
                {
                    client1.BaseAddress = new Uri(AppConstants.EndPoints.BaseAPI.ToString());
                    var response = await client1.PostAsync(AppConstants.EndPoints.individual, new FormUrlEncodedContent(formContent));

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
                            HttpStatusCode = response.StatusCode,
                            Success = false

                        };
                        return qRCodeResponse;
                    }
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

                var handler = new HttpClientHandler
                {
                    UseProxy = false // Disable using the system proxy
                };

                using (var client1 = new HttpClient(handler))
                {
                    client1.BaseAddress = new Uri(AppConstants.EndPoints.BaseAPI.ToString());

                    var response = await client1.PostAsync(AppConstants.EndPoints.KeyExchange, new FormUrlEncodedContent(formContent));


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
                            HttpStatusCode = response.StatusCode,
                            Success = false
                        };
                        return qRCodeResponse;
                    }
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

                var handler = new HttpClientHandler
                {
                    UseProxy = false // Disable using the system proxy
                };

                using (var client1 = new HttpClient(handler))
                {
                    client1.BaseAddress = new Uri(AppConstants.EndPoints.BaseAPI.ToString());
                    var response = await client1.PostAsync(AppConstants.EndPoints.DeviceHealth, new FormUrlEncodedContent(formContent));
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
                            HttpStatusCode = response.StatusCode,
                            Success = false
                        };
                        return qRCodeResponse;
                    }
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

                var handler = new HttpClientHandler
                {
                    UseProxy = false // Disable using the system proxy
                };

                using (var client1 = new HttpClient(handler))
                {
                    client1.BaseAddress = new Uri(AppConstants.EndPoints.BaseAPI.ToString());
                    var response = await client1.PostAsync(AppConstants.EndPoints.DeviceConfigCheck, new FormUrlEncodedContent(formContent));
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
                            HttpStatusCode = response.StatusCode,
                            Success = false
                        };
                        return qRCodeResponse;
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions and log errors if needed.
                return null;
            }
        }


        public async Task<DeviceConfigCheckResponse> DeviceConfigurationTestCheckAsync()
        {
            string apiUrl = "https://localhost:7255/api/Values";

            using (HttpClient httpClient = new HttpClient())
            {
                try
                {
                    var handler = new HttpClientHandler
                    {
                        UseProxy = false // Disable using the system proxy
                    };

                    using (var client1 = new HttpClient(handler))
                    {
                        client1.BaseAddress = new Uri(AppConstants.EndPoints.BaseAPI.ToString());
                        HttpResponseMessage response = await client1.GetAsync(apiUrl);

                        // Check if the response status code is successful (2xx)

                        if (response.IsSuccessStatusCode)
                        {
                            string jsonResponse = await response.Content.ReadAsStringAsync();

                            // Deserialize the JSON response into an object
                            DeviceConfigCheckResponse config = Newtonsoft.Json.JsonConvert.DeserializeObject<DeviceConfigCheckResponse>(jsonResponse);

                            return config;
                        }
                    }

                }
                catch (Exception ex)
                {
                    return null;
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
            return null;
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

                var handler = new HttpClientHandler
                {
                    UseProxy = false // Disable using the system proxy
                };

                using (var client1 = new HttpClient(handler))
                {
                    client1.BaseAddress = new Uri(AppConstants.EndPoints.BaseAPI.ToString());

                    var response = await client1.PostAsync(AppConstants.EndPoints.DeviceReauth, new FormUrlEncodedContent(formContent));
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
                            HttpStatusCode = response.StatusCode,
                            Success = false
                        };
                        return qRCodeResponse;
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions and log errors if needed.
                return null;
            }
        }


        public async Task<DeviceResponse> GenerateQRCodeLicenseAsync(string vals)
        {
            try
            {

                // Get the latest txtlicense.Text value just before sending the request
                string licenseText = vals;

                var formContent = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("serial_number", AppConstants.SerialNumber),
                    new KeyValuePair<string, string>("device_name", AppConstants.MachineName),
                    new KeyValuePair<string, string>("mac_address", AppConstants.MACAddress),
                    new KeyValuePair<string, string>("device_type", AppConstants.DeviceType),
                    new KeyValuePair<string, string>("code_version", AppConstants.CodeVersion),
                    new KeyValuePair<string, string>("os_version", AppConstants.OSVersion),
                    new KeyValuePair<string, string>("device_uuid", AppConstants.UUId),
                    new KeyValuePair<string, string>("license_text", licenseText),
                };


                var handler = new HttpClientHandler
                {
                    UseProxy = false // Disable using the system proxy
                };

                using (var client1 = new HttpClient(handler))
                {
                    client1.BaseAddress = new Uri(AppConstants.EndPoints.BaseAPI.ToString());
                    var response = await client1.PostAsync(AppConstants.EndPoints.Start, new FormUrlEncodedContent(formContent));
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
                            httpStatusCode = response.StatusCode,
                            Success = false
                        };
                        return qRCodeResponse;
                    }
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


                var handler = new HttpClientHandler
                {
                    UseProxy = false // Disable using the system proxy
                };

                using (var client1 = new HttpClient(handler))
                {
                    client1.BaseAddress = new Uri(AppConstants.EndPoints.BaseAPI.ToString());
                    var response = await client1.PostAsync(AppConstants.EndPoints.Start, new FormUrlEncodedContent(formContent));
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
                            httpStatusCode = response.StatusCode,
                            Success = false
                        };
                        return qRCodeResponse;
                    }
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


                var handler = new HttpClientHandler
                {
                    UseProxy = false // Disable using the system proxy
                };

                using (var client1 = new HttpClient(handler))
                {
                    client1.BaseAddress = new Uri(AppConstants.EndPoints.BaseAPI.ToString());
                    var response = await client1.PostAsync(AppConstants.EndPoints.DeviceDetails, new FormUrlEncodedContent(formContent));

                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = await response.Content.ReadAsStringAsync();
                        var apiResponse = JsonConvert.DeserializeObject<ResponseData>(responseString);
                        apiResponse.Success = true;
                        apiResponse.HttpStatusCode = HttpStatusCode.OK;
                        return apiResponse;
                    }
                    else
                    {
                        ResponseData qRCodeResponse = new ResponseData
                        {
                            HttpStatusCode = response.StatusCode,
                            Success = false
                        };
                        return qRCodeResponse;
                    }
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

                var handler = new HttpClientHandler
                {
                    UseProxy = false // Disable using the system proxy
                };

                using (var client1 = new HttpClient(handler))
                {
                    client1.BaseAddress = new Uri(AppConstants.EndPoints.BaseAPI.ToString());
                    var response = await client1.PostAsync(AppConstants.EndPoints.DeviceServices, new FormUrlEncodedContent(formContent));

                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = await response.Content.ReadAsStringAsync();
                        var responseData = JsonConvert.DeserializeObject<DTO.Responses.ResponseData>(responseString);
                        responseData.Success = true;
                        response.StatusCode = HttpStatusCode.OK;
                        return responseData;
                    }
                    else
                    {
                        ResponseData qRCodeResponse = new ResponseData
                        {
                            HttpStatusCode = response.StatusCode,
                            Success = false
                        };
                        return qRCodeResponse;
                    }
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

                var handler = new HttpClientHandler
                {
                    UseProxy = false // Disable using the system proxy
                };

                using (var client1 = new HttpClient(handler))
                {
                    client1.BaseAddress = new Uri(AppConstants.EndPoints.BaseAPI.ToString());
                    var response = await client1.PostAsync(AppConstants.EndPoints.UninstallDevice, new FormUrlEncodedContent(formContent));

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
                            HttpStatusCode = response.StatusCode,
                            Success = false
                        };
                        return qRCodeResponse;
                    }
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

                var handler = new HttpClientHandler
                {
                    UseProxy = false // Disable using the system proxy
                };

                using (var client1 = new HttpClient(handler))
                {
                    client1.BaseAddress = new Uri(AppConstants.EndPoints.BaseAPI.ToString());
                    var response = await client1.PostAsync(AppConstants.EndPoints.UninstallCheck, new FormUrlEncodedContent(formContent));

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
                            HttpStatusCode = response.StatusCode,
                            Success = false
                        };
                        return qRCodeResponse;
                    }
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

                var handler = new HttpClientHandler
                {
                    UseProxy = false // Disable using the system proxy
                };

                using (var client1 = new HttpClient(handler))
                {
                    client1.BaseAddress = new Uri(AppConstants.EndPoints.BaseAPI.ToString());
                    var response = await client1.PostAsync(AppConstants.EndPoints.AutoUpdate, new FormUrlEncodedContent(formContent));

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
                            HttpStatusCode = response.StatusCode,
                            Success = false
                        };
                        return qRCodeResponse;
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions and log errors if needed.
                return null;
            }
        }




        public async Task<ResponseData> SendOTPAsync(string email, string phone, string countryCode)
        {
            try
            {

                var formContent = new List<KeyValuePair<string, string>> {
                        new KeyValuePair<string, string>("token", email),
                        new KeyValuePair<string, string>("phone_no", phone),
                        new KeyValuePair<string, string>("phone_code", countryCode)
                    };


                var handler = new HttpClientHandler
                {
                    UseProxy = false // Disable using the system proxy
                };

                using (var client1 = new HttpClient(handler))
                {
                    client1.BaseAddress = new Uri(AppConstants.EndPoints.BaseAPI.ToString());
                    var response = await client1.PostAsync(AppConstants.EndPoints.Otp, new FormUrlEncodedContent(formContent));

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
                            HttpStatusCode = response.StatusCode,
                            Success = false
                        };
                        return qRCodeResponse;
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions and log errors if needed.
                return null;
            }
        }

        public async Task<HealthScoreDetails> GetHealthscoreAsync()
        {
            try
            {

                var handler = new HttpClientHandler
                {
                    UseProxy = false // Disable using the system proxy
                };

                using (var client1 = new HttpClient(handler))
                {
                    client1.BaseAddress = new Uri(AppConstants.EndPoints.BaseAPI.ToString());
                    var response = await client1.GetAsync(AppConstants.EndPoints.healthscore + "?device_uuid=" + AppConstants.UUId);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = await response.Content.ReadAsStringAsync();
                        HealthScoreDetails healthScore = JsonConvert.DeserializeObject<HealthScoreDetails>(responseString);


                        healthScore.HttpStatusCode = HttpStatusCode.OK;
                        healthScore.Success = true;
                       
                        return healthScore;
                    }
                    else
                    {
                        HealthScoreDetails healthScore = new HealthScoreDetails();

                        healthScore.HttpStatusCode = HttpStatusCode.OK;
                        healthScore.Success = true;
                        
                        return healthScore;
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions and log errors if needed.
                return null;
            }
        }



        public HealthScoreDetails GetHealthscore()
        {
            try
            {
                var handler = new HttpClientHandler
                {
                    UseProxy = false // Disable using the system proxy
                };

                using (var client1 = new HttpClient(handler))
                {
                    client1.BaseAddress = new Uri(AppConstants.EndPoints.BaseAPI.ToString());
                    var response = client1.GetAsync(AppConstants.EndPoints.healthscore + "?device_uuid=" + AppConstants.UUId).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = response.Content.ReadAsStringAsync().Result;                       
                        HealthScoreDetails healthScore = JsonConvert.DeserializeObject<HealthScoreDetails>(responseString);

                        healthScore.HttpStatusCode = HttpStatusCode.OK;
                        healthScore.Success = true;
                        
                        return healthScore;
                    }
                    else
                    {
                        // If the response is not successful, handle accordingly
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions and log errors if needed.
                return null;
            }
        }


        public async Task<ServiceResponseNew> GetServiceInfoAsync()
        {
            try
            {
                var handler = new HttpClientHandler
                {
                    UseProxy = false // Disable using the system proxy
                };

                using (var client1 = new HttpClient(handler))
                {
                    client1.BaseAddress = new Uri(AppConstants.EndPoints.BaseAPI.ToString());
                    var getresponse = await client1.GetAsync(AppConstants.EndPoints.serviceinfo + "?device_uuid=" + AppConstants.UUId);
                    if (getresponse.IsSuccessStatusCode)
                    {
                        var responseString = await getresponse.Content.ReadAsStringAsync();

                        // Deserialize the JSON response into an object
                        ServiceResponseNew serviceResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<ServiceResponseNew>(responseString);
                        serviceResponse.HttpStatusCode = HttpStatusCode.OK;
                        serviceResponse.Success = true;
                        return serviceResponse;
                    }
                    else
                    {
                        ServiceResponseNew qRCodeResponse = new ServiceResponseNew
                        {
                            HttpStatusCode = getresponse.StatusCode,
                            Success = false
                        };
                        return qRCodeResponse;
                    }
                }
            }


            catch (Exception ex)
            {
                // Handle exceptions and log errors if needed.
                return null;
            }

            return null;
        }


        

        public async Task<List<DTO.Responses.LogEntry>> GetServiceInfoAsync(int serviceID, int pageSize, int page)
        {
            try
            {
                var handler = new HttpClientHandler { UseProxy = false };
                using (var client1 = new HttpClient(handler))
                {
                    client1.BaseAddress = new Uri(AppConstants.EndPoints.BaseAPI.ToString());
                    var getresponse = await client1.GetAsync($"{AppConstants.EndPoints.serviceinfo}?device_uuid={AppConstants.UUId}&subservices_id={serviceID}&page_size={pageSize}&page={page}");
                    if (getresponse.IsSuccessStatusCode)
                    {
                        var responseString = await getresponse.Content.ReadAsStringAsync();
                        LogResponse logResponse = JsonConvert.DeserializeObject<LogResponse>(responseString);

                        if (logResponse != null && logResponse.data != null)
                        {
                            var logEntries = logResponse.data.logs;
                            // Now you can work with logEntries, which is a List<LogEntry>
                        }
                        else
                        {
                            return null;
                        }
                        return logResponse.data.logs;
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions appropriately (e.g., logging)
            }
            return new List<DTO.Responses.LogEntry>();
        }




        public async Task<ResponseData> QRGeneratortimerAsync(string emailToken, string phone, string countryCode, string varificationCode, string qrCodeToken)
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


                var handler = new HttpClientHandler
                {
                    UseProxy = false // Disable using the system proxy
                };

                using (var client1 = new HttpClient(handler))
                {
                    client1.BaseAddress = new Uri(AppConstants.EndPoints.BaseAPI.ToString());
                    var response = await client1.PostAsync(AppConstants.EndPoints.DeviceToken, new FormUrlEncodedContent(formContent));

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
                            HttpStatusCode = response.StatusCode,
                            Success = false
                        };
                        return qRCodeResponse;
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions and log errors if needed.
                return null;
            }
        }


        public async Task<bool> DownloadURLAsync(string downloadUrl, string temporaryMSIPath)
        {

            var handler = new HttpClientHandler
            {
                UseProxy = false // Disable using the system proxy
            };

            try
            {
                using (var client1 = new HttpClient(handler))
                {
                    client1.BaseAddress = new Uri(AppConstants.EndPoints.BaseAPI.ToString());
                    HttpResponseMessage response = await client1.GetAsync(downloadUrl);
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
            }
            catch { return false; }
            //WebRequest.DefaultWebProxy = null;

            //using (var client1 = new WebClient())
            //{
            //    int maxRetries = 3;
            //    int retryCount = 0;

            //    while (retryCount < maxRetries)
            //    {
            //        try
            //        {
            //            // Download logic here
            //            await client1.DownloadFileTaskAsync(new Uri(downloadUrl), temporaryMSIPath);
            //            Console.WriteLine($"File downloaded to: {temporaryMSIPath}");
            //            break; // Break out of the loop if download is successful
            //        }
            //        catch (Exception ex)
            //        {
            //            Console.WriteLine($"Attempt {retryCount + 1} failed. Retrying... Error: {ex.Message}");
            //            retryCount++;
            //        }
            //    }
            //}

            return true;
        }
        // Define other API methods as needed.
    }

}

