using FDS.Common;
using FDS.DTO.Requests;
using FDS.DTO.Responses;
using FDS.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace FDS.Services.AbstractClass
{
    public abstract class BaseService
    {

        protected ILogger _logger;

       

        public BaseService(ILogger logger)
        {
            _logger = logger;
        }


        public void ExecuteLogicForBrowser(string browserName)
        {
            switch (browserName)
            {
                case "chrome":
                    ExecuteChromeLogic();
                    break;
                case "edge":
                    ExecuteEdgeLogic();
                    break;
                case "firefox":
                    ExecuteFirefoxLogic();
                    break;
            }
        }

        protected abstract void ExecuteChromeLogic();
        protected abstract void ExecuteEdgeLogic();
        protected abstract void ExecuteFirefoxLogic();


        public abstract void RunService(SubservicesData subservices);
        public void LogServicesData(string authorizationCode, string subServiceName, long FileProcessed, string ServiceId, bool IsManualExecution)
        {             
            _logger.LogInformation(authorizationCode, subServiceName, FileProcessed, ServiceId, IsManualExecution);            
        }

        public void KillCmd()
        {
            Array.ForEach(Process.GetProcessesByName("cmd"), x => x.Kill());
        }
      
        public int IsBrowserOpen(string browser)
        {
            int bCnt = 0;
            Process[] chromeProcesses = Process.GetProcessesByName(browser);
            string test = string.Empty;
            foreach (Process process in chromeProcesses)
            {
                string processOwner = GetProcessOwner2(process.Id);
                if (!string.IsNullOrEmpty(processOwner))
                {
                    test = processOwner;
                    if (System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToUpper().ToString().Contains(processOwner.ToUpper().ToString()))
                    {
                        bCnt++;
                    }
                }
            }
            // MessageBox.Show("User1 " + WindowsIdentity.GetCurrent().Name.ToUpper().ToString() + " User2 " + test + " Count = " + bCnt + " For Browser " + browser);
            return bCnt;
        }

        static string GetProcessOwner2(int processId)
        {
            string query = "SELECT * FROM Win32_Process WHERE ProcessId = " + processId;
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
            ManagementObjectCollection processList = searcher.Get();

            foreach (ManagementObject obj in processList)
            {
                string[] ownerInfo = new string[2];
                obj.InvokeMethod("GetOwner", (object[])ownerInfo);
                return ownerInfo[0];
            }
            return null;
        }
    }
}
