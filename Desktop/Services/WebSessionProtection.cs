using FDS.Common;
using FDS.DTO.Responses;
using FDS.Logging;
using FDS.Services.AbstractClass;
using FDS.Services.Interface;
using FDS.SingleTon;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;

using System.Net.Http;
using System.Windows.Forms;

namespace FDS.Services
{

    public class WebSessionProtection : BaseService, IService
    {
        GlobalVariables globals = GlobalVariables.Instance;
        GlobalDictionaryService globalDict = GlobalDictionaryService.Instance;

        public override int ExecuteLogicForBrowserIfClosed(string browserName, bool isBrowserClosed, List<string> whiteListDomain = null)
        {
             
            switch (browserName)
            {
                case "chrome":

                    if (isBrowserClosed)
                    {
                        if (!globals.IsLogicExecuted_ChromeCookies)
                        {                         
                            return ExecuteChromeLogic(whiteListDomain);
                        }                        
                    }
                    else
                    {
                        globals.IsLogicExecuted_ChromeCookies = false;
                    }
                    return 0;


                case "msedge":

                    if (isBrowserClosed)
                    {
                        if (!globals.IsLogicExecuted_EdgeCookies)
                        {    
                            return ExecuteEdgeLogic(whiteListDomain);
                        }                         
                    }
                    else
                    {
                        globals.IsLogicExecuted_EdgeCookies = false;
                    }
                    return 0;

                case "firefox":

                    if (isBrowserClosed)
                    {
                        if (!globals.IsLogicExecuted_FirefoxCookies)
                        {                            
                            return ExecuteFirefoxLogic(whiteListDomain);
                        }                         
                    }
                    else
                    {
                        globals.IsLogicExecuted_FirefoxCookies = false;
                    }
                    return 0;


                case "opera":

                    if (isBrowserClosed)
                    {
                        if (!globals.IsLogicExecuted_OperaCookies)
                        {
                            
                            return ExecuteOperaLogic(whiteListDomain);
                        }                        
                    }
                    else
                    {
                        globals.IsLogicExecuted_OperaCookies = false;
                    }
                    return 0;
            }
            return 0;
        }

        public int ExecuteChromeLogic(List<string> whitelistedDomain)
        {            

            string chromeProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Google\Chrome\User Data\";

            List<string> profiles = BrowsersGeneric.BrowsersProfileLists(chromeProfilePath);

            string cookiePath = "Network\\Cookies";

            var result = BrowsersDB.ClearCookies(profiles, whitelistedDomain, cookiePath);            

            if (result.Item2)
            {
                globals.IsLogicExecuted_ChromeCookies = true;
                globalDict.DictionaryService["WebSessionProtection"] = false;
            }

            return result.Item1;
        }

        protected int ExecuteEdgeLogic(List<string> whitelistedDomain)
        {           

            string edgeProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Microsoft\Edge\User Data\";

            List<string> profiles = BrowsersGeneric.BrowsersProfileLists(edgeProfilePath);

            string cookiePath = "Network\\Cookies";

            var result = BrowsersDB.ClearCookies(profiles, whitelistedDomain, cookiePath);
         
            if (result.Item2)
            {
                globals.IsLogicExecuted_EdgeCookies = true;
                globalDict.DictionaryService["WebSessionProtection"] = false;
            }

            return result.Item1;

        }

        protected int ExecuteFirefoxLogic(List<string> whitelistedDomain)
        {

            int TotalCount = 0;
            
            string firefoxProfilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mozilla", "Firefox", "Profiles");
            if (Directory.Exists(firefoxProfilePath))
            {
                string[] profileDirectories = Directory.GetDirectories(firefoxProfilePath);

                foreach (string profileDir in profileDirectories)
                {
                    string cookiesFilePath = Path.Combine(profileDir, "cookies.sqlite");
                    if (BrowsersGeneric.CheckFileExistBrowser(cookiesFilePath) > 0)
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
                                 
                                globals.IsLogicExecuted_FirefoxCookies = true;
                                globalDict.DictionaryService["WebSessionProtection"] = false;
                            }
                        }
                    }
                }
            }
            return TotalCount;            
        }

        

        protected int ExecuteOperaLogic(List<string> whitelistedDomain)
        {

            int TotalCount = 0;
            
            var str = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var cookiePath = str + "\\Opera Software\\Opera Stable\\Default\\Network\\Cookies";
            if (BrowsersGeneric.CheckFileExistBrowser(cookiePath) > 0)
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
                        globals.IsLogicExecuted_OperaCookies = true;
                         
                        globalDict.DictionaryService["WebSessionProtection"] = false;
                        Console.WriteLine($"Deleted {TotalCount} cookies.");
                    }
                    connection.Close();
                }
            }
            return TotalCount;
           
        }

        public override void LogService(string authorizationCode, string subServiceName, long FileProcessed, string ServiceId, bool IsManualExecution, string serviceTypeDetails)
        {
            DatabaseLogger databaseLogger = new DatabaseLogger();
            databaseLogger.LogInformation(authorizationCode, subServiceName, FileProcessed, ServiceId, IsManualExecution, serviceTypeDetails);
        }


        public bool RunService(SubservicesData subservices, string serviceTypeDetails)
        {
            return true;
        }




    }
}
