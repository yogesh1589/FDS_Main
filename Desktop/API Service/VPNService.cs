using FDS.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FDS.DTO.Responses;
using FDS.DTO.Requests;
using System.Net;
using System.IO;
using System.Net.Sockets;
using Tunnel;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace FDS.API_Service
{
    public class VPNService
    {
        public async Task<ResponseData> VPNConnectAsync()
        {
            try
            {


                var servicesObject = new RetriveServices
                {
                    authorization_token = String.IsNullOrEmpty(ConfigDetails.Authorization_token) ? string.Empty : ConfigDetails.Authorization_token,                     
                    mac_address = AppConstants.MACAddress,
                    serial_number = AppConstants.SerialNumber,
                    current_user = Environment.UserName,
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
                    client1.DefaultRequestHeaders.Add("x-api-region", "us-east-1");
                    var response = await client1.PostAsync(AppConstants.EndPoints.vpnService, new FormUrlEncodedContent(formContent));
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
            return null;
        }

        public async Task<string> GetPublicIpAddressAsync()
        {
            using (HttpClient httpClient = new HttpClient())
            {
                return await httpClient.GetStringAsync("https://api.ipify.org");
            }
        }

        public async Task<string> GetIpLocationAsync(string ipAddress)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                string apiUrl = $"http://ip-api.com/json/{ipAddress}";
                string response = await httpClient.GetStringAsync(apiUrl);

                JObject locationInfo = JObject.Parse(response);
                if ((string)locationInfo["status"] == "success")
                {
                    return (string)locationInfo["regionName"]; // Extract the state
                }
                else
                {
                    return "Location information not available.";
                }
            }
        }


    }
}
