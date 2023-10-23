using FDS.Common;
using FDS.DTO.Responses;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace FDS.API_Service
{
    public class ProxyService
    {
        private readonly HttpClient client1;

        public ProxyService()
        {
            client1 = new HttpClient();
            client1 = new HttpClient { BaseAddress = AppConstants.EndPoints.BaseAPI };
        }

        public async Task<int> ProxyDataAsync(string payload)
        {
            int cntTotal = 0;
            try
            {

                client1.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var jsonResponse = await client1.PostAsync(AppConstants.EndPoints.PostProxy, content);


                if (jsonResponse.IsSuccessStatusCode)
                {
                    var responseString = await jsonResponse.Content.ReadAsStringAsync();

                    //CertificatePayloadResponse apiResponse = JsonConvert.DeserializeObject<CertificatePayloadResponse>(responseString);

                }
            }
            catch (Exception ex)
            {
                // Handle exceptions and log errors if needed.
                return cntTotal;
            }
            return cntTotal;
        }
    }
}
