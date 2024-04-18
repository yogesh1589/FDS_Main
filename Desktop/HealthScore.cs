using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDS
{
    public class HealthScore : INotifyPropertyChanged
    {
        private double endAngle;

        public event PropertyChangedEventHandler PropertyChanged;

        public double EndAngle
        {
            get { return endAngle; }
            set
            {
                if (endAngle != value)
                {
                    endAngle = value;
                    OnPropertyChanged(nameof(EndAngle));
                }
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
