using FDS.DTO.Responses;
using FDS.Logging;
using FDS.Services.AbstractClass;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDS.Services
{
    public class WebTrackingProtection : BaseService
    {
        public WebTrackingProtection(ILogger logger) : base(logger)
        {
        }

        public override void RunService(SubservicesData subservices)
        {

            try
            {
                int ChromeCount = ClearChromeHistory();
                int FireFoxCount = ClearFireFoxHistory();
                int EdgeCount = ClearEdgeHistory();
                int OperaCount = ClearOperaHistory();

                //int TotalCount = EdgeCount + ChromeCount;
                int TotalCount = ChromeCount + FireFoxCount + EdgeCount + OperaCount;

                LogServicesData(subservices.Sub_service_authorization_code, subservices.Sub_service_name, 0, Convert.ToString(subservices.Id), subservices.Execute_now);

            }
            catch (Exception exp)
            {
                exp.ToString();
            }
        }

        public int ClearChromeHistory()
        {
            int TotalCount = 0;


            int bCount = IsBrowserOpen("chrome");
            //MessageBox.Show(bCount.ToString());
            //Process[] chromeInstances = Process.GetProcesses("chrome");
            if (bCount == 0)
            {
                //MessageBox.Show("Chrome History Deletion Started");
                List<string> profiles = new List<string>();
                string chromeProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Google\Chrome\User Data\";
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
                        string historyPath = Path.Combine(profile, "History");
                        if (File.Exists(historyPath))
                        {
                            using (SQLiteConnection connection = new SQLiteConnection("Data Source=" + historyPath + ";Version=3;New=False;Compress=True;"))
                            {
                                connection.Open();
                                using (SQLiteCommand command = new SQLiteCommand("DELETE FROM urls", connection))
                                {
                                    TotalCount += command.ExecuteNonQuery();
                                }
                                connection.Close();
                                //MessageBox.Show("Chrome History Deleted Sucessfully");
                            }
                        }
                    }
                }

                Console.WriteLine("Total number of history deleted: " + TotalCount);
            }
            return TotalCount;
        }
        public int ClearFireFoxHistory()
        {
            int TotalCount = 0;


            int bCount = IsBrowserOpen("firefox");


            //Process[] firefoxInstances = Process.GetProcessesByName("firefox");
            if (bCount == 0)
            {
                try
                {
                    string firefoxProfilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mozilla", "Firefox", "Profiles");
                    if (Directory.Exists(firefoxProfilePath))
                    {
                        string[] profileDirectories = Directory.GetDirectories(firefoxProfilePath);

                        foreach (string profileDir in profileDirectories)
                        {
                            string placesFilePath = Path.Combine(profileDir, "places.sqlite");
                            if (File.Exists(placesFilePath))
                            {
                                using (SQLiteConnection connection = new SQLiteConnection($"Data Source={placesFilePath};Version=3;"))
                                {
                                    connection.Open();

                                    using (SQLiteCommand command = connection.CreateCommand())
                                    {
                                        command.CommandText = "DELETE FROM moz_places";
                                        TotalCount += command.ExecuteNonQuery();
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle any exceptions that occur while clearing Firefox history
                }
            }
            return TotalCount;
        }
        public void ClearIEHitory()
        {
            // MessageBox.Show("Third");
            // Get current number of history items
            int currentCount = 0;
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Internet Explorer\TypedURLs"))
            {
                if (key != null)
                {
                    currentCount = key.ValueCount;
                }
            }

            // Clear browsing history of Internet Explorer
            Process.Start("rundll32.exe", "InetCpl.cpl,ClearMyTracksByProcess 1");

            // Get new number of history items
            int newCount = 0;
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Internet Explorer\TypedURLs"))
            {
                if (key != null)
                {
                    newCount = key.ValueCount;
                }
            }

            // Calculate number of items cleared
            int countCleared = currentCount - newCount;

        }
        public int ClearEdgeHistory()
        {
            //MessageBox.Show("History Start 1");
            int TotalCount = 0;


            int bCount = IsBrowserOpen("msedge");


            //Process[] msedgeInstances = Process.GetProcessesByName("msedge");
            if (bCount == 0)
            {
                // Connect to the Edge History database
                //string historyPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Microsoft\Edge\User Data\Default\History";
                List<string> profiles = new List<string>();
                string edgeProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Microsoft\Edge\User Data\";
                string defaultProfilePath = Path.Combine(edgeProfilePath, "Default");
                if (Directory.Exists(defaultProfilePath))
                {
                    profiles.Add(defaultProfilePath);
                }
                //MessageBox.Show("History Start 2 - " + edgeProfilePath);
                if (Directory.Exists(edgeProfilePath))
                {
                    string[] profileDirectories = Directory.GetDirectories(edgeProfilePath, "Profile *");

                    foreach (string profileDir in profileDirectories)
                    {
                        string profilePath = Path.Combine(edgeProfilePath, profileDir);
                        profiles.Add(profilePath);
                    }
                }



                //MessageBox.Show("History Start 3 - " + profiles.Count.ToString());
                foreach (var profile in profiles)
                {
                    if (Directory.Exists(profile))
                    {
                        string historyPath = Path.Combine(profile, "History");

                        if (File.Exists(historyPath))
                        {
                            string connectionString = "Data Source=" + historyPath + ";Version=3;New=False;Compress=True;";
                            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                            {
                                connection.Open();
                                try
                                {
                                    // Perform your database operations here
                                    // Delete all browsing history records
                                    using (SQLiteCommand command = new SQLiteCommand("DELETE FROM urls;", connection))
                                    {
                                        TotalCount += command.ExecuteNonQuery();

                                    }
                                }
                                catch (Exception ex)
                                {
                                    // Handle any exceptions that occur during the transaction
                                    Console.WriteLine("An error occurred: " + ex.Message);

                                }
                                finally
                                {
                                    connection.Close();
                                }
                            }
                        }
                    }
                }


            }
            return TotalCount;
            //MessageBox.Show("History Start 3 - Done");
            Console.WriteLine($"Deleted {TotalCount} browsing history items.");
        }
        public int ClearOperaHistory()
        {
            int TotalCount = 0;


            int bCount = IsBrowserOpen("opera");

            //Process[] OperaInstances = Process.GetProcessesByName("opera");
            if (bCount == 0)
            {
                // Set the path to the Opera profile directory
                string historyPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Opera Software\Opera Stable\";
                if (Directory.Exists(historyPath))
                {
                    // Connect to the history database file
                    using (SQLiteConnection connection = new SQLiteConnection("Data Source=" + historyPath + "History"))
                    {
                        connection.Open();

                        // Execute the SQL command to delete the browsing history
                        using (SQLiteCommand command = new SQLiteCommand("DELETE FROM urls", connection))
                        {
                            TotalCount = command.ExecuteNonQuery();
                        }
                    }
                }

                Console.WriteLine("Deleted {0} history records.", TotalCount);
            }
            return TotalCount;
        }
    }
}
