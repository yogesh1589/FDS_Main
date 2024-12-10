using FDS.API_Service;
using FDS.DTO.Responses;
using Newtonsoft.Json;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tunnel;

namespace FDS.Common
{
    public class VPNServiceCom
    {
        
        bool connectedVPN = false;
        string configFileVPN = String.Format("{0}wg0.conf", AppDomain.CurrentDomain.BaseDirectory);

        public async Task<bool> ConnectVPN()
        {

            try
            {
                VPNService vpnService = new VPNService();

                var apiResponse = await vpnService.VPNConnectAsync();
                if (apiResponse != null)
                {

                    var plainText = EncryptDecryptData.RetriveDecrypt(apiResponse.payload);

                    string cleanJson = Regex.Replace(plainText, @"[^\x20-\x7E]+", "");

                    var finalData = JsonConvert.DeserializeObject<VPNResponseNew>(cleanJson);

                    var configData = finalData.Data.Config.ToString();


                    if (File.Exists(configFileVPN))
                    {
                        File.Delete(configFileVPN);
                    }
                    File.WriteAllText(configFileVPN, configData);


                    await WriteAllBytesAsync(configFileVPN, Encoding.UTF8.GetBytes(configData));
                    Tunnel.Service.Run(configFileVPN);
                    Tunnel.Service.Add(configFileVPN, false);

                    connectedVPN = true;
                    return true;
                }
                else
                {
                    connectedVPN = false;
                    return false;
                }
            }
            catch (Exception ex)
            {               
                try { File.Delete(configFileVPN); } catch { }
            }
            return false;
        }





        private const string PipeName = "AdminTaskPipe";
        public bool SendRequest(string request)
        {
            try
            {
                
                if (!connectedVPN)
                {
                    connectedVPN = true;
                    return SendRequestNamedPipe(request);

                }
                else
                {
                    connectedVPN = false;
                    return SendRequestNamedPipe(request);
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private static readonly byte[] Key = Encoding.UTF8.GetBytes("ThisIsASecretKeyForAES256Example!");
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("InitializationVec"); // 16 bytes for AES


        public bool SendRequestNamedPipe(string request)
        {


            //string encryptedMessage = Encrypt(request);

             

            try
            {
                using (var client = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut))
                {
                    client.Connect(5000); // Timeout in milliseconds

                    var writer = new StreamWriter(client) { AutoFlush = true };
                    var reader = new StreamReader(client);

                    try
                    {

                        writer.WriteLine(request);
                        string response = reader.ReadLine();
                        return true; // Indicate success
                    }
                    catch (IOException ioEx)
                    {
                        // Handle IO exceptions
                        Console.WriteLine("IOException: " + ioEx.Message);
                        return false; // Indicate failure
                    }
                    catch (ObjectDisposedException disposedEx)
                    {
                        // Check if the exception message is "Cannot access a closed pipe."
                        if (disposedEx.Message.Contains("Cannot access a closed pipe."))
                        {
                            return true; // Return true if the message matches
                        }
                        else
                        {
                            Console.WriteLine("ObjectDisposedException: " + disposedEx.Message);
                            return false; // Return false for other messages
                        }
                    }
                    catch (Exception ex)
                    {
                        // Handle other exceptions
                        Console.WriteLine("Exception: " + ex.Message);
                        return false; // Indicate failure
                    }
                    finally
                    {
                        // Dispose of the writer and reader explicitly
                        writer.Dispose();
                        reader.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Pipe is broken."))
                {
                    return true; // Return true if the message matches
                }
                // Handle connection exceptions
                Console.WriteLine("Connection Exception: " + ex.Message);
                return false; // Indicate failure
            }



        }

        public async Task WriteAllBytesAsync(string filePath, byte[] bytes)
        {
            using (FileStream sourceStream = new FileStream(filePath,
                FileMode.Create, FileAccess.Write, FileShare.None,
                bufferSize: 4096, useAsync: true))
            {
                await sourceStream.WriteAsync(bytes, 0, bytes.Length);
            }
        }


        public async Task<string> GetIPConfig(string configData)
        {
            string publicIP = string.Empty;
            string pattern = @"Endpoint = (\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})";
            Match match = Regex.Match(configData, pattern);

            if (match.Success)
            {
                string ip = match.Groups[1].Value;
                publicIP = ip;
                
            }
            return publicIP;
         
        }

        public async Task ReadConfigFileAsync(string filePath)
        {
            try
            {
                // Read the entire file content asynchronously
                string configData = File.ReadAllText(filePath);
                await GetIPConfig(configData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading file: {ex.Message}");
            }
        }


 

        public static string Encrypt(string plainText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        using (StreamWriter writer = new StreamWriter(cs))
                        {
                            writer.Write(plainText);
                        }
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

      
    }
}
