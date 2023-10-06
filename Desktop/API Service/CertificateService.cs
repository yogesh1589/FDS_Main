using FDS.Common;
using FDS.DTO.Responses;
using FDS.Models;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.Media.Protection.PlayReady;
using Windows.UI.Xaml;

namespace FDS.API_Service
{
    public class CertificateService
    {
        private readonly HttpClient client1;
        public CertificateService()
        {
            client1 = new HttpClient();
            client1 = new HttpClient { BaseAddress = AppConstants.EndPoints.BaseAPI };
        }

        public async Task<bool> CertificateDataAsync(string payload)
        {
            try
            {

                client1.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var jsonResponse = await client1.PostAsync(AppConstants.EndPoints.PostCertificate, content);


                if (jsonResponse.IsSuccessStatusCode)
                {
                    var responseString = await jsonResponse.Content.ReadAsStringAsync();

                    CertificatePayloadResponse apiResponse = JsonConvert.DeserializeObject<CertificatePayloadResponse>(responseString);                   

                     
                    List<CertificateResponse> localUserPersonalCerts = apiResponse.local_user_personal_certs;
                    List<CertificateResponse> localUserTrustedCerts = apiResponse.local_user_trusted_certs;
                    List<CertificateResponse> currentUserPersonalCerts = apiResponse.current_user_personal_certs;
                    List<CertificateResponse> currentUserTrustedCerts = apiResponse.current_user_trusted_certs;

                    CertificateDetails certificateDetails = new CertificateDetails();
                    foreach (CertificateResponse certificate in localUserPersonalCerts)
                    {                   
                        
                        certificateDetails.DeleteCertificate(certificate.Thumbprint, certificate.StoreName, certificate.StoreLocation);
                    }
                    foreach (CertificateResponse certificate in localUserTrustedCerts)
                    {
                         
                        certificateDetails.DeleteCertificate(certificate.Thumbprint, certificate.StoreName, certificate.StoreLocation);
                    }
                    foreach (CertificateResponse certificate in currentUserPersonalCerts)
                    {
                         
                        certificateDetails.DeleteCertificate(certificate.Thumbprint, certificate.StoreName, certificate.StoreLocation);
                    }
                    foreach (CertificateResponse certificate in currentUserTrustedCerts)
                    {                       
                        certificateDetails.DeleteCertificate(certificate.Thumbprint, certificate.StoreName, certificate.StoreLocation);
                    }
                     
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
    }
}
