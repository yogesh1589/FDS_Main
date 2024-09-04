using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace FDS.UI
{
    public class TimerManager
    {
        private Dictionary<string, DispatcherTimer> timers = new Dictionary<string, DispatcherTimer>();

        public TimerManager()
        {
            InitializeTimers();
        }

        private void InitializeTimers()
        {
            // Define timers with intervals and handlers
            CreateTimer("OTPCodeTimer", TimeSpan.FromSeconds(30), TimerOTPCode_Tick);
            CreateTimer("NetworkMonitoring", TimeSpan.FromMinutes(1), TimerNetworkMonitoring_Tick);
            CreateTimer("EventBasedService", TimeSpan.FromSeconds(10), TimerEventBasedService_Tick);
            // Add more timers as needed
        }

        private void CreateTimer(string name, TimeSpan interval, EventHandler eventHandler)
        {
            var timer = new DispatcherTimer();
            timer.Interval = interval;
            timer.Tick += eventHandler;
            timers.Add(name, timer);
        }

        public void StartTimer(string name)
        {
            if (timers.ContainsKey(name))
            {
                timers[name].Start();
            }
        }

        public void StopTimer(string name)
        {
            if (timers.ContainsKey(name) && timers[name].IsEnabled)
            {
                timers[name].Stop();
            }
        }

        private void TimerOTPCode_Tick(object sender, EventArgs e)
        {
            // Handle OTP Code Timer Tick
        }

        private void TimerNetworkMonitoring_Tick(object sender, EventArgs e)
        {
            // Handle Network Monitoring Timer Tick
        }

        private void TimerEventBasedService_Tick(object sender, EventArgs e)
        {
            // Handle Event-Based Service Timer Tick
        }
    }
}
