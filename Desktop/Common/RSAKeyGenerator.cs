using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FDS.Common
{
    public static class RSAKeyGenerator
    {
        /// <summary>
        /// Generates an RSA key pair (private and public keys).
        /// </summary>
        /// <returns>A tuple containing the private and public RSA keys.</returns>
        public static (RSAParameters PrivateKey, RSAParameters PublicKey) GenerateKeys()
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))
            {
                // Export the private and public keys
                return (rsa.ExportParameters(true), rsa.ExportParameters(false));
            }
        }
    }
}
