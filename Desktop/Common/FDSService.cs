using FDS.DTO.Responses;
using FDS.WindowService;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDS.Common
{
    public class FDSService
    {
        public string watcherServiceName = "Service_FDS";
        public bool isVPNServiceRunning = false;
        public string vpnServieName = "WireGuardTunnel$wg0";
        string basePathEncryption = String.Format("{0}Tempfolder", AppDomain.CurrentDomain.BaseDirectory);
      

        public void SetShortCut(string LauncherAppPath)
        {
            try
            {

                // Destination path for the shortcut in the Startup folder
                string startupFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup), "FDS.lnk");

                // Check if the shortcut already exists in the Startup folder
                if (!File.Exists(startupFolderPath))
                {
                    // Create a WshShell instance
                    IWshRuntimeLibrary.WshShell wshShell = new IWshRuntimeLibrary.WshShell();

                    // Create a shortcut
                    IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)wshShell.CreateShortcut(startupFolderPath);
                    shortcut.TargetPath = LauncherAppPath;
                    shortcut.Save();

                    Console.WriteLine("Shortcut created in the Startup folder successfully.");
                }
                else
                {
                    Console.WriteLine("Shortcut already exists in the Startup folder.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }


        public void LoadWinService()
        {
            try
            {
                string LauncherAppPath = String.Format("{0}LauncherApp.exe", AppDomain.CurrentDomain.BaseDirectory);
                SetShortCut(LauncherAppPath);
                WindowServiceInstaller windowServiceInstaller = new WindowServiceInstaller();
                windowServiceInstaller.InstallService(watcherServiceName, "WindowServiceFDS.exe");
                windowServiceInstaller.StartService(watcherServiceName);

                if (isVPNServiceRunning)
                {
                    windowServiceInstaller.StartService(vpnServieName);
                }


            }
            catch (Exception ex)
            {
                ex.ToString();
                //  
            }
        }

        public void CheckEncryptFile()
        {
            ConfigDataClear();
            string encryptOutPutFile = basePathEncryption + @"\Main";
            if (File.Exists(encryptOutPutFile))
            {
                string finalOutPutFile = basePathEncryption + @"\FinalDecrypt";
                Common.EncryptionDecryption.DecryptFile(encryptOutPutFile, finalOutPutFile);
                Common.EncryptionDecryption.ReadDecryptFile(finalOutPutFile);
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

    }
}
