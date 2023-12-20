using FDS_Administrator.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace FDS_Administrator
{
    internal class Program
    {
        public static string pipName = @"\\.\pipe\AdminPipes";
        static void Main(string[] args)
        {

            try
            {
                using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", pipName, PipeDirection.Out))
                {
                    pipeClient.Connect(); // Connect to the service
                                          // Send a message/command to the service
                    byte[] buffer = Encoding.UTF8.GetBytes("WindowsRegistryProtection");
                    pipeClient.Write(buffer, 0, buffer.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occurred while connecting to the named pipe: " + ex.Message);
                // Handle or log exception
            }

            //WriteLog("Starting 1");
            // Check for command-line arguments
            //if (args.Length > 0)
            //{
            //    string[] parameters = args[0].Split(',');
            //    string methodName = parameters[0];

            //    // Determine which method to call based on the argument
            //    switch (methodName)
            //    {
            //        case "Certificates":
            //            Method1(args); // Call Method1 and pass additional arguments
            //            break;
            //        case "WindowsRegistryProtection":
            //            Method2(args); // Call Method2 and pass additional arguments
            //            break;
            //        // Add more cases for other methods if needed
            //        default:
            //            Console.WriteLine("Invalid method name");
            //            break;
            //    }
            //}
            //else
            //{
            //    Console.WriteLine("No method specified");
            //}
        }

        static void Method1(string[] args)
        {            
            DeleteCertificates deleteCertificates = new DeleteCertificates();
            deleteCertificates.Delete(args);            
        }
 
      
        static void Method2(string[] args)
        {
            WindowsRegistryProtection windowsRegistryProtection = new WindowsRegistryProtection();
            windowsRegistryProtection.DeleteRegistriesKey();
        }
    }
}
