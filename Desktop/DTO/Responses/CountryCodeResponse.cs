using FDS.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FDS.DTO.Responses
{
    public class CountryCodeResponse
    {
        public List<CountryCode> data { get; set; }
    }
    public class CountryCode
    {

        public string name { get; set; }
        public string phone_code { get; set; }
        public string flag { get; set; }
        public string country_code { get; set; }
        public string flag_emoji { get; set; }

        public string Phone_code
        {
            get { return phone_code; }
            set
            {
                phone_code = value;
                OnPropertyChanged();
            }
        }
        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                OnPropertyChanged();
            }
        }

        public string DisplayText => $"{Phone_code} - {country_code} - {Name}";

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ViewModel : INotifyPropertyChanged
    {
        public List<CountryCode> AllCountries { get; set; }

        private string selectedCountryCode;
        public string SelectedCountryCode
        {
            get { return selectedCountryCode; }
            set
            {
                selectedCountryCode = value;
                OnPropertyChanged(nameof(SelectedCountryCode));
            }
        }

         

        //public ViewModel()
        //{
        //    // Populate the list of countries
        //    AllCountries = new List<CountryCode>
        //    {
        //        new CountryCode { Name = "IN", phone_code = "India" },
        //        new CountryCode { Name = "US", phone_code = "United States" },
        //        new CountryCode { Name = "GB", phone_code = "United Kingdom" },
        //        new CountryCode { Name = "JP", phone_code = "Japan" }
        //        // Add more countries as needed
        //    };
        //}

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
