using FDS.Models;
using OpenQA.Selenium;
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

                            if (typeOfproxy == "System Proxy")
                            {
                            }
                            else if (typeOfproxy == "Manual Proxy")
                            {
                                List<ProxyLists> proxyDataHTTP = GetMozilaHTTP(prefsFileContent);

                                List<ProxyLists> proxyDataHTTPS = GetMozilaHTTPS(prefsFileContent);

                                List<ProxyLists> proxyDataHTTPSocks = GetMozilaSocks(prefsFileContent);



                                listProxy.AddRange(proxyDataHTTP);
                                listProxy.AddRange(proxyDataHTTPS);
                                listProxy.AddRange(proxyDataHTTPSocks);

                            }
                            else if (typeOfproxy == "Proxy Auto-Configuration")
                            {
                                List<ProxyLists> proxyDataAutoConfigURL = GetMozilaAutoConfigURL(prefsFileContent);
                                listProxy.AddRange(proxyDataAutoConfigURL);
                            }
                            Console.WriteLine("Proxy is set in Mozilla Firefox.");
                        }
                        else
                        {
                            Console.WriteLine("Proxy is not set in Mozilla Firefox.");
                        }
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

            proxyData.proxy_on = "Mozilla";
            proxyData.proxy_on_plugin = string.Empty;

            proxyData.proxy_type = "http";

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
                proxyData.proxy_ip = GetIPFromHostsFile(proxyData.proxy_address);

                if (!string.IsNullOrWhiteSpace(proxyData.proxy_ip))
                {
                    Console.WriteLine("IP address from hosts file: " + proxyData.proxy_ip);
                }
                else
                {

                    proxyData.proxy_ip = GetIPFromHostsFile(proxyData.proxy_address);

                    if (!string.IsNullOrWhiteSpace(proxyData.proxy_ip))
                    {
                        Console.WriteLine("IP address from hosts file: " + proxyData.proxy_ip);
                    }
                    else
                    {
                        IPAddress[] addresses = Dns.GetHostAddresses(proxyData.proxy_address);

                        StringBuilder stringBuilder = new StringBuilder();
                        foreach (IPAddress address in addresses)
                        {
                            if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                stringBuilder.Append("," + address.ToString());
                                proxyData.proxy_ip = stringBuilder.ToString().TrimStart(',');
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to resolve {proxyData.proxy_address}: {ex.Message}");
            }
            if (proxyData.proxy_ip == null)
            {
                proxyData.proxy_ip = proxyData.proxy_address;
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
                    proxy_type = proxyData.proxy_type,
                    autoconfig_url = ""
                });
            }

            return proxyDatas;
        }


        public string GetIPFromHostsFile(string hostname)
        {
            try
            {
                // Read the hosts file
                string hostsFilePath = Environment.SystemDirectory + @"\drivers\etc\hosts";
                string[] lines = File.ReadAllLines(hostsFilePath);

                // Search for the hostname in the hosts file
                string ipAddress = lines
                    .Select(line => line.Trim())
                    .Where(line => !line.StartsWith("#") && !string.IsNullOrWhiteSpace(line))
                    .Select(line =>
                    {
                        string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                        {
                            return new { IP = parts[0], Hostnames = parts.Skip(1).ToList() };
                        }
                        return null;
                    })
                    .Where(entry => entry != null && entry.Hostnames.Contains(hostname))
                    .Select(entry => entry.IP)
                    .FirstOrDefault();

                return ipAddress;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public List<ProxyLists> GetMozilaHTTPS(string prefsFileContent)
        {
            List<ProxyLists> proxyDatas = new List<ProxyLists>();
            ProxyLists proxyData = new ProxyLists();
            // HTTP
            string proxyHTTPSPattern = @"user_pref\(""network.proxy.ssl"", ""([^""]+)""\);";
            string proxyHTTPSPortPattern = @"user_pref\(""network.proxy.ssl_port"", (\d+)\);";



            proxyData.proxy_on = "Mozilla";
            proxyData.proxy_on_plugin = string.Empty;

            proxyData.proxy_type = "https";

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

                proxyData.proxy_ip = GetIPFromHostsFile(proxyData.proxy_address);

                if (!string.IsNullOrWhiteSpace(proxyData.proxy_ip))
                {
                    Console.WriteLine("IP address from hosts file: " + proxyData.proxy_ip);
                }
                else
                {
                    IPAddress[] addresses = Dns.GetHostAddresses(proxyData.proxy_address);

                    StringBuilder stringBuilder = new StringBuilder();
                    foreach (IPAddress address in addresses)
                    {
                        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            stringBuilder.Append("," + address.ToString());
                            proxyData.proxy_ip = stringBuilder.ToString().TrimStart(',');
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to resolve {proxyData.proxy_address}: {ex.Message}");
            }
            if (proxyData.proxy_ip == null)
            {
                proxyData.proxy_ip = proxyData.proxy_address;
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
                    proxy_type = proxyData.proxy_type,
                    autoconfig_url = ""
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


            proxyData.proxy_on = "Mozilla";
            proxyData.proxy_on_plugin = string.Empty;

            proxyData.proxy_type = "socks";

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

                proxyData.proxy_ip = GetIPFromHostsFile(proxyData.proxy_address);

                if (!string.IsNullOrWhiteSpace(proxyData.proxy_ip))
                {
                    Console.WriteLine("IP address from hosts file: " + proxyData.proxy_ip);
                }
                else
                {
                    IPAddress[] addresses = Dns.GetHostAddresses(proxyData.proxy_address);

                    StringBuilder stringBuilder = new StringBuilder();
                    foreach (IPAddress address in addresses)
                    {
                        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            stringBuilder.Append("," + address.ToString());
                            proxyData.proxy_ip = stringBuilder.ToString().TrimStart(',');
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to resolve {proxyData.proxy_address}: {ex.Message}");
            }
            if (proxyData.proxy_ip == null)
            {
                proxyData.proxy_ip = proxyData.proxy_address;
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
                    proxy_type = proxyData.proxy_type,
                    autoconfig_url = ""
                });
            }

            return proxyDatas;
        }

        public List<ProxyLists> GetMozilaAutoConfigURL(string prefsFileContent)
        {
            List<ProxyLists> proxyDatas = new List<ProxyLists>();
            ProxyLists proxyData = new ProxyLists();

            string proxySOCKSPattern = @"user_pref\(""network.proxy.autoconfig_url"", ""([^""]+)""\);";
            string proxySOCKSPortPattern = @"user_pref\(""network.proxy.autoconfig_url"", (\d+)\);";


            proxyData.proxy_on = "Mozilla";
            proxyData.proxy_on_plugin = string.Empty;

            proxyData.proxy_type = "autoconfig_url";

            Match proxySOCKSMatch = Regex.Match(prefsFileContent, proxySOCKSPattern);
            if (proxySOCKSMatch.Success)
            {
                proxyData.autoconfig_url = proxySOCKSMatch.Groups[1].Value;
                if ((proxyData.autoconfig_url.Split(':').Length > 2) && (proxyData.autoconfig_url.Contains("//")))
                {
                    proxyData.proxy_type = proxyData.autoconfig_url.Split(':')[1].Trim();
                    proxyData.proxy_address = proxyData.autoconfig_url.Split(':')[2].Replace("//", "").Trim();
                    proxyData.proxy_port = proxyData.autoconfig_url.Split(':')[3].Trim();

                }
                else
                {
                    proxyData.proxy_address = proxyData.autoconfig_url.Split(':')[1].Trim().Replace("//", "").Replace("/", "");
                    string pattern = @":(\d+)/";

                    // Use Regex.Match to find the port number in the URL
                    Match match = Regex.Match(proxyData.autoconfig_url, pattern);

                    if (match.Success)
                    {
                        proxyData.proxy_port = match.Groups[1].Value;
                    }

                    //proxyData.proxy_port = proxyData.autoconfig_url.Split(':').Length > 1 ? proxyData.autoconfig_url.Split(':')[2].Trim() : string.Empty;
                }
            }

            // Match SOCKS proxy port
            Match proxySOCKSPortMatch = Regex.Match(prefsFileContent, proxySOCKSPortPattern);
            if (proxySOCKSPortMatch.Success)
            {
                proxyData.proxy_port = proxySOCKSPortMatch.Groups[1].Value;
            }

            try
            {
                proxyData.proxy_ip = GetIPFromHostsFile(proxyData.proxy_address);

                if (!string.IsNullOrWhiteSpace(proxyData.proxy_ip))
                {
                    Console.WriteLine("IP address from hosts file: " + proxyData.proxy_ip);
                }
                else
                {
                    IPAddress[] addresses = Dns.GetHostAddresses(proxyData.proxy_address);

                    foreach (IPAddress address in addresses)
                    {
                        proxyData.proxy_ip = address.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to resolve {proxyData.proxy_address}: {ex.Message}");
            }
            if (proxyData.proxy_ip == null)
            {
                proxyData.proxy_ip = proxyData.proxy_address;
            }

            string jsonMergedList = string.Empty;

            if ((!string.IsNullOrEmpty(proxyData.proxy_address)) || (!string.IsNullOrEmpty(proxyData.autoconfig_url)))
            {
                proxyDatas.Add(new ProxyLists
                {
                    proxy_address = proxyData.proxy_address ?? string.Empty,
                    proxy_ip = proxyData.proxy_ip ?? string.Empty,
                    proxy_on = proxyData.proxy_on ?? string.Empty,
                    proxy_on_plugin = "",
                    proxy_port = proxyData.proxy_port ?? string.Empty,
                    proxy_type = proxyData.proxy_type ?? string.Empty,
                    autoconfig_url = proxyData.autoconfig_url ?? string.Empty
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
