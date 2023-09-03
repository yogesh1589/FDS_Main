using FDS.DTO.Responses;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDS.Logging
{
    static class BrowsersDB
    {
        public static (int, bool) ClearCookies(List<string> profiles, List<string> whitelistedDomain, string cookiePath)
        {

            int totalCount = 0;
            bool isServiceExecute = false;

            foreach (var profile in profiles)
            {
                if (Directory.Exists(profile))
                {
                    string cookiesPath = Path.Combine(profile, cookiePath);

                    if (CheckFileExistBrowser(cookiesPath) > 0)
                    {
                        using (SQLiteConnection connection = new SQLiteConnection(string.Format("Data Source={0};", cookiesPath)))
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
                                totalCount += cmd.ExecuteNonQuery();
                                isServiceExecute = true;
                            }
                            connection.Close();
                        }
                    }
                }
            }

            return (totalCount, isServiceExecute);
        }

        public static (int, bool) ClearCache(List<string> profiles, string cachePath)
        {

            int totalCount = 0;
            int totalSize = 0;
            bool isServiceExecute = false;

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
                                totalSize += file.Length;
                                File.Delete(file);
                                totalCount++;
                                
                            }
                            catch (IOException) { } // handle any exceptions here
                        }
                        isServiceExecute = true;

                    }
                }
                else
                {
                    Console.WriteLine("Chrome Cache file not found.");
                }
            }
            return (totalCount, isServiceExecute);
        }

        public static (int, bool) ClearHistory(List<string> profiles)
        {
            int totalCount = 0;
            bool isServiceExecuted = false;

            foreach (var profile in profiles)
            {
                if (Directory.Exists(profile))
                {
                    string historyPath = Path.Combine(profile, "History");

                    if (CheckFileExistBrowser(historyPath) > 0)
                    {
                        {
                            using (SQLiteConnection connection = new SQLiteConnection("Data Source=" + historyPath + ";Version=3;New=False;Compress=True;"))
                            {
                                connection.Open();

                                using (SQLiteCommand command = new SQLiteCommand("DELETE FROM urls", connection))
                                {
                                    totalCount += command.ExecuteNonQuery();
                                }

                                isServiceExecuted = true;
                                connection.Close();

                            }
                        }
                    }
                }
            }

            return (totalCount, isServiceExecuted);
        }

        public static double CheckFileExistBrowser(string fullPath)
        {
            FileInfo fileInfo = new FileInfo(fullPath);
            double fileSizeInKb = 0;
            if (fileInfo.Exists)
            {
                long fileSizeInBytes = fileInfo.Length;
                fileSizeInKb = fileSizeInBytes / 1024.0; // Convert to kilobytes              
            }
            return fileSizeInKb;
        }
    }
}
