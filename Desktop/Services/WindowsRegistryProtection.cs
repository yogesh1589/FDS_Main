using FDS.DTO.Responses;
using FDS.Logging;
using FDS.Services.Interface;
using Microsoft.Win32;
using System;
using System.Security.AccessControl;

namespace FDS.Services
{
    public class WindowsRegistryProtection : IService, ILogger
    {
        public bool RunService(SubservicesData subservices,string serviceTypeDetails)
        {
            int totalCount = 0;
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

                RegistryKey localMachine = Environment.Is64BitProcess == true ? RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64) : Registry.LocalMachine;//Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Mozilla\Mozilla Firefox\", true);

                var key = localMachine.OpenSubKey(@"SOFTWARE", true);
                key.SetAccessControl(rs);
                // Scan all subkeys under the defined key
                foreach (string subkeyName in key.GetSubKeyNames())
                {
                    RegistryKey subkey = key.OpenSubKey(subkeyName);

                    //Check if the subkey contains any values
                    if (subkey.ValueCount == 0 && subkey.SubKeyCount == 0)
                    {
                        // If the subkey does not contain any values, delete it
                        key.DeleteSubKeyTree(subkeyName);
                        Console.WriteLine("Deleted empty subkey: " + subkeyName);
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
                                // If the value is invalid or obsolete, delete it
                                subkey.DeleteValue(valueName);
                                Console.WriteLine("Deleted invalid value: " + valueName);
                                LMCount++;
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
                totalCount = CUCount + LMCount;

                LogInformation(subservices.Sub_service_authorization_code, subservices.Sub_service_name, totalCount, Convert.ToString(subservices.Id), subservices.Execute_now, serviceTypeDetails);

            }
            catch (Exception exp)
            {
                exp.ToString();
            }
            return true;
        }

        public void LogInformation(string authorizationCode, string subServiceName, long FileProcessed, string ServiceId, bool IsManualExecution, string serviceTypeDetails)
        {
            DatabaseLogger databaseLogger = new DatabaseLogger();
            databaseLogger.LogInformation(authorizationCode, subServiceName, FileProcessed, ServiceId, IsManualExecution, serviceTypeDetails);
        }


    }
}
