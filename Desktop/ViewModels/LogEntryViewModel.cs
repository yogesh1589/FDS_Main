using FDS.API_Service;
using FDS.Common;
using FDS.DTO.Responses;
using FDS.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FDS.ViewModels
{
    public class LogEntryViewModel : INotifyPropertyChanged
    {
        private ApiService apiService = new ApiService();
        private ObservableCollection<LogEntryModel> _logEntries;
        private int _currentPage = 1;
        private const int PageSize = 4; // Change this to adjust how many items you want per page

        public ObservableCollection<LogEntryModel> LogEntries
        {
            get => _logEntries;
            set
            {
                _logEntries = value;
                OnPropertyChanged(nameof(LogEntries));
                OnPropertyChanged(nameof(PagedLogEntries));
            }
        }

        public IEnumerable<LogEntryModel> PagedLogEntries => LogEntries.Skip((CurrentPage - 1) * PageSize).Take(PageSize);

        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                _currentPage = value;
                OnPropertyChanged(nameof(CurrentPage));
                OnPropertyChanged(nameof(PagedLogEntries));
                //OnPropertyChanged(nameof(CanGoToNextPage));
                //OnPropertyChanged(nameof(CanGoToPreviousPage));
            }
        }

        public bool CanGoToNextPage = false;
        public bool CanGoToPreviousPage = false;

        //public bool CanGoToNextPage => (CurrentPage * PageSize) < LogEntries.Count;
        //public bool CanGoToPreviousPage => CurrentPage > 1;

        //public ICommand NextPageCommand { get; }
        //public ICommand PreviousPageCommand { get; }

        public LogEntryViewModel()
        {
            LogEntries = new ObservableCollection<LogEntryModel>();
        }

        public int ServiceID { get; set; }



        public async Task LoadMoreLogEntries(int serviceID, string clickDrag)
        {

            if (clickDrag == "p")
            {
                CurrentPage--;
                if (CurrentPage <= 0)
                {
                    CurrentPage = 1;
                }
                else
                {
                    if (CurrentPage > 1)
                    {
                        CanGoToPreviousPage = true;
                    }
                    else
                    {
                        CanGoToPreviousPage = false;
                    }
                }

            }
            else if (clickDrag == "n")
            {
                CurrentPage++;
                if (CurrentPage > 1)
                {
                    CanGoToPreviousPage = true;
                }
                else
                {
                    CanGoToPreviousPage = false;
                }
            }

            Thread.Sleep(200);
            if (ServiceID != serviceID)
            {
                CurrentPage = 1;
                LogEntries.Clear();
            }

            var newLogEntries = await apiService.GetServiceInfoAsync(serviceID, PageSize, CurrentPage);
            if (newLogEntries != null && newLogEntries.Count > 0)
            {
                readData(newLogEntries);
                if ((newLogEntries.Count < 4) && (CurrentPage == 1))
                {
                    CanGoToNextPage = false;
                    CanGoToPreviousPage = false;
                }
                else if ((newLogEntries.Count < 4) && (CurrentPage > 1))
                {
                    CanGoToNextPage = false;
                    CanGoToPreviousPage = true;
                }
            }
            else
            {
                CurrentPage--;
                var newLogEntries1 = await apiService.GetServiceInfoAsync(serviceID, PageSize, CurrentPage);
                if (newLogEntries1 != null && newLogEntries1.Count > 0)
                { readData(newLogEntries1); }
                CanGoToNextPage = false;
                CanGoToPreviousPage = false;
            }
            ServiceID = serviceID;
            OnPropertyChanged(nameof(PagedLogEntries));



        }



        private void readData(List<LogEntry> newLogEntries)
        {
            CanGoToNextPage = true;
            LogEntries.Clear();
            foreach (var logEntry in newLogEntries)
            {
                var timeFDS = FormatDateTime(logEntry.time);
                if (logEntry.changed_by == "_")
                {
                    logEntry.changed_by = "FDS";
                }


                if (logEntry.service_name == "Web Cache Protection")
                {
                    if (logEntry.title.Contains("Event"))
                    {
                        LogEntries.Add(new LogEntryModel
                        {
                            Header = timeFDS.Item1,
                            Title = logEntry.file_deleted.ToString() + " B cleared",
                            Description = logEntry.title + " by " + logEntry.changed_by.ToString() + Environment.NewLine + " at " + timeFDS.Item2
                        });
                    }
                    else
                    {
                        var eventDetails = GetLogTitles(logEntry.title, logEntry.changed_by.ToString(), timeFDS.Item2);
                        LogEntries.Add(new LogEntryModel
                        {
                            Header = timeFDS.Item1,
                            Title = eventDetails.Item1,
                            Description = eventDetails.Item2
                        });
                    }
                }
                else if (logEntry.service_name == "Web Session Protection")
                {
                    if (logEntry.title.Contains("Event"))
                    {
                        LogEntries.Add(new LogEntryModel
                        {
                            Header = timeFDS.Item1,
                            Title = logEntry.file_deleted.ToString() + " Cookies cleared",
                            Description = logEntry.title + " by " + logEntry.changed_by.ToString() + Environment.NewLine + " at " + timeFDS.Item2
                        });
                    }
                    else
                    {
                        var eventDetails = GetLogTitles(logEntry.title, logEntry.changed_by.ToString(), timeFDS.Item2);
                        LogEntries.Add(new LogEntryModel
                        {
                            Header = timeFDS.Item1,
                            Title = eventDetails.Item1,
                            Description = eventDetails.Item2
                        });
                    }
                }
                else if (logEntry.service_name == "Web Tracking Protection")
                {

                    if (logEntry.title.Contains("Event"))
                    {
                        LogEntries.Add(new LogEntryModel
                        {
                            Header = timeFDS.Item1,
                            Title = logEntry.file_deleted.ToString() + " Files cleared",
                            Description = logEntry.title + " by " + logEntry.changed_by.ToString() + Environment.NewLine + " at " + timeFDS.Item2
                        });
                    }
                    else
                    {
                        var eventDetails = GetLogTitles(logEntry.title, logEntry.changed_by.ToString(), timeFDS.Item2);
                        LogEntries.Add(new LogEntryModel
                        {
                            Header = timeFDS.Item1,
                            Title = eventDetails.Item1,
                            Description = eventDetails.Item2
                        });
                    }
                }
                else if (logEntry.service_name == "DNS Cache Protection")
                {
                    var eventDetails = GetLogTitles(logEntry.title, logEntry.changed_by.ToString(), timeFDS.Item2);
                    LogEntries.Add(new LogEntryModel
                    {
                        Header = timeFDS.Item1,
                        Title = eventDetails.Item1,
                        Description = eventDetails.Item2
                    });
                }
                else if (logEntry.service_name == "Windows Registry Protection")
                {
                    LogEntries.Add(new LogEntryModel
                    {
                        Header = timeFDS.Item1,
                        Title = logEntry.file_deleted.ToString() + " Files cleared",
                        Description = logEntry.title + " by " + logEntry.changed_by.ToString() + " at " + timeFDS.Item2
                    });
                }
                else if (logEntry.service_name == "Free Storage Protection")
                {
                    LogEntries.Add(new LogEntryModel
                    {
                        Header = timeFDS.Item1,
                        Title = logEntry.title,
                        Description = logEntry.title + " by " + logEntry.changed_by.ToString() + " at " + timeFDS.Item2
                    });
                }
                else if (logEntry.service_name == "Trash Data Protection")
                {
                    //if (!apiCalled)
                    //{
                    LogEntries.Add(new LogEntryModel
                    {
                        Header = timeFDS.Item1,
                        Title = logEntry.file_deleted.ToString() + " Files cleared",
                        Description = logEntry.title + " completed by " + logEntry.changed_by.ToString() + " at " + timeFDS.Item2
                    });
                    //apiCalled = true;
                    //}
                }
                else if (logEntry.service_name == "System Network Monitoring Protection")
                {
                    if (logEntry.title.Contains("Requested"))
                    {
                        LogEntries.Add(new LogEntryModel
                        {
                            Header = timeFDS.Item1,
                            Title = logEntry.file_deleted + " Issues requested to delete",
                            Description = logEntry.title
                        });
                    }
                    else if (logEntry.title.Contains("Whitelisted"))
                    {
                        LogEntries.Add(new LogEntryModel
                        {
                            Header = timeFDS.Item1,
                            Title = logEntry.file_deleted + " Issues whitelisted",
                            Description = logEntry.title
                        });
                    }
                    else
                    {
                        LogEntries.Add(new LogEntryModel
                        {
                            Header = timeFDS.Item1,
                            Title = logEntry.file_deleted + " Issues deleted",
                            Description = logEntry.title + " by " + logEntry.changed_by.ToString()
                        });

                    }
                }
            }
        }



        public (string, string) GetLogTitles(string logTitle, string logEntryChangedby, string logEntryTime)
        {
            string eventTitle = string.Empty;
            string eventDesc = string.Empty;
            if (logTitle.Contains("completed"))
            {
                eventTitle = logTitle;
                eventDesc = logTitle + " by " + logEntryChangedby + " at " + logEntryTime;
            }
            else
            {
                eventTitle = logTitle + " completed";
                eventDesc = logTitle + " completed by " + logEntryChangedby + " at " + logEntryTime;
            }
            return (eventTitle, eventDesc);
        }

        public (string, string) FormatDateTime(string utcDateTimeString)
        {
            try
            {
                string logTime = string.Empty;
                string dataTimeVal = string.Empty;
                try
                {

                    // Parse the UTC datetime string to DateTime object
                    DateTime utcDateTime = DateTime.ParseExact(utcDateTimeString, "yyyy-MM-ddTHH:mm:ss.ffffffZ", null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal);

                    // Get the local time zone of the system
                    TimeZoneInfo localTimeZone = TimeZoneInfo.Local;

                    // Convert to local time zone
                    DateTime localDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, localTimeZone);

                    // Convert to EST if in USA, otherwise keep in local time zone
                    DateTime convertedDateTime;
                    if (localTimeZone.Id == "Eastern Standard Time")
                    {
                        // Convert to EST
                        TimeZoneInfo estTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                        convertedDateTime = TimeZoneInfo.ConvertTime(localDateTime, localTimeZone, estTimeZone);
                    }
                    else
                    {
                        // Already in EST
                        convertedDateTime = localDateTime;
                    }


                    logTime = convertedDateTime.ToString("hh:mm tt");
                    string eventHeader = Generic.FormatDateTime(convertedDateTime.ToString());
                    return (eventHeader, logTime);

                }
                catch (Exception ex)
                {
                    ex.ToString();
                    return ("", "");
                }

            }
            catch (Exception ex)
            {
                return ("", "");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}