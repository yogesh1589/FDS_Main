using FDS.DTO.Responses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Tunnel;

namespace FDS.Common
{
    public class VPNServiceCom
    {
        public static VPNResponse ParseWireGuardConfiguration(string configString)
        {
            VPNResponse config = new VPNResponse();
            InterfaceConfiguration interfaceConfig = new InterfaceConfiguration();
            PeerConfiguration peerConfig = new PeerConfiguration();
            var lines = configString.Split('\n');
            string currentSection = "";

            foreach (var line in lines)
            {
                if (line.Trim().StartsWith("[") && line.Trim().EndsWith("]"))
                {
                    currentSection = line.Trim();
                    continue;
                }

                var parts = line.Trim().Split('=');
                if (parts.Length != 2)
                {
                    // Check if there's an equal sign in the value part
                    parts = line.Trim().Split(new char[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 2)
                        continue;
                }

                var key = parts[0].Trim();
                var value = parts[1].Trim();

                if (currentSection == "[Interface]")
                {
                    switch (key)
                    {
                        case "PrivateKey":
                            interfaceConfig.PrivateKey = value;
                            break;
                        case "Address":
                            interfaceConfig.Address = value;
                            break;
                        case "DNS":
                            interfaceConfig.DNS = new List<string>(value.Split(','));
                            break;
                        default:
                            break;
                    }
                }
                else if (currentSection == "[Peer]")
                {
                    switch (key)
                    {
                        case "PublicKey":
                            peerConfig.PublicKey = value;
                            break;
                        case "Endpoint":
                            peerConfig.Endpoint = value;
                            break;
                        case "AllowedIPs":
                            peerConfig.AllowedIPs = value;
                            break;
                        default:
                            break;
                    }
                }
            }

            config.Interface = interfaceConfig;
            config.Peer = peerConfig;
            return config;
        }

        public static async Task<string> generateNewConfig()
        {
            return string.Format("[Interface]\nPrivateKey = cF9Vwgt4OXR6YZdocIzXFZa3XlgpUaa3/lUPuVsPwVU=\nAddress = 10.66.66.2/32,fd42:42:42::2/128\nDNS = 1.1.1.1,1.0.0.1\n\n[Peer]\nPublicKey = eEhNt9rYKAfqHSJx1C0HEw7GbhpHjofsWFVxjlr+tCY=\nPresharedKey = W2O7L+SawuUnrqjlkb5L82wiW5W080VUnLM12NRs5cw=\nEndpoint = 3.129.250.218:51280\nAllowedIPs = 0.0.0.0/0,::/0\n");
        }

        public static async Task<string> generateNewConfig2(string privateKey, string serverPubkey2,string ipAddress)
        {

            var keys = Keypair.Generate();
            var client = new TcpClient();
            string[] myIP = ipAddress.Trim().Split(':');
            await client.ConnectAsync("3.129.250.218", 51280);
            var stream = client.GetStream();
            var reader = new StreamReader(stream, Encoding.UTF8);
            var pubKeyBytes = Encoding.UTF8.GetBytes(keys.Public + "\n");
            await stream.WriteAsync(pubKeyBytes, 0, pubKeyBytes.Length);
            await stream.FlushAsync();
            var ret = (await reader.ReadLineAsync()).Split(':');
            client.Close();
            var status = ret.Length >= 1 ? ret[0] : "";
            var serverPubkey = ret.Length >= 2 ? ret[1] : "";
            var serverPort = ret.Length >= 3 ? ret[2] : "";
            var internalIP = ret.Length >= 4 ? ret[3] : "";
            if (status != "OK")
                throw new InvalidOperationException(string.Format("Server status is {0}", status));
            return string.Format("[Interface]\nPrivateKey = {0}\nAddress = {1}\nDNS = 1.1.1.1,1.0.0.1\n\n[Peer]\nPublicKey = {2}\nEndpoint = {3}\nAllowedIPs = 0.0.0.0/0,::/0\n", privateKey, internalIP, serverPubkey2, ipAddress);
        }
    }
}
