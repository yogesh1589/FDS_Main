﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace FDS_Administrator.Services
{
    public class DeleteCertificates
    {
        public void Delete(string[] args)
        {
            //string abc = "Certificates,f769bb0beebd2f03f3ba26157251a657e39a01a5," + StoreLocation.CurrentUser + "," + StoreName.My;
            //string[] parameters = abc.Split(',');
            //string[] parameters = args[0].Split(',');

            string[] parameters = args[0].Split(',');
            string certificateThumbprint = parameters[1];             
            string storeLocationString = parameters[2];             
            string storeNameString = parameters[3];

            Console.WriteLine(certificateThumbprint);
            Console.WriteLine(storeLocationString);
            Console.WriteLine(storeNameString);

            Enum.TryParse(storeNameString, out StoreName storeName);
            Enum.TryParse(storeLocationString, out StoreLocation storeLocation);

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
                {                    // Remove the certificate from the store
                    store.RemoveRange(certificates);
                    Console.WriteLine("Removed");
                }
            }
            // Implement Method1 logic here
            Console.WriteLine("Certificate Deleted");
            Console.ReadLine();
        }
    }
}