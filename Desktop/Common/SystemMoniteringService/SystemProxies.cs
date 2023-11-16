using FDS.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FDS.Common.SystemMoniteringService
{
    public class SystemProxies
    {

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
        public List<ProxyLists> CheckSystemProxy()
        {
            List<ProxyLists> proxyDatas = new List<ProxyLists>();
            ProxyLists proxyData = new ProxyLists();
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "powershell.exe";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardInput = true; // Enable input redirection
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;

                process.Start();

                // Define your PowerShell command or script
                string powershellCommand = "Get-ItemProperty 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings'";

                // Write the PowerShell command to the standard input
                process.StandardInput.WriteLine(powershellCommand);
                process.StandardInput.Flush();
                process.StandardInput.Close();

                // Read and process the output
                string output = process.StandardOutput.ReadToEnd();
                //Console.WriteLine(output);


                string proxyOverRide = "";

                bool proxyEnabled = false;
                ProxyLists proxy = new ProxyLists();

                foreach (string line in output.Split('\n'))
                {
                    if (line.Contains("ProxyServer"))
                    {
                        if ((line.Split(':').Length > 2) && (line.Contains("//")))
                        {
                            proxy.proxy_type = line.Split(':')[1].Trim();
                            proxy.proxy_address = line.Split(':')[2].Replace("//", "").Trim();
                            proxy.proxy_port = line.Split(':')[3].Trim();

                        }
                        else
                        {
                            proxy.proxy_address = line.Split(':')[1].Trim();
                            //string pattern = @":(\d+)/";

                            //// Use Regex.Match to find the port number in the URL
                            //Match match = Regex.Match(line, pattern);

                            //if (match.Success)
                            //{
                            //    proxyData.proxy_port = match.Groups[1].Value;
                            //}
                            proxy.proxy_port = line.Split(':')[2].Trim();
                        }
                    }
                    else if (line.Contains("ProxyEnable"))
                    {
                        proxyEnabled = int.Parse(line.Split(':')[1].Trim()) == 1;
                    }
                    else if (line.Contains("ProxyOverride"))
                    {
                        proxyOverRide = line.Split(':')[1].Trim();
                    }
                }


                proxy.proxy_on = "System";
                proxy.proxy_on_plugin = string.Empty;

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
                    Console.WriteLine($"Failed to resolve {proxy.proxy_address}: {ex.Message}");
                }
                if (proxy.proxy_ip == null)
                {
                    proxy.proxy_ip = proxyData.proxy_address;
                }
                 

                if (proxyEnabled)
                {

                    proxyDatas.Add(new ProxyLists
                    {
                        proxy_address = proxy.proxy_address ?? string.Empty,
                        proxy_ip = proxy.proxy_ip ?? string.Empty,
                        proxy_on = proxy.proxy_on ?? string.Empty,
                        proxy_on_plugin = "",
                        proxy_port = proxy.proxy_port ?? string.Empty,
                        proxy_type = proxy.proxy_type ?? string.Empty
                    });
                }

                process.WaitForExit();
                return proxyDatas;
            }
        }

    }
}