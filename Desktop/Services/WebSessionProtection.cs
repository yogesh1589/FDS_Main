using FDS.Common;
using FDS.DTO.Responses;
using FDS.Logging;
using FDS.Services.AbstractClass;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Protection.PlayReady;

namespace FDS.Services
{
    public class WebSessionProtection : BaseService
    {
        List<string> whitelistedDomain = new List<string>();
        public HttpClient client { get; }
        public WebSessionProtection(ILogger logger) : base(logger)
        {
        }

        public override void RunService(SubservicesData subservices)
        {

            try
            {
                string SubServiceId = Convert.ToString(subservices.Id);

                CheckWhiteListDomains(SubServiceId, subservices.Sub_service_authorization_code, subservices.Sub_service_name, subservices.Execute_now);

                int ChromeCount = ClearChromeCookie();
                int FireFoxCount = ClearFirefoxCookies();
                int EdgeCount = ClearEdgeCookies();
                int OperaCount = ClearOperaCookies();

                int TotalCount = ChromeCount + FireFoxCount + EdgeCount + OperaCount;

                LogServicesData(subservices.Sub_service_authorization_code, subservices.Sub_service_name, TotalCount, Convert.ToString(subservices.Id), subservices.Execute_now);

            }
            catch (Exception exp)
            {
                exp.ToString();
            }
        }

        private async void CheckWhiteListDomains(string SubServiceId, string Sub_service_authorization_code, string Sub_service_name, bool ExecuteNow)
        {
            try
            {
                whitelistedDomain.Clear();
                var response = await client.GetAsync(AppConstants.EndPoints.WhiteListDomains + SubServiceId + "/");
                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var responseData = JsonConvert.DeserializeObject<WhiteListDomainResponse>(responseString);
                    if (responseData.device_domains.Count > 0)
                    {
                        foreach (var domain in responseData.device_domains)
                        {
                            whitelistedDomain.Add("'%" + domain.domain_name + "%'");
                        }

                    }
                    if (responseData.org_domains.Count > 0)
                    {
                        foreach (var domain in responseData.org_domains)
                        {
                            whitelistedDomain.Add("'%" + domain.domain_name + "%'");
                        }
                    }

                    //  MessageBox.Show("Total " + whitelistedDomain.Count.ToString() + " whitelistedDomain");



                }
                 
            }
            catch
            {
                //MessageBox.Show("An error occurred while fatching whitelist domains: ", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private int ClearChromeCookie()
        {

            int TotalCount = 0;
            int bCount = IsBrowserOpen("chrome");
            //Process[] chromeInstances = Process.GetProcessesByName("chrome");            
            if (bCount == 0)
            {
                string chromeProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Google\Chrome\User Data\";

                List<string> profiles = new List<string>();
                string defaultProfilePath = Path.Combine(chromeProfilePath, "Default");
                if (Directory.Exists(defaultProfilePath))
                {
                    profiles.Add(defaultProfilePath);
                }
                if (Directory.Exists(chromeProfilePath))
                {
                    string[] profileDirectories = Directory.GetDirectories(chromeProfilePath, "Profile *");

                    foreach (string profileDir in profileDirectories)
                    {
                        string profilePath = Path.Combine(chromeProfilePath, profileDir);
                        profiles.Add(profilePath);
                    }
                }

                foreach (var profile in profiles)
                {
                    if (Directory.Exists(profile))
                    {
                        string CookiesPath = Path.Combine(profile, "Network\\Cookies");
                        if (File.Exists(CookiesPath))
                        {
                            using (SQLiteConnection connection = new SQLiteConnection("Data Source=" + CookiesPath))
                            {
                                ///MessageBox.Show("Chrome path : " + chromeProfilePath.ToString() + " || Cookies Path " + CookiesPath.ToString());
                                connection.Open();
                                using (SQLiteCommand command = connection.CreateCommand())
                                {
                                    string query = "DELETE FROM Cookies";
                                    if (whitelistedDomain.Count > 0)
                                    {
                                        query += " WHERE ";
                                        foreach (string domain in whitelistedDomain)
                                        {
                                            query += " host_key not like " + domain + " And";
                                        }
                                        query = query.Remove(query.Length - 4);
                                    }
                                    command.CommandText = query;
                                    command.Prepare();
                                    TotalCount += command.ExecuteNonQuery();
                                }
                                connection.Close();
                                //MessageBox.Show("Cookies done from Chrome");
                            }
                        }
                    }
                }

                // Display the count of items deleted
                Console.WriteLine("Total number of cookies deleted: " + TotalCount);
            }
            return TotalCount;
        }
        public int ClearFirefoxCookies()
        {
            int TotalCount = 0;
            int bCount = IsBrowserOpen("firefox");
            //Process[] firefoxInstances = Process.GetProcessesByName("firefox");
            if (bCount == 0)
            {
                string firefoxProfilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mozilla", "Firefox", "Profiles");
                if (Directory.Exists(firefoxProfilePath))
                {
                    string[] profileDirectories = Directory.GetDirectories(firefoxProfilePath);

                    foreach (string profileDir in profileDirectories)
                    {
                        string cookiesFilePath = Path.Combine(profileDir, "cookies.sqlite");
                        if (File.Exists(cookiesFilePath))
                        {
                            using (SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};", cookiesFilePath)))
                            {
                                connection.Open();
                                using (SQLiteCommand command = connection.CreateCommand())
                                {
                                    string query = "DELETE FROM moz_cookies";
                                    if (whitelistedDomain.Count > 0)
                                    {
                                        query += " WHERE ";
                                        foreach (string domain in whitelistedDomain)
                                        {
                                            query += "  host not like " + domain + " And";
                                        }
                                        query = query.Remove(query.Length - 4);
                                    }
                                    command.CommandText = query;
                                    TotalCount += command.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
            }
            Console.WriteLine("{0} Firefox cookies deleted", TotalCount);

            return TotalCount;
        }
        public void ClearIECookies()
        {
            //MessageBox.Show("Second");
            foreach (string domain in whitelistedDomain)
            {
                // Add domains to the PerSite privacy settings
                Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Internet Settings\PerSiteCookieDecision", domain, 1, RegistryValueKind.DWord);
            }

            // Clear cookies of Internet Explorer
            Process.Start("rundll32.exe", "InetCpl.cpl,ClearMyTracksByProcess 2");

            foreach (string domain in whitelistedDomain)
            {
                // Add domains to the PerSite privacy settings
                Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Internet Settings\PerSiteCookieDecision", domain, 0, RegistryValueKind.DWord);
            }
        }
        public int ClearEdgeCookies()
        {

            int TotalCount = 0;
            int bCount = IsBrowserOpen("msedge");
            //Process[] msedgeInstances = Process.GetProcessesByName("msedge");
            if (bCount == 0)
            {
                List<string> profiles = new List<string>();
                string edgeProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Microsoft\Edge\User Data\";
                string defaultProfilePath = Path.Combine(edgeProfilePath, "Default");
                if (Directory.Exists(defaultProfilePath))
                {
                    profiles.Add(defaultProfilePath);
                }

                if (Directory.Exists(edgeProfilePath))
                {
                    string[] profileDirectories = Directory.GetDirectories(edgeProfilePath, "Profile *");

                    foreach (string profileDir in profileDirectories)
                    {
                        string profilePath = Path.Combine(edgeProfilePath, profileDir);
                        profiles.Add(profilePath);
                    }
                }

                foreach (var profile in profiles)
                {
                    if (Directory.Exists(profile))
                    {
                        string cookiePath = Path.Combine(profile, "Network\\Cookies");
                        if (File.Exists(cookiePath))
                        {
                            using (SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};", cookiePath)))
                            {
                                // Clear the cookies by deleting all records from the cookies table
                                connection.Open();
                                using (SQLiteCommand cmd = connection.CreateCommand())
                                {
                                    string query = "DELETE FROM Cookies";
                                    if (whitelistedDomain.Count > 0)
                                    {
                                        query += " WHERE ";
                                        foreach (string domain in whitelistedDomain)
                                        {
                                            query += " host_key not like " + domain + " And";
                                        }
                                        query = query.Remove(query.Length - 4);
                                    }
                                    cmd.CommandText = query;
                                    cmd.Prepare();
                                    TotalCount += cmd.ExecuteNonQuery();

                                }
                                connection.Close();
                            }
                        }
                    }
                }
                Console.WriteLine($"Deleted {TotalCount} cookies.");
            }
            return TotalCount;
        }
        public int ClearOperaCookies()
        {
            int TotalCount = 0;
            int bCount = IsBrowserOpen("opera");
            //Process[] msedgeInstances = Process.GetProcessesByName("opera");
            if (bCount == 0)
            {
                var str = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var cookiePath = str + "\\Opera Software\\Opera Stable\\Network\\Cookies";
                if (File.Exists(cookiePath))
                {
                    using (SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};", cookiePath)))
                    {
                        // Clear the cookies by deleting all records from the cookies table
                        connection.Open();
                        using (SQLiteCommand cmd = connection.CreateCommand())
                        {
                            string query = "DELETE FROM Cookies";
                            if (whitelistedDomain.Count > 0)
                            {
                                query += " WHERE ";
                                foreach (string domain in whitelistedDomain)
                                {
                                    query += " host_key not like " + domain + " And";
                                }
                                query = query.Remove(query.Length - 4);
                            }
                            cmd.CommandText = query;
                            cmd.Prepare();
                            TotalCount = cmd.ExecuteNonQuery();
                            Console.WriteLine($"Deleted {TotalCount} cookies.");
                        }
                        connection.Close();
                    }
                }
            }
            return TotalCount;
        }


    }
}
