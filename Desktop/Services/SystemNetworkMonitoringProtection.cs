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
                //Certificates
                CertificateDetails certificateDetails = new CertificateDetails();
                var resultCert = certificateDetails.GetCertificates();

                if (!string.IsNullOrEmpty(resultCert.Item1))
                {
                    PostCertificates(resultCert.Item2, subservices, serviceTypeDetails, resultCert.Item1);
                }

                //Proxy
                ProxyDetails proxyDetails = new ProxyDetails();
                var resultProxy = proxyDetails.GetProxyDetails();

                if (!string.IsNullOrEmpty(resultProxy.Item1))
                {
                    PostProxies(subservices, serviceTypeDetails, resultProxy.Item1);
                }                
            }
            catch (Exception exp)
            {
                return false;
            }
            return true;
        }


        public async void PostProxies(SubservicesData subservices, string serviceTypeDetails, string payload)
        {
            //int countTotal = 0;
            ProxyService proxyService = new ProxyService();
            int resultCnt = await proxyService.ProxyDataAsync(payload);

            //if (resultCnt > 0)
            //{
            //    LogInformation(subservices.Sub_service_authorization_code, subservices.Sub_service_name, resultCnt, Convert.ToString(subservices.Id), subservices.Execute_now, serviceTypeDetails);
            //}
        }

        public async void PostCertificates(int cntTotal, SubservicesData subservices, string serviceTypeDetails, string payload)
        {            
            CertificateService certificateService = new CertificateService();
            bool result = await certificateService.CertificateDataAsync(payload);
            if (result)
            {
                //LogInformation(subservices.Sub_service_authorization_code, subservices.Sub_service_name, cntTotal, Convert.ToString(subservices.Id), subservices.Execute_now, serviceTypeDetails);
            }
        }


        public void LogInformation(string authorizationCode, string subServiceName, long FileProcessed, string ServiceId, bool IsManualExecution, string serviceTypeDetails)
        {
            DatabaseLogger databaseLogger = new DatabaseLogger();
            databaseLogger.LogInformation(authorizationCode, subServiceName, FileProcessed, ServiceId, IsManualExecution, serviceTypeDetails);
        }


    }
}
