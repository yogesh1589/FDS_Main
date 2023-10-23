using FDS.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace FDS.Common
{
    public class CertificateDetails
    {


        public (string, int) GetCertificates()
        {
            int cntCertif = 0;
            string jsonMergedList = string.Empty;
            try
            {

                List<CertificationLists> lstLocalMachinePersonal = GetCertificates_LocalMachine_My();
                List<CertificationLists> lstLocalMachineTrusted = GetCertificates_LocalMachine_Root();
                List<CertificationLists> lstCurrentUserPersonal = GetCertificates_CurrentUser_My();
                List<CertificationLists> lstCurrentUserTrusted = GetCertificates_CurrentUser_Root();

                cntCertif = lstLocalMachinePersonal.Count + lstLocalMachineTrusted.Count + lstCurrentUserPersonal.Count + lstCurrentUserTrusted.Count;


                var certificateData = new CertificateData
                {
                    device_uuid = AppConstants.UUId,
                    payload = new Payload
                    {
                        local_user_personal_certs = lstLocalMachinePersonal,
                        local_user_trusted_certs = lstLocalMachineTrusted,
                        current_user_personal_certs = lstCurrentUserPersonal,
                        current_user_trusted_certs = lstCurrentUserTrusted
                    }
                };

                jsonMergedList = JsonConvert.SerializeObject(certificateData);

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            return (jsonMergedList, cntCertif);
        }

        public List<CertificationLists> GetCertificates(X509Store store, string storeName, string storeLocation)
        {
            List<CertificationLists> certificateList = new List<CertificationLists>();
            store.Open(OpenFlags.ReadOnly);

            // Enumerate through all certificates in the store
            foreach (X509Certificate2 certificate in store.Certificates)
            {
                // Add certificate data to the list
                certificateList.Add(new CertificationLists
                {
                    Subject = string.IsNullOrEmpty(certificate.Subject) ? string.Empty : certificate.Subject,
                    Thumbprint = string.IsNullOrEmpty(certificate.Thumbprint) ? string.Empty : certificate.Thumbprint,
                    FriendlyName = string.IsNullOrEmpty(certificate.FriendlyName) ? string.Empty : certificate.FriendlyName,
                    Version = certificate.Version.ToString(),
                    SerialNumber = string.IsNullOrEmpty(certificate.SerialNumber) ? string.Empty : certificate.SerialNumber,
                    SignatureAlgorithm = string.IsNullOrEmpty(certificate.SignatureAlgorithm.FriendlyName) ? string.Empty : certificate.SignatureAlgorithm.FriendlyName.ToString(),
                    Issuer = string.IsNullOrEmpty(certificate.Issuer) ? string.Empty : certificate.Issuer,
                    ValidFrom = certificate.NotBefore,
                    ValidTo = certificate.NotAfter,
                    PublicKey = string.IsNullOrEmpty(certificate.PublicKey.Oid.FriendlyName) ? string.Empty : certificate.PublicKey.Oid.FriendlyName.ToString(),
                    StoreName = storeName,
                    StoreLocation = storeLocation
                });
            }

            // Serialize the list to JSON
            //string json = JsonConvert.SerializeObject(certificateList, Newtonsoft.Json.Formatting.Indented);

            store.Close();
            return certificateList;

        }

        public List<CertificationLists> GetCertificates_LocalMachine_My()
        {
            // My Store - LocalMachine
            List<CertificationLists> myCertificateList = new List<CertificationLists>();
            X509Store myStore = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            myCertificateList = GetCertificates(myStore, "Personal", "LocalComputer");
            return myCertificateList;
        }

        public List<CertificationLists> GetCertificates_LocalMachine_Root()
        {
            // Root Store - LocalMachine
            List<CertificationLists> rootCertificateList = new List<CertificationLists>();
            X509Store rootStore = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            rootCertificateList = GetCertificates(rootStore, "Trusted", "LocalComputer");
            return rootCertificateList;
        }

        public List<CertificationLists> GetCertificates_CurrentUser_My()
        {
            // My Store - CurrentUser
            List<CertificationLists> myCertificateListCurrentUser = new List<CertificationLists>();
            X509Store myStoreCurrentUser = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            myCertificateListCurrentUser = GetCertificates(myStoreCurrentUser, "Personal", "CurrentUser");
            return myCertificateListCurrentUser;

        }

        public List<CertificationLists> GetCertificates_CurrentUser_Root()
        {
            // Root Store - CurrentUser
            List<CertificationLists> rootCertificateListCurrentUser = new List<CertificationLists>();
            X509Store rootStoreCurrentUser = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            rootCertificateListCurrentUser = GetCertificates(rootStoreCurrentUser, "Trusted", "CurrentUser");
            return rootCertificateListCurrentUser;

        }



        public bool DeleteCertificate(string certificateThumbprint, string storename, string storeLocation)
        {
            bool result = false;

            StoreName storeNameV = StoreName.My;
            StoreLocation storeLocationV = StoreLocation.LocalMachine;

            if ((storename == "My") && (storeLocation == "LocalMachine"))
            {
                storeNameV = StoreName.My;
                storeLocationV = StoreLocation.LocalMachine;
            }
            else if ((storename == "Root") && (storeLocation == "LocalMachine"))
            {
                storeNameV = StoreName.Root;
                storeLocationV = StoreLocation.LocalMachine;
            }
            else if ((storename == "Root") && (storeLocation == "CurrentUser"))
            {
                storeNameV = StoreName.Root;
                storeLocationV = StoreLocation.CurrentUser;
            }
            else if ((storename == "My") && (storeLocation == "CurrentUser"))
            {
                storeNameV = StoreName.My;
                storeLocationV = StoreLocation.CurrentUser;
            }
            try
            {
                result = DeleteCertificationDetails(certificateThumbprint, storeNameV, storeLocationV);

            }
            catch (Exception ex)
            {
                return false;                 
            }             
            return result;
        }

        public bool DeleteCertificationDetails(string certificateThumbprint, StoreName storeName, StoreLocation storeLocation)
        {
            bool result = false;
            try
            {
                // Open the certificate store
                using (X509Store store = new X509Store(storeName, storeLocation))
                {
                    store.Open(OpenFlags.ReadWrite); // Open the store for writing

                    // Find the certificate by thumbprint
                    X509Certificate2Collection certificates = store.Certificates.Find(
                        X509FindType.FindByThumbprint,
                        certificateThumbprint,
                        false); // Set to true to do partial matching

                    // Check if the certificate was found
                    if (certificates.Count > 0)
                    {
                        result = true;
                        // Remove the certificate from the store
                        store.RemoveRange(certificates);
                        Console.WriteLine("Certificate removed successfully.");
                    }
                    else
                    {
                        result = false;
                        Console.WriteLine("Certificate not found in the store.");
                    }
                }
            }
            catch
            {
                return false;
            }

            return result;
        }
    }
}
