using FDS.Common;
using FDS.DTO.Responses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Usb;

namespace FDS.SingleTon
{
    public class CheckKeysSingleTon
    {
        private static CheckKeysSingleTon _instance;
        private RSACryptoServiceProvider _rsaserver;
        private RSACryptoServiceProvider _rsaDevice;
        public QRCodeResponse QRCodeResponse { get; private set; }

        private static readonly object _lock = new object();

        private CheckKeysSingleTon() { }

        public static CheckKeysSingleTon Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new CheckKeysSingleTon();
                            _instance.Initialize();
                        }
                    }
                }
                return _instance;
            }
        }

        public void ConfigDataClear()
        {
            ConfigDetails.Key1 = string.Empty;
            ConfigDetails.Key2 = string.Empty;
            ConfigDetails.Authentication_token = string.Empty;
            ConfigDetails.Authorization_token = string.Empty;
            ConfigDetails.Modulus = string.Empty;
            ConfigDetails.Exponent = string.Empty;
            ConfigDetails.D = string.Empty;
            ConfigDetails.DP = string.Empty;
            ConfigDetails.DQ = string.Empty;
            ConfigDetails.Q = string.Empty;
            ConfigDetails.InverseQ = string.Empty;
        }

        private void Initialize()
        {

            
            //string basePathEncryption = String.Format("{0}Tempfolder", AppDomain.CurrentDomain.BaseDirectory);
            //string encryptOutPutFile = @"\Main";
            //encryptOutPutFile = basePathEncryption + @"\Main";
            //ConfigDataClear();
            //if (File.Exists(encryptOutPutFile))
            //{
            //    string finalOutPutFile = basePathEncryption + @"\FinalDecrypt";
            //    Common.EncryptionDecryption.DecryptFile(encryptOutPutFile, finalOutPutFile);
            //    Common.EncryptionDecryption.ReadDecryptFile(finalOutPutFile);
            //}

            _rsaDevice = new RSACryptoServiceProvider(2048);
            _rsaserver = CreateRSACryptoServiceProvider();            
            _rsaDevice.ImportParameters(_rsaserver.ExportParameters(false));
        }

        public RSACryptoServiceProvider RSAServer
        {
            get { return _rsaserver; }
        }

        public RSACryptoServiceProvider RSADevice
        {
            get { return _rsaDevice; }
        }


        private RSACryptoServiceProvider CreateRSACryptoServiceProvider()
        {
            RSACryptoServiceProvider RSAServer = null;
            try
            {
                RSAParameters RSAParam;

                
                RSAParam = _rsaDevice.ExportParameters(true);

                string basePathEncryption = String.Format("{0}Tempfolder", AppDomain.CurrentDomain.BaseDirectory);
                string filePath = Path.Combine(basePathEncryption, "Main");

                if (!File.Exists(filePath))
                {
                    return RSAServer;
                }

                RSAParam = new RSAParameters
                {
                    InverseQ = Convert.FromBase64String(String.IsNullOrEmpty(ConfigDetails.InverseQ) ? string.Empty : ConfigDetails.InverseQ),
                    DQ = Convert.FromBase64String(String.IsNullOrEmpty(ConfigDetails.DQ) ? string.Empty : ConfigDetails.DQ),
                    DP = Convert.FromBase64String(String.IsNullOrEmpty(ConfigDetails.DP) ? string.Empty : ConfigDetails.DP),
                    Q = Convert.FromBase64String(String.IsNullOrEmpty(ConfigDetails.Q) ? string.Empty : ConfigDetails.Q),
                    P = Convert.FromBase64String(String.IsNullOrEmpty(ConfigDetails.P) ? string.Empty : ConfigDetails.P),
                    D = Convert.FromBase64String(String.IsNullOrEmpty(ConfigDetails.D) ? string.Empty : ConfigDetails.D),
                    Exponent = Convert.FromBase64String(String.IsNullOrEmpty(ConfigDetails.Exponent) ? string.Empty : ConfigDetails.Exponent),
                    Modulus = Convert.FromBase64String(String.IsNullOrEmpty(ConfigDetails.Modulus) ? string.Empty : ConfigDetails.Modulus),
                };


                _rsaDevice.ImportParameters(RSAParam);

                var key1 = String.IsNullOrEmpty(ConfigDetails.Key1) ? string.Empty : ConfigDetails.Key1;
                var key2 = String.IsNullOrEmpty(ConfigDetails.Key2) ? string.Empty : ConfigDetails.Key2;
                var Authentication_token = String.IsNullOrEmpty(ConfigDetails.Authentication_token) ? string.Empty : ConfigDetails.Authentication_token;
                var Authorization_token = String.IsNullOrEmpty(ConfigDetails.Authorization_token) ? string.Empty : ConfigDetails.Authorization_token;


                bool ValidServerKey = !string.IsNullOrEmpty(key1) && !string.IsNullOrEmpty(key2) && !string.IsNullOrEmpty(Authentication_token) && !string.IsNullOrEmpty(Authorization_token);
                if (!ValidServerKey)
                {
                    return RSAServer;
                }
                QRCodeResponse = new QRCodeResponse
                {
                    Public_key = key1 + key2,
                    Authentication_token = Authentication_token,
                    Authorization_token = Authorization_token
                };

                RSAServer = new RSACryptoServiceProvider(2048);
                RSAServer = RSAKeys.ImportPublicKey(System.Text.ASCIIEncoding.ASCII.GetString(Convert.FromBase64String(QRCodeResponse.Public_key)));

                return RSAServer;
            }
            catch (Exception ex)
            {
                ex.ToString();
                return RSAServer;
            }
        }

    }
}
