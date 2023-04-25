using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Desktop.Common
{
    public class RSAKeys
    {
        /// <summary>
        /// Import OpenSSH PEM private key string into MS RSACryptoServiceProvider
        /// </summary>
        /// <param name="pem"></param>
        /// <returns></returns>
        public static RSACryptoServiceProvider ImportPrivateKey(string pem)
        {
            PemReader pr = new PemReader(new StringReader(pem));
            AsymmetricCipherKeyPair KeyPair = (AsymmetricCipherKeyPair)pr.ReadObject();
            RSAParameters rsaParams = DotNetUtilities.ToRSAParameters((RsaPrivateCrtKeyParameters)KeyPair.Private);

            RSACryptoServiceProvider csp = new RSACryptoServiceProvider(2048);// cspParams);
            csp.ImportParameters(rsaParams);
            return csp;
        }

        /// <summary>
        /// Import OpenSSH PEM public key string into MS RSACryptoServiceProvider
        /// </summary>
        /// <param name="pem"></param>
        /// <returns></returns>
        public static RSACryptoServiceProvider ImportPublicKey(string pem)
        {
            PemReader pr = new PemReader(new StringReader(pem));
            AsymmetricKeyParameter publicKey = (AsymmetricKeyParameter)pr.ReadObject();
            RSAParameters rsaParams = DotNetUtilities.ToRSAParameters((RsaKeyParameters)publicKey);

            RSACryptoServiceProvider csp = new RSACryptoServiceProvider(2048);// cspParams);
            csp.ImportParameters(rsaParams);
            return csp;
        }

        /// <summary>
        /// Export private (including public) key from MS RSACryptoServiceProvider into OpenSSH PEM string
        /// slightly modified from https://stackoverflow.com/a/23739932/2860309
        /// </summary>
        /// <param name="csp"></param>
        /// <returns></returns>
        public static string ExportPrivateKey(RSACryptoServiceProvider csp)
        {
            StringWriter outputStream = new StringWriter();
            if (csp.PublicOnly) throw new ArgumentException("CSP does not contain a private key", "csp");
            var parameters = csp.ExportParameters(true);
            using (var stream = new MemoryStream())
            {
                var writer = new BinaryWriter(stream);
                writer.Write((byte)0x30); // SEQUENCE
                using (var innerStream = new MemoryStream())
                {
                    var innerWriter = new BinaryWriter(innerStream);
                    EncodeIntegerBigEndian(innerWriter, new byte[] { 0x00 }); // Version
                    EncodeIntegerBigEndian(innerWriter, parameters.Modulus);
                    EncodeIntegerBigEndian(innerWriter, parameters.Exponent);
                    EncodeIntegerBigEndian(innerWriter, parameters.D);
                    EncodeIntegerBigEndian(innerWriter, parameters.P);
                    EncodeIntegerBigEndian(innerWriter, parameters.Q);
                    EncodeIntegerBigEndian(innerWriter, parameters.DP);
                    EncodeIntegerBigEndian(innerWriter, parameters.DQ);
                    EncodeIntegerBigEndian(innerWriter, parameters.InverseQ);
                    var length = (int)innerStream.Length;
                    EncodeLength(writer, length);
                    writer.Write(innerStream.GetBuffer(), 0, length);
                }

                var base64 = Convert.ToBase64String(stream.GetBuffer(), 0, (int)stream.Length).ToCharArray();
                // WriteLine terminates with \r\n, we want only \n
                outputStream.Write("-----BEGIN RSA PRIVATE KEY-----\n");
                // Output as Base64 with lines chopped at 64 characters
                for (var i = 0; i < base64.Length; i += 64)
                {
                    outputStream.Write(base64, i, Math.Min(64, base64.Length - i));
                    outputStream.Write("\n");
                }
                outputStream.Write("-----END RSA PRIVATE KEY-----");
            }

            return outputStream.ToString();
        }

        /// <summary>
        /// Export public key from MS RSACryptoServiceProvider into OpenSSH PEM string
        /// slightly modified from https://stackoverflow.com/a/28407693
        /// </summary>
        /// <param name="csp"></param>
        /// <returns></returns>
        public static string ExportPublicKey(RSACryptoServiceProvider csp)
        {
            StringWriter outputStream = new StringWriter();
            var parameters = csp.ExportParameters(false);
            using (var stream = new MemoryStream())
            {
                var writer = new BinaryWriter(stream);
                writer.Write((byte)0x30); // SEQUENCE
                using (var innerStream = new MemoryStream())
                {
                    var innerWriter = new BinaryWriter(innerStream);
                    innerWriter.Write((byte)0x30); // SEQUENCE
                    EncodeLength(innerWriter, 13);
                    innerWriter.Write((byte)0x06); // OBJECT IDENTIFIER
                    var rsaEncryptionOid = new byte[] { 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d, 0x01, 0x01, 0x01 };
                    EncodeLength(innerWriter, rsaEncryptionOid.Length);
                    innerWriter.Write(rsaEncryptionOid);
                    innerWriter.Write((byte)0x05); // NULL
                    EncodeLength(innerWriter, 0);
                    innerWriter.Write((byte)0x03); // BIT STRING
                    using (var bitStringStream = new MemoryStream())
                    {
                        var bitStringWriter = new BinaryWriter(bitStringStream);
                        bitStringWriter.Write((byte)0x00); // # of unused bits
                        bitStringWriter.Write((byte)0x30); // SEQUENCE
                        using (var paramsStream = new MemoryStream())
                        {
                            var paramsWriter = new BinaryWriter(paramsStream);
                            EncodeIntegerBigEndian(paramsWriter, parameters.Modulus); // Modulus
                            EncodeIntegerBigEndian(paramsWriter, parameters.Exponent); // Exponent
                            var paramsLength = (int)paramsStream.Length;
                            EncodeLength(bitStringWriter, paramsLength);
                            bitStringWriter.Write(paramsStream.GetBuffer(), 0, paramsLength);
                        }
                        var bitStringLength = (int)bitStringStream.Length;
                        EncodeLength(innerWriter, bitStringLength);
                        innerWriter.Write(bitStringStream.GetBuffer(), 0, bitStringLength);
                    }
                    var length = (int)innerStream.Length;
                    EncodeLength(writer, length);
                    writer.Write(innerStream.GetBuffer(), 0, length);
                }

                var base64 = Convert.ToBase64String(stream.GetBuffer(), 0, (int)stream.Length).ToCharArray();
                // WriteLine terminates with \r\n, we want only \n
                outputStream.Write("-----BEGIN PUBLIC KEY-----\n");
                for (var i = 0; i < base64.Length; i += 64)
                {
                    outputStream.Write(base64, i, Math.Min(64, base64.Length - i));
                    outputStream.Write("\n");
                }
                outputStream.Write("-----END PUBLIC KEY-----");
            }

            return outputStream.ToString();
        }

        /// <summary>
        /// https://stackoverflow.com/a/23739932/2860309
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="length"></param>
        private static void EncodeLength(BinaryWriter stream, int length)
        {
            if (length < 0) throw new ArgumentOutOfRangeException("length", "Length must be non-negative");
            if (length < 0x80)
            {
                // Short form
                stream.Write((byte)length);
            }
            else
            {
                // Long form
                var temp = length;
                var bytesRequired = 0;
                while (temp > 0)
                {
                    temp >>= 8;
                    bytesRequired++;
                }
                stream.Write((byte)(bytesRequired | 0x80));
                for (var i = bytesRequired - 1; i >= 0; i--)
                {
                    stream.Write((byte)(length >> (8 * i) & 0xff));
                }
            }
        }

        /// <summary>
        /// https://stackoverflow.com/a/23739932/2860309
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="value"></param>
        /// <param name="forceUnsigned"></param>
        private static void EncodeIntegerBigEndian(BinaryWriter stream, byte[] value, bool forceUnsigned = true)
        {
            stream.Write((byte)0x02); // INTEGER
            var prefixZeros = 0;
            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] != 0) break;
                prefixZeros++;
            }
            if (value.Length - prefixZeros == 0)
            {
                EncodeLength(stream, 1);
                stream.Write((byte)0);
            }
            else
            {
                if (forceUnsigned && value[prefixZeros] > 0x7f)
                {
                    // Add a prefix zero to force unsigned if the MSB is 1
                    EncodeLength(stream, value.Length - prefixZeros + 1);
                    stream.Write((byte)0);
                }
                else
                {
                    EncodeLength(stream, value.Length - prefixZeros);
                }
                for (var i = prefixZeros; i < value.Length; i++)
                {
                    stream.Write(value[i]);
                }
            }
        }
        public static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");
            byte[] encrypted;

            // Create an AesManaged object
            // with the specified key and IV.
            using (AesManaged aesAlg = new AesManaged())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create an encryptor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            // Return the encrypted bytes from the memory stream.
            return encrypted;
        }

        public static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");

            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            // Create an AesManaged object
            // with the specified key and IV.
            using (AesManaged aesAlg = new AesManaged())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create a decryptor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }

            return plaintext;
        }
        const int MAC_LEN = 16;
        //    //The Key and Nonce are randomly generated
        //    AeadParameters parameters = new AeadParameters(key, 16 * 8, nonce);

        public static string TestEncryptDecrypt()
        {
            string PlainText = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            byte[] Key = new byte[] { 1, 2, 3, 1, 2, 3, 1, 2, 3, 4, 4, 4, 1, 2, 3, 4 };
            byte[] Nonce = new byte[] { 1, 2, 3, 1, 2, 3, 1, 2, 3, 4, 4, 4, 1, 2, 3, 4 };
            AeadParameters parameters = new AeadParameters(new KeyParameter(Key), MAC_LEN * 8, Nonce);

            KeyParameter sessKey = new KeyParameter(Key);
            EaxBlockCipher encCipher = new EaxBlockCipher(new AesEngine());
            EaxBlockCipher decCipher = new EaxBlockCipher(new AesEngine());

            encCipher.Init(true, parameters);
            byte[] input = Encoding.Default.GetBytes(PlainText);
            byte[] encData = new byte[encCipher.GetOutputSize(input.Length)];
            int outOff = encCipher.ProcessBytes(input, 0, input.Length, encData, 0);
            outOff += encCipher.DoFinal(encData, outOff);

            decCipher.Init(false, parameters);
            byte[] decData = new byte[decCipher.GetOutputSize(outOff)];
            int resultLen = decCipher.ProcessBytes(encData, 0, outOff, decData, 0);
            resultLen += decCipher.DoFinal(decData, resultLen);
            return Encoding.Default.GetString(decData);
        }
        public static void Test(RSACryptoServiceProvider RSAServer, string PlainText, byte[] Key, byte[] Nonce)
        {
            //string PlainText = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            //byte[] Key = new byte[] { 1, 2, 3, 1, 2, 3, 1, 2, 3, 4, 4, 4, 1, 2, 3, 4 };
            //byte[] Nonce = new byte[] { 1, 2, 3, 1, 2, 3, 1, 2, 3, 4, 4, 4, 1, 2, 3, 4 };

            byte[] encrypted = Encrypt(RSAServer, PlainText, Key, Nonce);
            var plain = Decrypt(encrypted, Key, Nonce);
        }
        public static byte[] Encrypt(RSACryptoServiceProvider RSAServer, string PlainText, byte[] Key, byte[] Nonce)
        {
            AeadParameters parameters = new AeadParameters(new KeyParameter(Key), MAC_LEN * 8, Nonce);

            EaxBlockCipher encCipher = new EaxBlockCipher(new AesEngine());

            encCipher.Init(true, parameters);
            byte[] input = Encoding.Default.GetBytes(PlainText);
            byte[] encData = new byte[encCipher.GetOutputSize(input.Length)];
            int outOff = encCipher.ProcessBytes(input, 0, input.Length, encData, 0);
            outOff += encCipher.DoFinal(encData, outOff);

            //var tempFile = File.Open("tmp.tmp", FileMode.OpenOrCreate);
            var enc_session_key = (new RSACryptoServiceProvider()).Encrypt(Key, RSAEncryptionPadding.Pkcs1);
            var buffer = new byte[enc_session_key.Length + 16 + 16 + encData.Length];
            buffer = enc_session_key.Concat(Nonce).Concat(Nonce).Concat(encData).ToArray();

            //enc_session_key.ToList().ForEach(x=> tempFile.WriteByte(x));
            //Nonce.ToList().ForEach(x=> tempFile.WriteByte(x));
            //Nonce.ToList().ForEach(x=> tempFile.WriteByte(x));
            //encData.ToList().ForEach(x=> tempFile.WriteByte(x));
            //tempFile.Close();

            return buffer;
        }
        public static string Decrypt(byte[] Encrypted, byte[] Key, byte[] Nonce)
        {
            AeadParameters parameters = new AeadParameters(new KeyParameter(Key), MAC_LEN * 8, Nonce);

            EaxBlockCipher decCipher = new EaxBlockCipher(new AesEngine());

            decCipher.Init(false, parameters);
            int outOff = Encrypted.Length;
            byte[] decData = new byte[decCipher.GetOutputSize(outOff)];
            int resultLen = decCipher.ProcessBytes(Encrypted, 0, outOff, decData, 0);
            resultLen += decCipher.DoFinal(decData, resultLen);
            return Encoding.Default.GetString(decData);
        }
        public static void TestCanEncryptWithAesAnd256BitKey()
        {
            var hexString = "305B624727B235489A72B42F01564ED0CDF46230316EE74B2BB88170D08382C2";
            var key = StringToByteArray(hexString);

            var plainText = "The whole problem with the world is that fools and fanatics are always so certain of themselves, and wiser people so full of doubts - Bertrand Russell (1872-1970)";
            var plainTextBytes = Encoding.ASCII.GetBytes(plainText);
            Console.WriteLine($"Plain Text =>{plainText} (Number of bytes: {plainTextBytes.Length})");
            var associatedText = "My Associated Text";  //Random text that can be used in encryption
            var associatedTextBytes = Encoding.ASCII.GetBytes(associatedText);
            var useAssociatedText = true;

            var encryptCipher = new GcmBlockCipher(new AesFastEngine());
            var encryptKeyParameter = new KeyParameter(key);
            if (useAssociatedText)
            {
                encryptCipher.Init(true, new AeadParameters(encryptKeyParameter, 128, new byte[12], associatedTextBytes));
            }
            else
            {
                encryptCipher.Init(true, new AeadParameters(encryptKeyParameter, 128, new byte[12]));
            }

            var expectedNumberOfOutputBytes = (plainTextBytes.Length / 16) * 16;
            byte[] firstEncryptedBytes = new byte[expectedNumberOfOutputBytes];
            var numberOfOutputBytes = encryptCipher.ProcessBytes(plainTextBytes, 0, plainTextBytes.Length, firstEncryptedBytes, 0);
            //Assert.AreEqual(expectedNumberOfOutputBytes, numberOfOutputBytes);

            expectedNumberOfOutputBytes = (plainTextBytes.Length % 16) + 16;
            byte[] lastEncryptedBytes = new byte[expectedNumberOfOutputBytes];
            numberOfOutputBytes = encryptCipher.DoFinal(lastEncryptedBytes, 0); //Compute the MAC tag
            //Assert.AreEqual(expectedNumberOfOutputBytes, numberOfOutputBytes);

            // Concatentate byte array to get all encrypted bytes with tag
            byte[] totalEncryptedBytes = new byte[plainTextBytes.Length + 16];
            //Assert.AreEqual(totalEncryptedBytes.Length, firstEncryptedBytes.Length + lastEncryptedBytes.Length);

            Buffer.BlockCopy(firstEncryptedBytes, 0, totalEncryptedBytes, 0, firstEncryptedBytes.Length);
            Buffer.BlockCopy(lastEncryptedBytes, 0, totalEncryptedBytes, firstEncryptedBytes.Length, lastEncryptedBytes.Length);

            DumpBytes(plainTextBytes, nameof(plainTextBytes));
            DumpBytes(firstEncryptedBytes, nameof(firstEncryptedBytes));
            DumpBytes(lastEncryptedBytes, nameof(lastEncryptedBytes));
            DumpBytes(totalEncryptedBytes, nameof(totalEncryptedBytes));

            // ********* Copy the encrypted bytes to buffer (strictly speaking:not needed) ***********
            var bytesToDecrypt = new byte[totalEncryptedBytes.Length];
            Buffer.BlockCopy(totalEncryptedBytes, 0, bytesToDecrypt, 0, bytesToDecrypt.Length);

            // Tampering code that messes up the tag itself
            //bytesToDecrypt[bytesToDecrypt.Length - 2] = 117;
            // Tampering code that changes the first byte in the message
            //bytesToDecrypt[0] = 117;
            // Tampering with associated text
            //associatedTextBytes = Encoding.ASCII.GetBytes(associatedText + "Hacked!");

            // ********* Decryption ***********
            var decryptCipher = new GcmBlockCipher(new AesFastEngine());
            var decryptKeyParameter = new KeyParameter(key);
            if (useAssociatedText)
            {
                decryptCipher.Init(false, new AeadParameters(encryptKeyParameter, 128, new byte[12], associatedTextBytes));
            }
            else
            {
                decryptCipher.Init(false, new AeadParameters(encryptKeyParameter, 128, new byte[12]));
            }

            var lengthWithoutTag = bytesToDecrypt.Length - 16;
            expectedNumberOfOutputBytes = (lengthWithoutTag / 16) * 16;
            byte[] firstDecryptedBytes = new byte[expectedNumberOfOutputBytes];
            numberOfOutputBytes = decryptCipher.ProcessBytes(bytesToDecrypt, 0, bytesToDecrypt.Length, firstDecryptedBytes, 0);
            //Assert.AreEqual(expectedNumberOfOutputBytes, numberOfOutputBytes);
            //Console.WriteLine($"Decrypted Text =>{Encoding.ASCII.GetString(outputBytes)} (Number of bytes: {outputBytes.Length})");

            expectedNumberOfOutputBytes = (bytesToDecrypt.Length % 16);
            var lastDecryptedBytes = new byte[expectedNumberOfOutputBytes];
            numberOfOutputBytes = decryptCipher.DoFinal(lastDecryptedBytes, 0);  //Validate the MAC tag
            //Assert.AreEqual(expectedNumberOfOutputBytes, numberOfOutputBytes);

            // Concatentate byte array to get all decrypted bytes
            byte[] totalDecryptedBytes = new byte[lengthWithoutTag];
            //Assert.AreEqual(totalDecryptedBytes.Length, firstDecryptedBytes.Length + lastDecryptedBytes.Length);

            Buffer.BlockCopy(firstDecryptedBytes, 0, totalDecryptedBytes, 0, firstDecryptedBytes.Length);
            Buffer.BlockCopy(lastDecryptedBytes, 0, totalDecryptedBytes, firstDecryptedBytes.Length, lastDecryptedBytes.Length);

            //DumpBytes(bytesToDecrypt, nameof(bytesToDecrypt));
            //DumpBytes(decryptedBytes, nameof(decryptedBytes));
            //DumpBytes(finalDecryptedBlockOutputBytes, nameof(finalDecryptedBlockOutputBytes));
            //DumpBytes(totalDecryptedOutputBytes, nameof(totalDecryptedOutputBytes));

            Console.WriteLine($"{plainText} (Number of bytes: {plainTextBytes.Length}) (Before Encryption)");
            Console.WriteLine($"{Encoding.ASCII.GetString(totalEncryptedBytes)} (Number of bytes: {totalEncryptedBytes.Length})  (After Encryption)");
            Console.WriteLine($"{Encoding.ASCII.GetString(totalDecryptedBytes)} (Number of bytes: {totalDecryptedBytes.Length})  (After Decryption)");

            //Assert.AreEqual(plainTextBytes.Length, totalDecryptedBytes.Length);
            //Assert.AreEqual(plainText, Encoding.ASCII.GetString(totalDecryptedBytes));
        }

        public static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);

            DumpBytes(bytes);
            return bytes;
        }

        public static void DumpBytes(byte[] bytes, string name = null)
        {
            if (name != null)
            {
                Console.WriteLine($"{name}");
            }

            for (int i = 0; i < bytes.Length; i++)
            {
                Console.WriteLine($"[{i}]: {bytes[i]}");
            }

            var hexString = BitConverter.ToString(bytes).Replace("-", string.Empty);
            Console.WriteLine($"Hex format: {hexString}");
            Console.WriteLine($"byte array length: {bytes.Length} ({name})");
        }
    }
}
