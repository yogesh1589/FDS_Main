using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace FDS.Solid.SRP
{
    public class TimerService
    {
        public DispatcherTimer QRGeneratortimer { get; private set; }
        public DispatcherTimer TimerQRCode { get; private set; }
        public DispatcherTimer TimerOTPCode { get; private set; }
        // Add other timers as needed

        public TimerService()
        {
            InitializeTimers();
        }

        private void InitializeTimers()
        {
            QRGeneratortimer = new DispatcherTimer();
            QRGeneratortimer.Interval = TimeSpan.FromMilliseconds(100);
            QRGeneratortimer.Tick += QRGeneratortimer_Tick;

            TimerQRCode = new DispatcherTimer();
            TimerQRCode.Interval = TimeSpan.FromMilliseconds(1000);
            TimerQRCode.Tick += TimerQRCode_Tick;

            TimerOTPCode = new DispatcherTimer();
            TimerOTPCode.Interval = TimeSpan.FromSeconds(1);
            TimerOTPCode.Tick += TimerOTPCode_Tick;

            // Initialize other timers
        }

        private void QRGeneratortimer_Tick(object sender, EventArgs e)
        {
            // Your QRGeneratortimer tick logic
        }

        private void TimerQRCode_Tick(object sender, EventArgs e)
        {
            // Your TimerQRCode tick logic
        }

        private void TimerOTPCode_Tick(object sender, EventArgs e)
        {
            // Your TimerOTPCode tick logic
        }

        // Add other timer tick event handlers as needed
    }
}
