using FDS.Common;
using FDS.DTO.Responses;
using FDS.Logging;
using FDS.Services.Interface;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Windows;

namespace FDS.Services
{
    public class WindowsRegistryProtection : IService, ILogger
    {
        public bool RunService(SubservicesData subservices, string serviceTypeDetails)
        {
            try
            {
                int totalCnt = 0;


                //Delete Current Users Keys
                // currentUserCnt = DeleteCurrentUserCount();

                //Delete LocalMachine Keys
                //Generic.SendCommandToService("WindowsRegistryProtection");

                Generic.SendCommandToService("WindowsRegistryProtection");

                System.Threading.Thread.Sleep(5000);

                string AutoStartBaseDir = Generic.GetApplicationpath();
                string resultFilePath = Path.Combine(AutoStartBaseDir, "result.txt");

                totalCnt = GetCurrentUserCnt(resultFilePath);

                totalCnt = GetCurrentUserCnt(resultFilePath);

                if (File.Exists(resultFilePath))
                {
                    File.Delete(resultFilePath);
                }

                LogInformation(subservices.Sub_service_authorization_code, subservices.Sub_service_name, totalCnt, Convert.ToString(subservices.Id), subservices.Execute_now, serviceTypeDetails);

            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }


        public int DeleteCurrentUserCount()
        {

            int currentUserCount = 0;

            try
            {

                RegistryKey currentUser = Registry.CurrentUser.OpenSubKey("SOFTWARE", true);

                //RegistryKey CUkey = Registry.CurrentUser.OpenSubKey("SOFTWARE", true);

                if (currentUser != null)
                {
                    currentUserCount = TotalCountDeleted(currentUser);
                }
                else
                {
                    Console.WriteLine("Registry key not found.");
                }

                Console.WriteLine("Total LM Count: " + currentUserCount);

            }
            catch { }
            return currentUserCount;
        }

        public int TotalCountDeleted(RegistryKey CUkey)
        {
            int cntDeleted = 0;

            try
            {
                foreach (string subkeyName in CUkey.GetSubKeyNames())
                {
                    RegistryKey subkey = CUkey.OpenSubKey(subkeyName);
                    if (subkey.ValueCount == 0 && subkey.SubKeyCount == 0)
                    {
                        // If the subkey does not contain any values, delete it
                        CUkey.DeleteSubKeyTree(subkeyName);
                        cntDeleted++;
                    }
                    else
                    {
                        // If the subkey contains values, check if they are valid
                        foreach (string valueName in subkey.GetValueNames())
                        {
                            object value = subkey.GetValue(valueName);
                            // Check if the value is invalid or obsolete
                            if (value == null || value.ToString().Contains("[obsolete]"))
                            {
                                subkey.DeleteValue(valueName);
                                cntDeleted++;
                            }
                        }
                    }
                }
            }
            catch
            { }

            return cntDeleted;
        }

        public int GetCurrentUserCnt(string resultFilePath)
        {


            try
            {
                System.Threading.Thread.Sleep(5000);

                if (File.Exists(resultFilePath))
                {
                    string result = File.ReadAllText(resultFilePath);
                    Console.WriteLine("Result from console application: " + result);
                    // Delete the file after reading its content                 

                    return string.IsNullOrEmpty(result) ? 0 : int.TryParse(result, out int parsedValue) ? parsedValue : 0;
                }
                else
                {
                    return 0;
                }
            }
            catch (Exception ex)
            {
                return 0;
            }
        }


        public void LogInformation(string authorizationCode, string subServiceName, long FileProcessed, string ServiceId, bool IsManualExecution, string serviceTypeDetails)
        {
            DatabaseLogger databaseLogger = new DatabaseLogger();
            databaseLogger.LogInformation(authorizationCode, subServiceName, FileProcessed, ServiceId, IsManualExecution, serviceTypeDetails);
        }


    }
}
