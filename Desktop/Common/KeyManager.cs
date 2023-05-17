using CredentialManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FDS.Common
{
    public class KeyManager
    {

        public static void SaveValue(string key, string value)
        {
            using (var cred = new Credential())
            {
                cred.Password = value;
                cred.Target = AppConstants.KeyPrfix + key;
                cred.Type = CredentialType.Generic;
                cred.PersistanceType = PersistanceType.LocalComputer;
                cred.Save();
            }

        }

        public static string GetValue(string key)
        {
            using (var cred = new Credential())
            {
                cred.Target = AppConstants.KeyPrfix + key;
                cred.Load();
                return cred.Password;
            }
        }
    }
}
