using Microsoft.Win32;
using System;

namespace Installer
{
    public class Installer
    {
        public static void RegisterControlPanelProgram()
        {
            try
            {
                string appName = "FDS";
                string installLocation = "C:\\Program Files(x86)\\Fusion Tech Solution\\FDS Setup\\";
                string displayIcon = "C:\\Program Files(x86)\\Fusion Tech Solution\\FDS Setup\\LogoFDSXL.ico";
                string uninstallString = "C:\\Program Files(x86)\\Fusion Tech Solution\\FDS Setup\\Desktop.exe";
                string Install_Reg_Loc = "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall";

                RegistryKey hKey = (Registry.LocalMachine).OpenSubKey(Install_Reg_Loc, true);

                RegistryKey appKey = hKey.CreateSubKey(appName);

                appKey.SetValue("DisplayName", (object)appName, RegistryValueKind.String);

                appKey.SetValue("Publisher", (object)"Fusion Tech Solution", RegistryValueKind.String);

                appKey.SetValue("InstallLocation",
                         (object)installLocation, RegistryValueKind.ExpandString);

                appKey.SetValue("DisplayIcon", (object)displayIcon, RegistryValueKind.String);

                appKey.SetValue("UninstallString",
                       (object)uninstallString, RegistryValueKind.ExpandString);

                appKey.SetValue("DisplayVersion", (object)"v1.0", RegistryValueKind.String);
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}
