using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Configuration.Install;
//using Desktop;


namespace FDSService
{
    [System.ComponentModel.RunInstaller(true)]
    public partial class Service1 : ServiceBase
    {
        //private System.Timers.Timer timer;

        public Service1()
        {
            ServiceName = "FDS";
        }

        protected override void OnStart(string[] args)
        {
            // Start the timer to execute the service logic periodically
            //timer = new System.Timers.Timer();
            //timer.Interval = 60000; // Execute the service logic every 60 seconds
            //timer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimerElapsed);
            //timer.Enabled = true;
            base.OnStart(args);
            //var obj = new FDSMain();
            //obj.CheckDeviceHealthFromService();
        }

        protected override void OnStop()
        {
            // Stop the timer when the service is stopped
            //timer.Enabled = false;
            //timer.Dispose();
            base.OnStop();
        }

        //private void OnTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        //{
        //    // Execute the service logic here
        //}

        //public static void Main()
        //{
        //    ServiceBase.Run(new Service1());
        //}
    }
}
