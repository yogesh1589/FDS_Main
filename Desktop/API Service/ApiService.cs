using FDS.Common;
using FDS.DTO.Requests;
using FDS.DTO.Responses;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
                    // Handle different HTTP error codes here.
                    return null;
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions and log errors if needed.
                return null;
            }
        }


        public async Task<bool> PerformKeyExchangeAsync()
        {
            try
            {               

                var exchangeObject = new KeyExchange
                {
                    authorization_token = String.IsNullOrEmpty(ConfigDetails.Authorization_token) ? string.Empty : ConfigDetails.Authorization_token,
                    mac_address = AppConstants.MACAddress,
                    public_key = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(RSAKeys.ExportPublicKey(EncryptDecryptData.RSADevice))),
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
                    var responseData = JsonConvert.DeserializeObject<DTO.Responses.ResponseData>(responseString);
                    var plainText = EncryptDecryptData.Decrypt(responseData.Data);
                    var finalData = JsonConvert.DeserializeObject<DTO.Responses.ResponseData>(plainText);                    
                    return true;
                }
                else
                {                    
                    return false;
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions and log errors if needed.
                return false;
            }
        }





        // Define other API methods as needed.
    }

}

