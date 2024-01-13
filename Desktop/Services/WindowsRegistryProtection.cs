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
                int totalCount = GetTotalDeletedCount();

                //int localMachineCount = DeleteLocalMachineCount();

                //int currentMachineCount = DeleteCurrentUserCount();

                //int totalCount = localMachineCount + currentMachineCount;






                //Delete Current Users Keys
                // currentUserCnt = DeleteCurrentUserCount();

                //Delete LocalMachine Keys
                //Generic.SendCommandToService("WindowsRegistryProtection");

                //Generic.SendCommandToService("WindowsRegistryProtection");

                //System.Threading.Thread.Sleep(5000);

                //string AutoStartBaseDir = Generic.GetApplicationpath();
                //string resultFilePath = Path.Combine(AutoStartBaseDir, "result.txt");

                //totalCnt = GetCurrentUserCnt(resultFilePath);

                //totalCnt = GetCurrentUserCnt(resultFilePath);

                //if (File.Exists(resultFilePath))
                //{
                //    File.Delete(resultFilePath);
                //}

                LogInformation(subservices.Sub_service_authorization_code, subservices.Sub_service_name, totalCount, Convert.ToString(subservices.Id), subservices.Execute_now, serviceTypeDetails);

            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }


        public int GetTotalDeletedCount()
        {
            int TotalCount = 0;
            try
            {
                string user = Environment.UserDomainName + "\\" + Environment.UserName;
                RegistrySecurity rs = new RegistrySecurity();
                int CUCount = 0;
                int LMCount = 0;

                // Allow the current user to read and delete the key.
                rs.AddAccessRule(new RegistryAccessRule(user,
                    RegistryRights.ReadKey | RegistryRights.WriteKey | RegistryRights.Delete,
                    InheritanceFlags.None,
                    PropagationFlags.None,
                    AccessControlType.Allow));

                RegistryKey localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey("SOFTWARE", true);

                RegistryKey LMkey = localMachine;

                if (LMkey != null)
                {
                    LMkey.SetAccessControl(rs);
                    //Console.WriteLine("Permissions granted successfully.");

                    foreach (string subkeyName in LMkey.GetSubKeyNames())
                    {
                        RegistryKey subkey = LMkey.OpenSubKey(subkeyName);
                        if (subkey.ValueCount == 0 && subkey.SubKeyCount == 0)
                        {
                            // If the subkey does not contain any values, delete it
                            LMkey.DeleteSubKeyTree(subkeyName);
                            LMCount++;
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
                                    LMCount++;
                                }
                            }
                        }
                    }
                }


                RegistryKey CUkey = Registry.CurrentUser.OpenSubKey("SOFTWARE", true);
                CUkey.SetAccessControl(rs);
                // Scan all subkeys under the defined key
                foreach (string subkeyName in CUkey.GetSubKeyNames())
                {
                    RegistryKey subkey = CUkey.OpenSubKey(subkeyName);

                    // Check if the subkey contains any values
                    if (subkey.ValueCount == 0 && subkey.SubKeyCount == 0)
                    {
                        // If the subkey does not contain any values, delete it
                        CUkey.DeleteSubKeyTree(subkeyName);
                        Console.WriteLine("Deleted empty subkey: " + subkeyName);
                        CUCount++;
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
                                // If the value is invalid or obsolete, delete it
                                subkey.DeleteValue(valueName);
                                Console.WriteLine("Deleted invalid value: " + valueName);
                                CUCount++;
                            }
                        }
                    }
                }

                Console.WriteLine("Total Regitry cleaned from current user", CUCount);
                Console.WriteLine("Total Regitry cleaned from current user", LMCount);
                TotalCount = CUCount + LMCount;

            }
            catch
            {

            }
            return TotalCount;
        }



        public int DeleteLocalMachineCount()
        {
            int localMachineCount = 0;

            string user = Environment.UserDomainName + "\\" + Environment.UserName;
            RegistrySecurity rs = new RegistrySecurity();

            // Allow the current user to read, write, and delete the key.
            rs.AddAccessRule(new RegistryAccessRule(user,
                RegistryRights.ReadKey | RegistryRights.WriteKey | RegistryRights.Delete,
                InheritanceFlags.None,
                PropagationFlags.None,
                AccessControlType.Allow));



            try
            {

                RegistryKey localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey("SOFTWARE", true);

                RegistryKey CUkey = localMachine;

                if (CUkey != null)
                {
                    CUkey.SetAccessControl(rs);
                    //Console.WriteLine("Permissions granted successfully.");

                    foreach (string subkeyName in CUkey.GetSubKeyNames())
                    {
                        RegistryKey subkey = CUkey.OpenSubKey(subkeyName);
                        if (subkey.ValueCount == 0 && subkey.SubKeyCount == 0)
                        {
                            // If the subkey does not contain any values, delete it
                            CUkey.DeleteSubKeyTree(subkeyName);
                            localMachineCount++;
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
                                    localMachineCount++;
                                }
                            }
                        }
                    }

                }
                else
                {
                    Console.WriteLine("Registry key not found.");
                }

                Console.WriteLine("Total LM Count: " + localMachineCount);

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            return localMachineCount;
        }

        public int DeleteCurrentUserCount()
        {

            int currentUserCount = 0;

            try
            {
                string user = Environment.UserDomainName + "\\" + Environment.UserName;
                RegistrySecurity rs = new RegistrySecurity();

                // Allow the current user to read, write, and delete the key.
                rs.AddAccessRule(new RegistryAccessRule(user,
                    RegistryRights.ReadKey | RegistryRights.WriteKey | RegistryRights.Delete,
                    InheritanceFlags.None,
                    PropagationFlags.None,
                    AccessControlType.Allow));


                RegistryKey CUkey = Registry.CurrentUser.OpenSubKey("SOFTWARE", true);
                CUkey.SetAccessControl(rs);
                // Scan all subkeys under the defined key
                foreach (string subkeyName in CUkey.GetSubKeyNames())
                {
                    RegistryKey subkey = CUkey.OpenSubKey(subkeyName);

                    // Check if the subkey contains any values
                    if (subkey.ValueCount == 0 && subkey.SubKeyCount == 0)
                    {
                        // If the subkey does not contain any values, delete it
                        CUkey.DeleteSubKeyTree(subkeyName);
                        Console.WriteLine("Deleted empty subkey: " + subkeyName);
                        currentUserCount++;
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
                                // If the value is invalid or obsolete, delete it
                                subkey.DeleteValue(valueName);
                                Console.WriteLine("Deleted invalid value: " + valueName);
                                currentUserCount++;
                            }
                        }
                    }
                }

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
