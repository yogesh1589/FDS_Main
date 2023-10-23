using FDS.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FDS.Common
{
    public class MozilaProxies
    {
        public List<ProxyLists> CheckMozilaProxy()
        {
            List<ProxyLists> listProxy = new List<ProxyLists>();

            string firefoxProfilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mozilla", "Firefox", "Profiles");

            // Get the path to the default Firefox profile
            //string profilePath = GetDefaultProfilePath(firefoxProfilePath);

            if (Directory.Exists(firefoxProfilePath))
            {
                string[] profileDirectories = Directory.GetDirectories(firefoxProfilePath);

                foreach (string profileDir in profileDirectories)
                {
                    string prefsFilePath = Path.Combine(profileDir, "prefs.js");

                    if (File.Exists(prefsFilePath))
                    {
                        string prefsFileContent = File.ReadAllText(prefsFilePath);

                        // Check if a proxy configuration exists in the prefs.js file
                        bool hasProxyConfig = CheckForProxyConfig(prefsFileContent);

                        if (hasProxyConfig)
                        {
                            // Match proxy type
                            string typeOfproxy = string.Empty;
                            string proxyTypePattern = @"user_pref\(""network.proxy.type"", (\d+)\);";
                            Match proxyTypeMatch = Regex.Match(prefsFileContent, proxyTypePattern);
                            if (proxyTypeMatch.Success)
                            {
                                int proxyTypeValue = int.Parse(proxyTypeMatch.Groups[1].Value);

                                switch (proxyTypeValue)
                                {
                                    case 0:
                                        typeOfproxy = "No Proxy";
                                        break;
                                    case 1:
                                        typeOfproxy = "Manual Proxy";
                                        break;
                                    case 2:
                                        typeOfproxy = "Proxy Auto-Configuration";
                                        break;
                                    case 4:
                                        typeOfproxy = "Auto-Detect Proxy";
                                        break;
                                    case 5:
                                        typeOfproxy = "System Proxy";
                                        break;
                                    default:
                                        // Handle invalid or unexpected values
                                        Console.WriteLine($"Invalid proxy type value: {proxyTypeValue}");
                                        break;
                                }

                            }
                            else { typeOfproxy = "System Proxy"; }

                            if (typeOfproxy == "Manual Proxy")
                            {
                                List<ProxyLists> proxyDataHTTP = GetMozilaHTTP(prefsFileContent);

                                List<ProxyLists> proxyDataHTTPS = GetMozilaHTTPS(prefsFileContent);

                                List<ProxyLists> proxyDataHTTPSocks = GetMozilaSocks(prefsFileContent);

                                listProxy.AddRange(proxyDataHTTP);
                                listProxy.AddRange(proxyDataHTTPS);
                                listProxy.AddRange(proxyDataHTTPSocks);
                            }
                            Console.WriteLine("Proxy is set in Mozilla Firefox.");
                        }
                        else
                        {
                            Console.WriteLine("Proxy is not set in Mozilla Firefox.");
                        }
                        return listProxy;

                    }
                    else
                    {
                        Console.WriteLine("The Firefox prefs.js file does not exist.");
                    }
                }
            }             
            return listProxy;
        }

        public List<ProxyLists> GetMozilaHTTP(string prefsFileContent)
        {
            List<ProxyLists> proxyDatas = new List<ProxyLists>();
            ProxyLists proxyData = new ProxyLists();
            // HTTP
            string proxyIPPatern = @"user_pref\(""network.proxy.http"", ""([^""]+)""\);";
            string proxyPortPattern = @"user_pref\(""network.proxy.http_port"", (\d+)\);";

            proxyData.proxy_on = "Mozila";
            proxyData.proxy_on_plugin = string.Empty;
             
            proxyData.proxy_type = "HTTP";
             
            // Match proxy IP
            Match proxyIPMatch = Regex.Match(prefsFileContent, proxyIPPatern);
            if (proxyIPMatch.Success)
            {
                proxyData.proxy_address = proxyIPMatch.Groups[1].Value;
            }

            // Match proxy port
            Match proxyPortMatch = Regex.Match(prefsFileContent, proxyPortPattern);
            if (proxyPortMatch.Success)
            {
                proxyData.proxy_port = proxyPortMatch.Groups[1].Value;
            }

            try
            {
                IPAddress[] addresses = Dns.GetHostAddresses(proxyData.proxy_address);

                foreach (IPAddress address in addresses)
                {
                    proxyData.proxy_ip = address.ToString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to resolve {proxyData.proxy_address}: {ex.Message}");
            }

        
            if (!string.IsNullOrEmpty(proxyData.proxy_address))
            {

                proxyDatas.Add(new ProxyLists
                {
                    proxy_address = proxyData.proxy_address,
                    proxy_ip = proxyData.proxy_ip,
                    proxy_on = proxyData.proxy_on,
                    proxy_on_plugin = "",
                    proxy_port = proxyData.proxy_port,
                    proxy_type = proxyData.proxy_type
                });
            }

            return proxyDatas;
        }


        public List<ProxyLists> GetMozilaHTTPS(string prefsFileContent)
        {
            List<ProxyLists> proxyDatas = new List<ProxyLists>();
            ProxyLists proxyData = new ProxyLists();
            // HTTP
            string proxyHTTPSPattern = @"user_pref\(""network.proxy.ssl"", ""([^""]+)""\);";
            string proxyHTTPSPortPattern = @"user_pref\(""network.proxy.ssl_port"", (\d+)\);";



            proxyData.proxy_on = "Mozila";
            proxyData.proxy_on_plugin = string.Empty;

            proxyData.proxy_type = "HTTPS";

            // Match HTTPS proxy address
            Match proxyHTTPSMatch = Regex.Match(prefsFileContent, proxyHTTPSPattern);
            if (proxyHTTPSMatch.Success)
            {
                proxyData.proxy_address = proxyHTTPSMatch.Groups[1].Value;
            }

            // Match HTTPS proxy port
            Match proxyHTTPSPortMatch = Regex.Match(prefsFileContent, proxyHTTPSPortPattern);
            if (proxyHTTPSPortMatch.Success)
            {
                proxyData.proxy_port = proxyHTTPSPortMatch.Groups[1].Value;
            }

            try
            {
                IPAddress[] addresses = Dns.GetHostAddresses(proxyData.proxy_address);

                foreach (IPAddress address in addresses)
                {
                    proxyData.proxy_ip = address.ToString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to resolve {proxyData.proxy_address}: {ex.Message}");
            }

            string jsonMergedList = string.Empty;

            if (!string.IsNullOrEmpty(proxyData.proxy_address))
            {

                proxyDatas.Add(new ProxyLists
                {
                    proxy_address = proxyData.proxy_address,
                    proxy_ip = proxyData.proxy_ip,
                    proxy_on = proxyData.proxy_on,
                    proxy_on_plugin = "",
                    proxy_port = proxyData.proxy_port,
                    proxy_type = proxyData.proxy_type
                });
            }

            return proxyDatas;
        }

        public List<ProxyLists> GetMozilaSocks(string prefsFileContent)
        {
            List<ProxyLists> proxyDatas = new List<ProxyLists>();
            ProxyLists proxyData = new ProxyLists();

            string proxySOCKSPattern = @"user_pref\(""network.proxy.socks"", ""([^""]+)""\);";
            string proxySOCKSPortPattern = @"user_pref\(""network.proxy.socks_port"", (\d+)\);";


            proxyData.proxy_on = "Mozila";
            proxyData.proxy_on_plugin = string.Empty;

            proxyData.proxy_type = "SOCKS";

            Match proxySOCKSMatch = Regex.Match(prefsFileContent, proxySOCKSPattern);
            if (proxySOCKSMatch.Success)
            {
                proxyData.proxy_address = proxySOCKSMatch.Groups[1].Value;
            }

            // Match SOCKS proxy port
            Match proxySOCKSPortMatch = Regex.Match(prefsFileContent, proxySOCKSPortPattern);
            if (proxySOCKSPortMatch.Success)
            {
                proxyData.proxy_port = proxySOCKSPortMatch.Groups[1].Value;
            }

            try
            {
                IPAddress[] addresses = Dns.GetHostAddresses(proxyData.proxy_address);

                foreach (IPAddress address in addresses)
                {
                    proxyData.proxy_ip = address.ToString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to resolve {proxyData.proxy_address}: {ex.Message}");
            }

            string jsonMergedList = string.Empty;

            if (!string.IsNullOrEmpty(proxyData.proxy_address))
            {

                proxyDatas.Add(new ProxyLists
                {
                    proxy_address = proxyData.proxy_address,
                    proxy_ip = proxyData.proxy_ip,
                    proxy_on = proxyData.proxy_on,
                    proxy_on_plugin = "",
                    proxy_port = proxyData.proxy_port,
                    proxy_type = proxyData.proxy_type
                });
            }

            return proxyDatas;
        }

        public bool CheckForProxyConfig(string prefsFileContent)
        {
            // Check if the 'network.proxy.http' or 'network.proxy.ssl' preferences are set
            return Regex.IsMatch(prefsFileContent, @"user_pref\(""network.proxy.http"", ""(.*)""\);") ||
                   Regex.IsMatch(prefsFileContent, @"user_pref\(""network.proxy.ssl"", ""(.*)""\);");
        }
    }
}
