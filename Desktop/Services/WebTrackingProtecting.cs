using FDS.Common;
using FDS.DTO.Responses;
using FDS.Logging;
using FDS.Services.AbstractClass;
using FDS.Services.Interface;
using FDS.SingleTon;
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
    public class WebTrackingProtecting : BaseService,IService
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
                        if (!globals.IsLogicExecuted_ChromeHistory)
                        {
                            return ExecuteChromeLogic();
                        }                        
                    }
                    else
                    {
                        globals.IsLogicExecuted_ChromeHistory = false;
                    }
                    return 0;


                case "msedge":

                    if (isBrowserClosed)
                    {
                        if (!globals.IsLogicExecuted_EdgeHistory)
                        {                            
                            return ExecuteEdgeLogic();
                        }
                    }
                    else
                    {
                        globals.IsLogicExecuted_EdgeHistory = false;
                    }
                    return 0;

                case "firefox":

                    if (isBrowserClosed)
                    {
                        if (!globals.IsLogicExecuted_FirefoxHistory)
                        {
                            return ExecuteFirefoxLogic();
                        }                        
                    }
                    else
                    {
                        globals.IsLogicExecuted_FirefoxHistory = false;
                    }
                    return 0;


                case "opera":

                    if (isBrowserClosed)
                    {
                        if (!globals.IsLogicExecuted_OperaHistory)
                        {
                            return ExecuteOperaLogic();
                        }                        
                    }
                    else
                    {
                        globals.IsLogicExecuted_OperaHistory = false;
                    }
                    return 0;


            }

            //if (isBrowserClosed)
            //{
            //    switch (browserName)
            //    {
            //        case "chrome":
            //            return ExecuteChromeLogic();
            //        case "edge":
            //            return ExecuteEdgeLogic();
            //        case "firefox":
            //            return ExecuteFirefoxLogic();
            //        case "opera":
            //            return ExecuteOperaLogic();
            //    }
            //}
            return 0;
        }



        protected int ExecuteChromeLogic()
        {
            string chromeProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Google\Chrome\User Data\";

            List<string> profiles = BrowsersGeneric.BrowsersProfileLists(chromeProfilePath);

            var result = BrowsersDB.ClearHistory(profiles);

            if (result.Item2)
            {
                globals.IsLogicExecuted_ChromeHistory = true;
                globalDict.DictionaryService["WebTrackingProtecting"] = false;
            }

            return result.Item1;

        }

        protected int ExecuteEdgeLogic()
        {
            string edgeProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Microsoft\Edge\User Data\";

            List<string> profiles = BrowsersGeneric.BrowsersProfileLists(edgeProfilePath);

            var result = BrowsersDB.ClearHistory(profiles);

            if (result.Item2)
            {
                globals.IsLogicExecuted_EdgeHistory = true;
                globalDict.DictionaryService["WebTrackingProtecting"] = false;
            }

            return result.Item1;
        }

        protected int ExecuteFirefoxLogic()
        {
            int totalCount = 0;
            string firefoxProfilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mozilla", "Firefox", "Profiles");
            if (Directory.Exists(firefoxProfilePath))
            {
                string[] profileDirectories = Directory.GetDirectories(firefoxProfilePath);

                foreach (string profileDir in profileDirectories)
                {
                    string placesFilePath = Path.Combine(profileDir, "places.sqlite");
                    if (BrowsersGeneric.CheckFileExistBrowser(placesFilePath) > 0)
                    {
                        globals.IsLogicExecuted_FirefoxHistory = true;
                        using (SQLiteConnection connection = new SQLiteConnection($"Data Source={placesFilePath};Version=3;"))
                        {
                            connection.Open();

                            using (SQLiteCommand command = connection.CreateCommand())
                            {
                                command.CommandText = "DELETE FROM moz_places";
                                totalCount += command.ExecuteNonQuery();


                                globals.IsLogicExecuted_FirefoxHistory = true;
                                globalDict.DictionaryService["WebTrackingProtecting"] = false;

                            }
                        }
                    }
                }
            }
            return totalCount;
        }

        protected int ExecuteOperaLogic()
        {
            int totalCount = 0;
            string historyPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Opera Software\Opera Stable\";
            if (Directory.Exists(historyPath))
            {
                if (BrowsersGeneric.CheckFileExistBrowser(historyPath + "History") > 0)
                {
                    globals.IsLogicExecuted_OperaHistory = true;
                    {
                        using (SQLiteConnection connection = new SQLiteConnection("Data Source=" + historyPath + "History"))
                        {
                            connection.Open();
                            using (SQLiteCommand command = new SQLiteCommand("DELETE FROM urls", connection))
                            {
                                totalCount = command.ExecuteNonQuery();

                                globals.IsLogicExecuted_OperaHistory = true;
                                globalDict.DictionaryService["WebTrackingProtecting"] = false;
                            }
                        }
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
