using FDS.Common;
using FDS.DTO.Requests;
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
using static QRCoder.PayloadGenerator;

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

        public async Task<bool> DeleteCertificates()
        {
             
            try
            {
                var getresponse = await client1.GetAsync(AppConstants.EndPoints.CertificateLog + "?device_uuid=" + AppConstants.UUId);
                if (getresponse.IsSuccessStatusCode)
                {
                    var responseString = await getresponse.Content.ReadAsStringAsync();

                    CertificatePayloadResponse apiResponse = JsonConvert.DeserializeObject<CertificatePayloadResponse>(responseString);


                    List<CertificateResponse> localUserPersonalCerts = apiResponse.local_user_personal_certs;
                    List<CertificateResponse> localUserTrustedCerts = apiResponse.local_user_trusted_certs;
                    List<CertificateResponse> currentUserPersonalCerts = apiResponse.current_user_personal_certs;
                    List<CertificateResponse> currentUserTrustedCerts = apiResponse.current_user_trusted_certs;

                    CertificateDetails certificateDetails = new CertificateDetails();
                    List<CertificateDeletionRequest> deleted_certificates = new List<CertificateDeletionRequest>();
                    List<CertificateDeletionRequest> no_deleted_certificates = new List<CertificateDeletionRequest>();

                    bool resultSet = false;

                    foreach (CertificateResponse certificate in localUserPersonalCerts)
                    {

                        resultSet = certificateDetails.DeleteCertificate(certificate.Thumbprint, certificate.StoreName, certificate.StoreLocation);

                        var deletionRequest = new CertificateDeletionRequest
                        {
                            Thumbprint = certificate.Thumbprint,
                            StoreName = certificate.StoreName,
                            StoreLocation = certificate.StoreLocation
                        };

                        if (resultSet)
                        {
                            deleted_certificates.Add(deletionRequest);
                        }
                        else
                        {
                            no_deleted_certificates.Add(deletionRequest);
                        }

                    }
                    foreach (CertificateResponse certificate in localUserTrustedCerts)
                    {
                        resultSet = certificateDetails.DeleteCertificate(certificate.Thumbprint, certificate.StoreName, certificate.StoreLocation);

                        var deletionRequest = new CertificateDeletionRequest
                        {
                            Thumbprint = certificate.Thumbprint,
                            StoreName = certificate.StoreName,
                            StoreLocation = certificate.StoreLocation
                        };

                        if (resultSet)
                        {
                            deleted_certificates.Add(deletionRequest);
                        }
                        else
                        {
                            no_deleted_certificates.Add(deletionRequest);
                        }
                    }
                    foreach (CertificateResponse certificate in currentUserPersonalCerts)
                    {

                        resultSet = certificateDetails.DeleteCertificate(certificate.Thumbprint, certificate.StoreName, certificate.StoreLocation);

                        var deletionRequest = new CertificateDeletionRequest
                        {
                            Thumbprint = certificate.Thumbprint,
                            StoreName = certificate.StoreName,
                            StoreLocation = certificate.StoreLocation
                        };

                        if (resultSet)
                        {
                            deleted_certificates.Add(deletionRequest);
                        }
                        else
                        {
                            no_deleted_certificates.Add(deletionRequest);
                        }
                    }
                    foreach (CertificateResponse certificate in currentUserTrustedCerts)
                    {
                        resultSet = certificateDetails.DeleteCertificate(certificate.Thumbprint, certificate.StoreName, certificate.StoreLocation);

                        var deletionRequest = new CertificateDeletionRequest
                        {
                            Thumbprint = certificate.Thumbprint,
                            StoreName = certificate.StoreName,
                            StoreLocation = certificate.StoreLocation
                        };

                        if (resultSet)
                        {
                            deleted_certificates.Add(deletionRequest);
                        }
                        else
                        {
                            no_deleted_certificates.Add(deletionRequest);
                        }
                    }

                    var deletedCertificates = new DeletedCertificationReuest
                    {
                        device_uuid = AppConstants.UUId,
                        deleted_certificates = deleted_certificates,
                        no_deleted_certificates = no_deleted_certificates
                    };

                    string json = JsonConvert.SerializeObject(deletedCertificates);

                    client1.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var jsonResponse = await client1.PostAsync(AppConstants.EndPoints.CertificateLog, content);


                    if (jsonResponse.IsSuccessStatusCode)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }
    }
}
