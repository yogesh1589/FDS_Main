using FDS.API_Service;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FDS.DTO.Responses
{
    public class LicenseResponse : INotifyPropertyChanged
    {
        private string _licenseText;
        private ApiService apiService;
        public DeviceResponse DeviceResponse { get; private set; }

        public LicenseResponse()
        {
            apiService = new ApiService(); // Initialize your ApiService
             
        }

        public async Task<DeviceResponse> LoadData(string vals)
        {
            // Assuming GenerateQRCodeAsync() returns a string representing the device response
            try
            {
                LicenseText = vals;
                DeviceResponse = await apiService.GenerateQRCodeLicenseAsync(LicenseText);
                if ((DeviceResponse.httpStatusCode == HttpStatusCode.OK) || (DeviceResponse.Success = true))
                {
                    var QRCodeResponse = await apiService.CheckLicenseAsync(DeviceResponse.qr_code_token, LicenseText.Trim());

                    if ((QRCodeResponse.HttpStatusCode == HttpStatusCode.OK) || (QRCodeResponse.Success))
                    {
                        return DeviceResponse;
                        ;
                        //txtlicenseTokenValidation.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        //txtlicenseTokenValidation.Text = "Invalid license key !";
                        //txtlicenseTokenValidation.Visibility = Visibility.Visible;
                        return DeviceResponse;
                    }

                }

            }
            catch (Exception ex)
            {
                // Handle exception or error from API call
                Console.WriteLine("Error fetching data: " + ex.Message);
            }
            return DeviceResponse;
        }

        public string LicenseText
        {
            get { return _licenseText; }
            set
            {
                _licenseText = value;
                OnPropertyChanged(nameof(LicenseText)); // Not shown here; raises PropertyChanged event
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
