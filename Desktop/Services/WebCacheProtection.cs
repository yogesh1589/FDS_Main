using FDS.Common;
using FDS.DTO.Responses;
using FDS.Logging;
using FDS.Services.AbstractClass;
using FDS.Services.Interface;
using FDS.SingleTon;
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
    public class WebCacheProtection : BaseService,IService
    {
        GlobalVariables globals = GlobalVariables.Instance;
        GlobalDictionaryService globalDict = GlobalDictionaryService.Instance;

        public override int ExecuteLogicForBrowserIfClosed(string browserName, bool isBrowserClosed, List<string> whitelistedDomain = null)
        {
            switch (browserName)
            {
                case "chrome":

                    if (isBrowserClosed)
                    {
                        if (!globals.IsLogicExecuted_ChromeCache)
                        {
                            return ExecuteChromeLogic();
                        }
                    }
                    else
                    {
                        globals.IsLogicExecuted_ChromeCache = false;
                    }
                    return 0;


                case "msedge":

                    if (isBrowserClosed)
                    {
                        if (!globals.IsLogicExecuted_EdgeCache)
                        {
                            return ExecuteEdgeLogic();
                        }
                    }
                    else
                    {
                        globals.IsLogicExecuted_EdgeCache = false;
                    }
                    return 0;

                case "firefox":

                    if (isBrowserClosed)
                    {
                        if (!globals.IsLogicExecuted_FirefoxCache)
                        {
                            return ExecuteFirefoxLogic();
                        }
                    }
                    else
                    {
                        globals.IsLogicExecuted_FirefoxCache = false;
                    }
                    return 0;


                case "opera":

                    if (isBrowserClosed)
                    {
                        if (!globals.IsLogicExecuted_OperaCache)
                        {
                            return ExecuteOperaLogic();
                        }
                    }
                    else
                    {
                        globals.IsLogicExecuted_OperaCache = false;
                    }
                    return 0;
            }
            return 0;
        }

        protected int ExecuteChromeLogic()
        {
            string chromeProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Google\Chrome\User Data\";

            List<string> profiles = BrowsersGeneric.BrowsersProfileLists(chromeProfilePath);

            string cachePath = "Cache\\Cache_Data";

            var result = BrowsersDB.ClearCache(profiles, cachePath);

            if (result.Item2)
            {
                globals.IsLogicExecuted_ChromeCache = true;
                globalDict.DictionaryService["WebCacheProtection"] = false;
            }

            return result.Item1;
        }

        protected int ExecuteEdgeLogic()
        {

            string edgeProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Microsoft\Edge\User Data\";

            List<string> profiles = BrowsersGeneric.BrowsersProfileLists(edgeProfilePath);

            string cachePath = "Cache\\Cache_Data";

            var result = BrowsersDB.ClearCache(profiles, cachePath);

            if (result.Item2)
            {
                globals.IsLogicExecuted_EdgeCache = true;
                globalDict.DictionaryService["WebCacheProtection"] = false;
            }

            return result.Item1;

        }

        protected int ExecuteOperaLogic()
        {
            int totalSize = 0;
            int totalCount = 0;

            string cachePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Opera Software\Opera Stable\Cache\Cache_Data";
            if (Directory.Exists(cachePath))
            {
                // Delete all files in the cache directory
                foreach (string file in Directory.GetFiles(cachePath))
                {
                    totalSize += file.Length;
                    File.Delete(file);
                    totalCount++;
                    globals.IsLogicExecuted_OperaCache = true;
                    globalDict.DictionaryService["WebCacheProtection"] = false;
                }
            }
            return totalCount;
        }

        protected int ExecuteFirefoxLogic()
        {

            long totalSize = 0;
            int totalCount = 0;

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
                                    totalCount++;
                                    totalSize += info.Length;
                                    File.Delete(file);
                                    globals.IsLogicExecuted_FirefoxCache = true;
                                    globalDict.DictionaryService["WebCacheProtection"] = false;
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error deleting file: {ex.Message}");
                                }
                            }
                        }
                        //else
                        //{
                        //    Console.WriteLine($"{folder} cache folder not found");
                        //}
                    }
                }
            }
            return totalCount;
        }

        public bool RunService(SubservicesData subservices, string serviceTypeDetails)
        {
            return true;
        }

        public override void LogService(string authorizationCode, string subServiceName, long FileProcessed, string ServiceId, bool IsManualExecution, string serviceTypeDetails)
        {
            DatabaseLogger databaseLogger = new DatabaseLogger();
            databaseLogger.LogInformation(authorizationCode, subServiceName, FileProcessed, ServiceId, IsManualExecution, serviceTypeDetails);
        }

    }
}
