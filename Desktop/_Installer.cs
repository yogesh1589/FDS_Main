using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Threading.Tasks;

namespace Desktop
{
    [RunInstaller(true)]
    public partial class _Installer : System.Configuration.Install.Installer
    {
        public _Installer()
        {
            InitializeComponent();
        }
        public override void Uninstall(System.Collections.IDictionary savedState)
        {
            base.Uninstall(savedState);
            System.Diagnostics.Process.Start(Context.Parameters["TARGETDIR"].ToString() + "application.exe");
            // Very important! Removes all those nasty temp files.
            base.Dispose();
        }
    }
}
