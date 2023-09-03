using FDS.DTO.Responses;
using FDS.Logging;
using FDS.Services.Interface;
using Shell32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

namespace FDS.Services
{
    public class TrashDataProtection : IService,ILogger
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, RecycleFlags dwFlags);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        static extern int SHQueryRecycleBin(string pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

        [Flags]
        enum RecycleFlags : uint
        {
            SHERB_NOCONFIRMATION = 0x00000001,
            SHERB_NOPROGRESSUI = 0x00000001,
            SHERB_NOSOUND = 0x00000004
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        struct SHQUERYRBINFO
        {
            public uint cbSize;
            public ulong i64Size;
            public ulong i64NumItems;
        }


        public bool RunService(SubservicesData subservices, string serviceTypeDetails)
        {

             
            try
            {


                ulong initialFileCount = GetDeletedFileCount();

                int result = SHEmptyRecycleBin(IntPtr.Zero, null, RecycleFlags.SHERB_NOCONFIRMATION | RecycleFlags.SHERB_NOPROGRESSUI | RecycleFlags.SHERB_NOSOUND);

                if (result == 0)
                {
                    LogInformation(subservices.Sub_service_authorization_code, subservices.Sub_service_name,Convert.ToInt32(initialFileCount), Convert.ToString(subservices.Id), subservices.Execute_now, serviceTypeDetails);
                }
                else
                {
                    Console.WriteLine("Failed to empty Recycle Bin. Error code: " + result);
                }

                //Shell32.Shell shell = new Shell32.Shell();

                ////Shell shell = new Shell();

                //Folder recycleBin = shell.NameSpace(10);

                //foreach (FolderItem2 item in recycleBin.Items())
                //{
                //    count++;
                //}

                ////SHEmptyRecycleBin(IntPtr.Zero, null, RecycleFlag.SHERB_NOCONFIRMATION | RecycleFlag.SHERB_NOPROGRESSUI | RecycleFlag.SHERB_NOSOUND);

                //KillCmd();

               

            }
            catch (Exception exp)
            {
                exp.ToString();
            }
            

            return true;
        }


        public static ulong GetDeletedFileCount()
        {
            SHQUERYRBINFO rbInfo = new SHQUERYRBINFO
            {
                cbSize = (uint)Marshal.SizeOf(typeof(SHQUERYRBINFO))
            };

            int result = SHQueryRecycleBin(null, ref rbInfo);

            if (result == 0)
            {
                return rbInfo.i64NumItems;
            }
            else
            {
                return 0;
            }
        }

        public void LogInformation(string authorizationCode, string subServiceName, long FileProcessed, string ServiceId, bool IsManualExecution, string serviceTypeDetails)
        {
            DatabaseLogger databaseLogger = new DatabaseLogger();
            databaseLogger.LogInformation(authorizationCode, subServiceName, FileProcessed, ServiceId, IsManualExecution, serviceTypeDetails);
        }


        public void KillCmd()
        {
            Array.ForEach(Process.GetProcessesByName("cmd"), x => x.Kill());
        }
    }
}
