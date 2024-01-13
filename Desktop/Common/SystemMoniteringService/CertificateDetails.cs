using FDS.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
                //List<CertificationLists> lstLocalMachineTrusted = GetCertificates_LocalMachine_Root();
                List<CertificationLists> lstCurrentUserPersonal = GetCertificates_CurrentUser_My();
                //List<CertificationLists> lstCurrentUserTrusted = GetCertificates_CurrentUser_Root();

                // cntCertif = lstLocalMachinePersonal.Count + lstLocalMachineTrusted.Count + lstCurrentUserPersonal.Count + lstCurrentUserTrusted.Count;

                cntCertif = lstLocalMachinePersonal.Count + lstCurrentUserPersonal.Count;


                var certificateData = new CertificateData
                {
                    device_uuid = AppConstants.UUId,
                    payload = new Payload
                    {
                        local_user_personal_certs = lstLocalMachinePersonal,
                        //local_user_trusted_certs = lstLocalMachineTrusted,
                        current_user_personal_certs = lstCurrentUserPersonal,
                        //current_user_trusted_certs = lstCurrentUserTrusted
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


        public List<CertificationLists> GetCertificatesTrusted(string storeLocation)
        {
            List<CertificationLists> certificateList = new List<CertificationLists>();

            StoreLocation storeLocationV = StoreLocation.LocalMachine;

            if (storeLocation == "CurrentUser")
            { storeLocationV = StoreLocation.CurrentUser; }
            else
            {
                storeLocationV = StoreLocation.LocalMachine;
            }
            X509Store rootStore = new X509Store(StoreName.Root, storeLocationV);
            X509Store caStore = new X509Store(StoreName.CertificateAuthority, storeLocationV);

            rootStore.Open(OpenFlags.ReadOnly);
            caStore.Open(OpenFlags.ReadOnly);

            List<X509Certificate2> commonCertificates = FindCommonCertificates(rootStore, caStore);

            rootStore.Close();
            caStore.Close();

            // Enumerate through all certificates in the store
            foreach (X509Certificate2 certificate in commonCertificates)
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
                    StoreName = "Trusted",
                    StoreLocation = storeLocation
                });
            }

            // Serialize the list to JSON
            //string json = JsonConvert.SerializeObject(certificateList, Newtonsoft.Json.Formatting.Indented);


            return certificateList;

        }

        public List<X509Certificate2> FindCommonCertificates(X509Store store1, X509Store store2)
        {
            List<X509Certificate2> commonCertificates = new List<X509Certificate2>();

            foreach (X509Certificate2 cert1 in store1.Certificates)
            {
                foreach (X509Certificate2 cert2 in store2.Certificates)
                {
                    if (cert1.Thumbprint == cert2.Thumbprint)
                    {
                        commonCertificates.Add(cert1);
                        break; // Once a match is found, no need to continue searching in store2
                    }
                }
            }

            return commonCertificates;
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
            rootCertificateList = GetCertificatesTrusted("LocalComputer");
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

            rootCertificateListCurrentUser = GetCertificatesTrusted("CurrentUser");
            return rootCertificateListCurrentUser;

        }



        public bool DeleteCertificate(string certificateThumbprint, string storename, string storeLocation)
        {
            bool result = false;

            StoreName storeNameV = StoreName.My;
            StoreLocation storeLocationV = StoreLocation.LocalMachine;

            if ((storename == "Personal") && (storeLocation == "LocalComputer"))
            {
                storeNameV = StoreName.My;
                storeLocationV = StoreLocation.LocalMachine;
            }
            else if ((storename == "Trusted") && (storeLocation == "LocalComputer"))
            {
                storeNameV = StoreName.Root;
                storeLocationV = StoreLocation.LocalMachine;
            }
            else if ((storename == "Trusted") && (storeLocation == "CurrentUser"))
            {
                storeNameV = StoreName.Root;
                storeLocationV = StoreLocation.CurrentUser;
            }
            else if ((storename == "Personal") && (storeLocation == "CurrentUser"))
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

            try
            {
                //string AutoStartBaseDir = Generic.GetApplicationpath();
                //string exeFile1 = Path.Combine(AutoStartBaseDir, "FDS_Administrator.exe");

                //try
                //{
                //    string methodName = "Certificates";
                //    string storeLocationN = storeLocation.ToString();
                //    string storeNameN = storeName.ToString();

                //    // Concatenate method name and parameters into a single string with a delimiter
                //    string arguments = $"{methodName},{certificateThumbprint},{storeLocation},{storeName}";

                //    if (storeLocationN == "CurrentUser" && (!Generic.IsUserAdministrator()))
                //    {
                //        //ProcessStartInfo psi = new ProcessStartInfo
                //        //{
                //        //    FileName = exeFile1, // Replace with your console application's executable                           
                //        //    UseShellExecute = true, // Use the shell execution
                //        //    CreateNoWindow = true,
                //        //    Arguments = arguments // Pass concatenated string as command-line argument
                //        //    //WindowStyle = ProcessWindowStyle.Hidden,// Set the window style to hidden        
                //        //};
                //        //Process.Start(psi);
                //        using (X509Store store = new X509Store(storeName, storeLocation))
                //        {
                //            store.Open(OpenFlags.ReadWrite); // Open the store for writing

                //            // Find the certificate by thumbprint
                //            X509Certificate2Collection certificates = store.Certificates.Find(
                //                X509FindType.FindByThumbprint,
                //                certificateThumbprint,
                //                false); // Set to true to do partial matching

                //            // Check if the certificate was found
                //            if (certificates.Count > 0)
                //            {

                //                // Remove the certificate from the store
                //                store.RemoveRange(certificates);

                //            }
                //            else
                //            {
                //                return false;
                //            }
                //        }


                //    }
                //    else
                //    {
                //        ProcessStartInfo psi = new ProcessStartInfo
                //        {
                //            FileName = exeFile1, // Replace with your console application's executable
                //            Verb = "runas", // Run as administrator if needed
                //            UseShellExecute = true, // Use the shell execution
                //            CreateNoWindow = true,
                //            Arguments = arguments, // Pass concatenated string as command-line argument
                //            WindowStyle = ProcessWindowStyle.Hidden,// Set the window style to hidden        
                //        };
                //        Process.Start(psi);
                //    }


                //    // Rest of your code...
                //}
                //catch (Exception ex)
                //{
                //    Console.WriteLine("Error: " + ex.Message);
                //}


                //DeleteCertificateFromTrustedRoot(certificateThumbprint);

                ////// Open the certificate store
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

                        // Remove the certificate from the store
                        store.RemoveRange(certificates);

                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch
            {
                //MessageBox.Show("Require Admin Access");
                return false;
            }

            return true;
        }


        static bool DeleteCertificateFromTrustedRoot(string certificateThumbprint)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "certutil",
                    Arguments = $"-delstore -user -split -enterprise -f \"Root\" \"{certificateThumbprint}\"",
                    Verb = "runas", // Request administrator privileges
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    process.WaitForExit();
                    return process.ExitCode == 0; // Exit code 0 indicates success
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return false;
            }
        }



        static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

    }
}
