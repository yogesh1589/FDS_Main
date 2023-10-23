using FDS.Common.SystemMoniteringService;
using FDS.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDS.Common
{
    public class ProxyDetails
    {
        public (string, int) GetProxyDetails()
        {
            int cntCertif = 0;
            string jsonMergedList = string.Empty;
            try
            {

                List<ProxyLists> firefoxProxy = GetMozilaProxy();
                List<ProxyLists> systemProxy = GetSystemProxy();

                cntCertif = firefoxProxy.Count + systemProxy.Count;

                List<ProxyLists> proxy_info1 = new List<ProxyLists>();

                proxy_info1.AddRange(firefoxProxy);
                proxy_info1.AddRange(systemProxy);

                var proxyData = new ProxyData
                {
                    device_uuid = AppConstants.UUId,
                    proxy_info = proxy_info1

                };

                jsonMergedList = JsonConvert.SerializeObject(proxyData);
            }
            catch { }

            return (jsonMergedList, cntCertif);
        }

        public List<ProxyLists> GetMozilaProxy()
        {
            List<ProxyLists> proxyDatas = new List<ProxyLists>();
            MozilaProxies mozilaProxies = new MozilaProxies();
            proxyDatas = mozilaProxies.CheckMozilaProxy();
            return proxyDatas;
        }

        public List<ProxyLists> GetSystemProxy()
        {
            List<ProxyLists> proxyDatas = new List<ProxyLists>();
            SystemProxies proxyDetails = new SystemProxies();
            proxyDetails.CheckSystemProxy();
            return proxyDatas;
        }


    }
}
