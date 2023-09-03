using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FDS.SingleTon
{
    public class RSASingleTon
    {
        private static RSASingleTon instance;

        // Private constructor to prevent external instantiation.
        private RSASingleTon() { }

        // Public method to get or create the Singleton instance.
        public static RSASingleTon GetInstance()
        {
            if (instance == null)
            {
                instance = new RSASingleTon();
            }
            return instance;
        }

        // Properties to hold the RSA keys.
        public RSACryptoServiceProvider RSAServer { get; set; }
        public RSACryptoServiceProvider RSADevice { get; set; }
    }
}
