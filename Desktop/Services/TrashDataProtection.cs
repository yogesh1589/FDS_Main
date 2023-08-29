using FDS.DTO.Responses;
using FDS.Logging;
using FDS.Services.AbstractClass;
using Shell32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FDS.Services
{
    public class TrashDataProtection : BaseService
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, RecycleFlag dwFlags);

        enum RecycleFlag : int
        {
            SHERB_NOCONFIRMATION = 0x00000001,
            SHERB_NOPROGRESSUI = 0x00000001,
            SHERB_NOSOUND = 0x00000004
        }
        public TrashDataProtection(ILogger logger) : base(logger)
        {
        }

        public override void RunService(SubservicesData subservices)
        {

            try
            {
                int count = 0;

                Shell shell = new Shell();

                Folder recycleBin = shell.NameSpace(10);

                foreach (FolderItem2 item in recycleBin.Items())
                {
                    count++;
                }

                SHEmptyRecycleBin(IntPtr.Zero, null, RecycleFlag.SHERB_NOCONFIRMATION | RecycleFlag.SHERB_NOPROGRESSUI | RecycleFlag.SHERB_NOSOUND);

                KillCmd();

                LogServicesData(subservices.Sub_service_authorization_code, subservices.Sub_service_name, 0, Convert.ToString(subservices.Id), subservices.Execute_now);

            }
            catch (Exception exp)
            {
                exp.ToString();
            }
        }
    }
}
