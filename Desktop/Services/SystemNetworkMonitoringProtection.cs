using FDS.DTO.Responses;
using FDS.Logging;
using FDS.Services.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace FDS.Services
{
    public class SystemNetworkMonitoringProtection : IService, ILogger
    {
        public bool RunService(SubservicesData subservices, string serviceTypeDetails)
        {
            try
            {

                GetCertificates();

                LogInformation(subservices.Sub_service_authorization_code, subservices.Sub_service_name, 0, Convert.ToString(subservices.Id), subservices.Execute_now, serviceTypeDetails);

            }
            catch (Exception exp)
            {
                return false;
            }
            return true;
        }

        public void GetCertificates()
        {
            try
            {
                // Open the X.509 certificate store (you can change the store name as needed)
                X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly);

                // Enumerate through all certificates in the store
                foreach (X509Certificate2 certificate in store.Certificates)
                {
                    // Print certificate properties
                    Console.WriteLine("Subject: " + certificate.Subject);
                    Console.WriteLine("Thumbprint: " + certificate.Thumbprint);
                    Console.WriteLine("Friendly Name: " + certificate.FriendlyName);
                    Console.WriteLine("=====================================");
                }

                // Close the certificate store
                store.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        public void LogInformation(string authorizationCode, string subServiceName, long FileProcessed, string ServiceId, bool IsManualExecution, string serviceTypeDetails)
        {
            DatabaseLogger databaseLogger = new DatabaseLogger();
            databaseLogger.LogInformation(authorizationCode, subServiceName, FileProcessed, ServiceId, IsManualExecution, serviceTypeDetails);
        }


    }
}
