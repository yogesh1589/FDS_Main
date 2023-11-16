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
using System.Windows.Forms;
using Windows.UI.Xaml;

namespace FDS.API_Service
{
    public class ProxyService
    {
        //private readonly HttpClient client1;

        public ProxyService()
        {
            //client1 = new HttpClient();
            //client1 = new HttpClient { BaseAddress = AppConstants.EndPoints.BaseAPI };
        }

        public async Task<bool> ProxyDataAsync(string payload)
        {
            try
            {

                var handler = new HttpClientHandler
                {
                    UseProxy = false // Disable using the system proxy
                };

                using (var client1 = new HttpClient(handler))
                {
                    client1.BaseAddress = new Uri(AppConstants.EndPoints.BaseAPI.ToString());
                    client1.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var content = new StringContent(payload, Encoding.UTF8, "application/json");
                    var jsonResponse = await client1.PostAsync(AppConstants.EndPoints.PostProxy, content);


                    if (jsonResponse.IsSuccessStatusCode)
                    {
                        var responseString = await jsonResponse.Content.ReadAsStringAsync();

                        //CertificatePayloadResponse apiResponse = JsonConvert.DeserializeObject<CertificatePayloadResponse>(responseString);
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
                // Handle exceptions and log errors if needed.
                return false;
            }
        }



        public async Task<bool> DeleteProxies()
        {
            try
            {
                var handler = new HttpClientHandler
                {
                    UseProxy = false // Disable using the system proxy
                };

                using (var client1 = new HttpClient(handler))
                {
                    client1.BaseAddress = new Uri(AppConstants.EndPoints.BaseAPI.ToString());
                    var getresponse = await client1.GetAsync(AppConstants.EndPoints.ProxyLog + "?device_uuid=" + AppConstants.UUId);
                    if (getresponse.IsSuccessStatusCode)
                    {
                        var responseString = await getresponse.Content.ReadAsStringAsync();
                        ProxyPayloadResponse response = JsonConvert.DeserializeObject<ProxyPayloadResponse>(responseString);
                        ProxyResponse[] proxy_info = response.ProxyResponse;

                        List<ProxyDeletionRequest> deleted_proxy = new List<ProxyDeletionRequest>();
                        List<ProxyDeletionRequest> no_deleted_proxy = new List<ProxyDeletionRequest>();
                        bool resultSet = false;

                        if (proxy_info != null)
                        {
                            foreach (ProxyResponse proxy in proxy_info)
                            {
                                resultSet = await DeleteProxy(proxy.ProxyOn, proxy.ProxyType);
                                var deletionRequest = new ProxyDeletionRequest
                                {
                                    proxy_on = proxy.ProxyOn,
                                    proxy_on_plugin = proxy.ProxyOnPlugin,
                                    proxy_type = proxy.ProxyType,
                                    proxy_address = proxy.ProxyAddress,
                                    proxy_port = proxy.ProxyPort,
                                    proxy_ip = proxy.ProxyIp
                                };

                                if (resultSet)
                                {
                                    deleted_proxy.Add(deletionRequest);
                                }
                                else
                                {
                                    no_deleted_proxy.Add(deletionRequest);
                                }
                            }
                            var deletedproxy = new DeletedProxyRequest
                            {
                                device_uuid = AppConstants.UUId,
                                deleted_proxy = deleted_proxy,
                                no_deleted_proxy = no_deleted_proxy
                            };

                            string json = JsonConvert.SerializeObject(deletedproxy);

                            client1.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                            var content = new StringContent(json, Encoding.UTF8, "application/json");
                            var jsonResponse = await client1.PostAsync(AppConstants.EndPoints.ProxyLog, content);


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
                }
            }
            catch (Exception ex)
            {

            }
            return true;
        }


        private async Task<bool> DeleteProxy(string proxyOn, string proxyType)
        {

            try
            {
                if (proxyOn == "Mozilla")
                {
                    return RemoveWebProxy(proxyType);
                }
                else
                {
                    return RemoveSystemProxy();
                }

            }
            catch (Exception ex)
            {
                return false;
            }
        }


        public bool RemoveWebProxy(string proxyType)
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
                            bool hasHttpProxyConfig = false;
                            string updatedPrefsFileContent = string.Empty;


                            if (proxyType == "http")
                            {
                                hasHttpProxyConfig = CheckForProxyConfig(prefsFileContent, "network.proxy.http", "network.proxy.http_port");
                                if (hasHttpProxyConfig)
                                {
                                    updatedPrefsFileContent = RemoveProxyConfig(prefsFileContent, "network.proxy.http", "network.proxy.http_port");
                                }
                            }
                            else if (proxyType == "https")
                            {
                                hasHttpProxyConfig = CheckForProxyConfig(prefsFileContent, "network.proxy.https", "network.proxy.https_port");
                                if (hasHttpProxyConfig)
                                {
                                    updatedPrefsFileContent = RemoveProxyConfig(prefsFileContent, "network.proxy.https", "network.proxy.https_port");
                                }
                            }
                            else if (proxyType == "socks")
                            {
                                hasHttpProxyConfig = CheckForProxyConfig(prefsFileContent, "network.proxy.socks", "network.proxy.socks_port");
                                if (hasHttpProxyConfig)
                                {
                                    updatedPrefsFileContent = RemoveProxyConfig(prefsFileContent, "network.proxy.socks", "network.proxy.socks_port");
                                }
                            }
                            else if (proxyType == "autoconfig_url")
                            {
                                hasHttpProxyConfig = CheckForProxyConfig(prefsFileContent, "network.proxy.autoconfig_url", "");
                                if (hasHttpProxyConfig)
                                {
                                    updatedPrefsFileContent = RemoveProxyConfig(prefsFileContent, "network.proxy.autoconfig_url", "");
                                }
                            }
                            File.WriteAllText(prefsFilePath, updatedPrefsFileContent);
                            Console.WriteLine("HTTP proxy setting removed for profile: " + profileDir);
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

        public bool CheckForProxyConfig(string prefsContent, string proxyType, string proxyPort)
        {
            // Use a regular expression to check if an HTTP proxy configuration exists
            // Modify this pattern according to the specific proxy settings format in prefs.js
            string httpProxyPattern = $@"user_pref\(""{proxyType}"", .+\);";
            return Regex.IsMatch(prefsContent, httpProxyPattern);
        }

        public string RemoveProxyConfig(string prefsContent, string proxyType, string proxyPort)
        {
            // Use a regular expression to remove the HTTP proxy configuration
            // Modify this pattern according to the specific proxy settings format in prefs.js
            string httpProxyPattern = $@"user_pref\(""{proxyType}"", .+\);";
            string httpProxyPort = $@"user_pref\(""{proxyPort}"", .+\);";
            prefsContent = Regex.Replace(prefsContent, httpProxyPort, "");
            return Regex.Replace(prefsContent, httpProxyPattern, "");
        }

        public bool RemoveSystemProxy()
        {
             
            const string registryPath = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";
            try
            {
                 
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(registryPath, true))
                {

                    // Remove the proxy server settings
                    key.SetValue("ProxyEnable", 0);
                    key.DeleteValue("ProxyServer");
                    //key.DeleteValue("ProxyEnable", false);
                     
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
