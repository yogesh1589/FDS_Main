using FDS_Administrator.General;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace FDS_Administrator
{
    public class WindowsRegistryProtection
    {
        public bool DeleteRegistriesKey()
        {             
            try
            {

                int localMachineCount = DeleteLocalMachineCount();
                int currentMachineCount = DeleteCurrentUserCount();

                string AutoStartBaseDir = Generic.GetApplicationpath();
                string resultFilePath = Path.Combine(AutoStartBaseDir, "result.txt");

                int totalCount = localMachineCount + currentMachineCount;

                File.WriteAllText(resultFilePath, totalCount.ToString());
            }
            catch (Exception)
            {
                return false;
            }
            return true;
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

    }
}
