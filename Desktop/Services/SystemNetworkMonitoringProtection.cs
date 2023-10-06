using FDS.API_Service;
using FDS.Common;
using FDS.DTO.Responses;
using FDS.Logging;
using FDS.Models;
using FDS.Services.Interface;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.X509;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Protection.PlayReady;

namespace FDS.Services
{
    public class SystemNetworkMonitoringProtection : IService, ILogger
    {

        public bool RunService(SubservicesData subservices, string serviceTypeDetails)
        {
            try
            {
                CertificateDetails certificateDetails = new CertificateDetails();
                var resultCert = certificateDetails.GetCertificates();

                if (!string.IsNullOrEmpty(resultCert.Item1))
                {
                    PostCertificates(resultCert.Item2, subservices, serviceTypeDetails);
                }
            }
            catch (Exception exp)
            {
                return false;
            }
            return true;
        }


        public async void PostCertificates(int cntTotal, SubservicesData subservices, string serviceTypeDetails)
        {
            int countTotal = 0;
            CertificateService certificateService = new CertificateService();
            bool result = await certificateService.CertificateDataAsync(Generic.certificateData);
            if (result)
            {
                countTotal = cntTotal;
            }
            LogInformation(subservices.Sub_service_authorization_code, subservices.Sub_service_name, countTotal, Convert.ToString(subservices.Id), subservices.Execute_now, serviceTypeDetails);

        }


        public void LogInformation(string authorizationCode, string subServiceName, long FileProcessed, string ServiceId, bool IsManualExecution, string serviceTypeDetails)
        {
            DatabaseLogger databaseLogger = new DatabaseLogger();
            databaseLogger.LogInformation(authorizationCode, subServiceName, FileProcessed, ServiceId, IsManualExecution, serviceTypeDetails);
        }


    }
}
