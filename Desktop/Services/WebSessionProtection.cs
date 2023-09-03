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
                        else
                        {
                            globals.IsLogicExecuted_ChromeCookies = false;
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

            //if (isBrowserClosed)
            //{
            //    switch (browserName)
            //    {
            //        case "chrome":
            //            return ExecuteChromeLogic(whiteListDomain);
            //        case "edge":
            //            return ExecuteEdgeLogic(whiteListDomain);
            //        case "firefox":
            //            return ExecuteFirefoxLogic(whiteListDomain);
            //        case "opera":
            //            return ExecuteOperaLogic(whiteListDomain);
            //    }
            //}
            //return 0;
        }

        public int ExecuteChromeLogic(List<string> whitelistedDomain)
        {
            string chromeProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Google\Chrome\User Data\";

            List<string> profiles = BrowsersGeneric.BrowsersProfileLists(chromeProfilePath);

            string cookiePath = "Network\\Cookies";

            var result = BrowsersDB.ClearCookies(profiles, whitelistedDomain, cookiePath);

            if(result.Item2)
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

            string firefoxProfilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mozilla", "Firefox", "Profiles");

            List<string> profiles = BrowsersGeneric.BrowsersProfileLists(firefoxProfilePath);

            string cookiePath = "cookies.sqlite";            

            var result = BrowsersDB.ClearCookies(profiles, whitelistedDomain, cookiePath);

            if (result.Item2)
            {
                globals.IsLogicExecuted_FirefoxCookies = true;
                globalDict.DictionaryService["WebSessionProtection"] = false;
            }

            return result.Item1;           

        }

        protected int ExecuteOperaLogic(List<string> whitelistedDomain)
        {

            string operaProfilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "\\Opera Software\\Opera Stable\\Network\\Cookies");

            List<string> profiles = BrowsersGeneric.BrowsersProfileLists(operaProfilePath);

            string cookiePath = "";

            var result = BrowsersDB.ClearCookies(profiles, whitelistedDomain, cookiePath);

            if (result.Item2)
            {
                globals.IsLogicExecuted_OperaCookies = true;
                globalDict.DictionaryService["WebSessionProtection"] = false;
            }

            return result.Item1;
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
