using FDS.DTO.Responses;
using FDS.Logging;
using FDS.Services.AbstractClass;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDS.Services
{
    public class WebCacheProtection : BaseService
    {
        public WebCacheProtection(ILogger logger) : base(logger)
        {
        }

        public override void RunService(SubservicesData subservices)
        {

            try
            {
                long ChromeCount = ClearChromeCache();
                long FireFoxCount = ClearFirefoxCache();
                long EdgeCount = ClearEdgeCache();
                long OperaCount = ClearOperaCache();

                long TotalSize = ChromeCount + FireFoxCount + EdgeCount + OperaCount;
                LogServicesData(subservices.Sub_service_authorization_code, subservices.Sub_service_name, TotalSize, Convert.ToString(subservices.Id), subservices.Execute_now);

            }
            catch (Exception exp)
            {
                exp.ToString();
            }
        }

        public long ClearChromeCache()
        {
            int TotalCount = 0;
            long TotalSize = 0;


            int bCount = IsBrowserOpen("chrome");


            //Process[] chromeInstances = Process.GetProcessesByName("chrome");
            if (bCount == 0)
            {
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
                        string CachePath = Path.Combine(profile, "Cache\\Cache_Data");
                        if (Directory.Exists(CachePath))
                        {
                            // Clear the cache folder
                            foreach (string file in Directory.GetFiles(CachePath))
                            {
                                try
                                {
                                    TotalSize += file.Length;
                                    File.Delete(file);
                                    TotalCount++;
                                }
                                catch (IOException) { } // handle any exceptions here
                            }

                        }
                    }
                    else
                    {
                        Console.WriteLine("Chrome Cache file not found.");
                    }
                }
                Console.WriteLine($"Total {TotalSize} files cleared from Chrome cache.");
            }
            return TotalSize;
        }
        public long ClearFirefoxCache()
        {
            int TotalCount = 0;
            long TotalSize = 0;
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
                        string[] cacheFolders = { "cache2", "shader-cache", "browser-extension-data", "startupCache", "thumbnails" };
                        foreach (string folder in cacheFolders)
                        {
                            string cachePath = Path.Combine(profileDir, folder);
                            if (Directory.Exists(cachePath))
                            {
                                foreach (string file in Directory.GetFiles(cachePath))
                                {
                                    try
                                    {
                                        FileInfo info = new FileInfo(file);
                                        TotalCount++;
                                        TotalSize += info.Length;
                                        File.Delete(file);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error deleting file: {ex.Message}");
                                    }
                                }
                                Console.WriteLine($"Deleted {TotalCount} file of total {TotalSize} bytes of {folder} cache");
                            }
                            else
                            {
                                Console.WriteLine($"{folder} cache folder not found");
                            }
                        }
                    }
                }
                Console.WriteLine("{0} Firefox cache items deleted", TotalSize);
            }
            return TotalSize;
        }
        public long ClearEdgeCache()
        {
            int TotalCount = 0;
            long TotalSize = 0;
            int bCount = IsBrowserOpen("msedge");
            //Process[] msedgeInstances = Process.GetProcessesByName("msedge");
            if (bCount == 0)
            {
                // Connect to the Edge cache database
                //string cachePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Microsoft\Edge\User Data\Default\Cache\Cache_Data";
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
                    if (File.Exists(profile))
                    {
                        string cachePath = Path.Combine(profile, "Cache\\Cache_Data");
                        if (File.Exists(cachePath))
                        {
                            // Clear the cache folder
                            foreach (string file in Directory.GetFiles(cachePath))
                            {
                                try
                                {
                                    TotalSize += file.Length;
                                    File.Delete(file);
                                    TotalCount++;
                                }
                                catch (IOException) { } // handle any exceptions here
                            }
                        }
                    }
                }
                Console.WriteLine($"Total {TotalSize} files cleared from Chrome cache.");
            }
            return TotalSize;
        }
        public void ClearIECache()
        {
            // MessageBox.Show("4");
            // Get current number of cache items
            int currentCount = 0;
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings\Cache"))
            {
                if (key != null)
                {
                    currentCount = key.ValueCount;
                }
            }

            // Clear cache of Internet Explorer
            Process.Start("rundll32.exe", "InetCpl.cpl,ClearMyTracksByProcess 8");

            // Get new number of cache items
            int newCount = 0;
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings\Cache"))
            {
                if (key != null)
                {
                    newCount = key.ValueCount;
                }
            }

            // Calculate number of items cleared
            int countCleared = currentCount - newCount;
            Console.WriteLine($"Deleted {countCleared} cache cleared");
        }
        public long ClearOperaCache()
        {
            int TotalCount = 0;
            long TotalSize = 0;
            int bCount = IsBrowserOpen("opera");
            // Process[] OperaInstances = Process.GetProcessesByName("opera");
            if (bCount == 0)
            {
                // Set the path to the Opera profile directory
                string cachePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Opera Software\Opera Stable\Cache\Cache_Data";
                if (Directory.Exists(cachePath))
                {
                    // Delete all files in the cache directory
                    foreach (string file in Directory.GetFiles(cachePath))
                    {
                        TotalSize += file.Length;
                        File.Delete(file);
                        TotalCount++;
                    }
                }
                Console.WriteLine("Deleted {0} files from the cache.", TotalSize);
            }
            return TotalSize;
        }
    }
}
