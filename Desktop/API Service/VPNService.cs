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

namespace FDS.API_Service
{
    public class VPNService
    {
        public async Task<VPNServiceRequest> VPNConnectAsync()
        {
            try
            {

                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, "https://vcs.fusiondatasecure.com/api/client/connect");
                request.Headers.Add("x-api-region", "us-east-1");
                var content = new StringContent("{\n    \"device_id\": \"" + AppConstants.UUId + "\"\n}", null, "application/json");
                request.Content = content;
                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    response.EnsureSuccessStatusCode();
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    var config = Newtonsoft.Json.JsonConvert.DeserializeObject<VPNServiceRequest>(jsonResponse);
                    return config;
                }
                //string apiUrl = "https://vcs.fusiondatasecure.com/api/";

                //var formContent = new List<KeyValuePair<string, string>>
                //{
                //    new KeyValuePair<string, string>("device_id", AppConstants.UUId)
                //};

                //var handler = new HttpClientHandler
                //{
                //    UseProxy = false // Disable using the system proxy
                //};

                //using (var client1 = new HttpClient(handler))
                //{
                //    client1.BaseAddress = new Uri(apiUrl);
                //    var response = await client1.PostAsync(AppConstants.EndPoints.vpnConnect, new FormUrlEncodedContent(formContent));

                //    if (response.IsSuccessStatusCode)
                //    {
                //        string jsonResponse = await response.Content.ReadAsStringAsync();

                //        // Deserialize the JSON response into an object
                //        var config = Newtonsoft.Json.JsonConvert.DeserializeObject<VPNServiceRequest>(jsonResponse);

                //        return config;
                //    }
                //}
            }
            catch (Exception ex)
            {
                // Handle exceptions and log errors if needed.
                return null;
            }
            return null;
        }


    }
}
