using FDS.Common;
using FDS.DTO.Requests;
using FDS.DTO.Responses;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
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

        public async Task<bool> DeleteCertificates()
        {

            try
            {
                RemoveWebProxy();
                RemoveSystemProxy();

            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }


        public bool RemoveWebProxy()
        {
            try
            {
                string firefoxProfilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mozilla", "Firefox", "Profiles");

                if (Directory.Exists(firefoxProfilePath))
                {
                    string[] profileDirectories = Directory.GetDirectories(firefoxProfilePath);

                    foreach (string profileDir in profileDirectories)
                    {
                        string prefsFilePath = Path.Combine(profileDir, "prefs.js");

                        if (File.Exists(prefsFilePath))
                        {
                            // Read the content of prefs.js
                            string prefsFileContent = File.ReadAllText(prefsFilePath);

                            // Check if HTTP proxy configuration exists in the prefs.js file
                            bool hasHttpProxyConfig = CheckForHttpProxyConfig(prefsFileContent);

                            if (hasHttpProxyConfig)
                            {
                                // Remove the HTTP proxy configuration
                                string updatedPrefsFileContent = RemoveHttpProxyConfig(prefsFileContent);

                                // Save the updated prefs.js content
                                File.WriteAllText(prefsFilePath, updatedPrefsFileContent);

                                Console.WriteLine("HTTP proxy setting removed for profile: " + profileDir);
                            }
                        }
                    }
                }

            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public bool CheckForHttpProxyConfig(string prefsContent)
        {
            // Use a regular expression to check if an HTTP proxy configuration exists
            // Modify this pattern according to the specific proxy settings format in prefs.js
            string httpProxyPattern = @"user_pref\(""network.proxy.http"", .+\);";
            return Regex.IsMatch(prefsContent, httpProxyPattern);
        }

        public string RemoveHttpProxyConfig(string prefsContent)
        {
            // Use a regular expression to remove the HTTP proxy configuration
            // Modify this pattern according to the specific proxy settings format in prefs.js
            string httpProxyPattern = @"user_pref\(""network.proxy.http"", .+\);";
            return Regex.Replace(prefsContent, httpProxyPattern, "");
        }

        public bool RemoveSystemProxy()
        {
            // Specify the path to the Firefox proxy settings in the Windows Registry
            const string registryPath = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(registryPath, true))
                {
                    // Remove the proxy server settings
                    key.DeleteValue("ProxyServer", false);
                    key.DeleteValue("ProxyEnable", false);                  
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
