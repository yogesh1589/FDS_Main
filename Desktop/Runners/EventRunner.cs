using FDS.DTO.Responses;
using FDS.Factories;
using FDS.Runners.Abstract;
using FDS.Services.AbstractClass;
using FDS.SingleTon;
using System;
using System.Collections.Generic;

namespace FDS.Runners
{

    public class EventRunner : BaseBrowser
    {
        Dictionary<BaseService, ServiceTypeName> serviceTypeMapping = new Dictionary<BaseService, ServiceTypeName>();

        public bool RunAll(Dictionary<string, SubservicesData> dicEventServices, string serviceRunType, List<string> whitelistedDomain)
        {

            
            var result = ServiceToRun(dicEventServices);

            var servicesToRun = result.Item1;
            serviceTypeMapping = result.Item2;

            bool LogicResult = RunLogic(servicesToRun, whitelistedDomain);

            bool LogResult = false;
            //MessageBox.Show(LogicResult.ToString());

            if (LogicResult)
            {
                LogResult = LogData(servicesToRun, dicEventServices, serviceRunType);
            }

            return LogResult;
        }



        public override bool LogData(List<BaseService> servicesToRun, Dictionary<string, SubservicesData> dicEventServices, string serviceRunType)
        {
            GlobalVariables globals = GlobalVariables.Instance;
            GlobalDictionaryService globalDict = GlobalDictionaryService.Instance;
            bool result = false;
            bool logFlg = false;
            try
            {


                foreach (BaseService service in servicesToRun)
                {
                    if (serviceTypeMapping.TryGetValue(service, out ServiceTypeName serviceType))
                    {
                        int totalLogicResult = service.GetTotalLogicResult();                    

                        if (dicEventServices.TryGetValue(serviceType.ToString(), out SubservicesData subservices))
                        {

                            //MessageBox.Show(globalDict.DictionaryService.ContainsKey(serviceType.ToString()).ToString());
                            //MessageBox.Show(globalDict.DictionaryService[serviceType.ToString()].ToString());


                            if (globalDict.DictionaryService.ContainsKey(serviceType.ToString()) && !globalDict.DictionaryService[serviceType.ToString()])
                            {
                                //MessageBox.Show("API Name 2 - " + subservices.Sub_service_name.ToString());

                                service.LogService(subservices.Sub_service_authorization_code, subservices.Sub_service_name, totalLogicResult, subservices.Id.ToString(), subservices.Execute_now, serviceRunType);
                                globalDict.DictionaryService[serviceType.ToString()] = true;
                                logFlg = true;
                            }
                            else if (serviceRunType != "E")
                            {
                                //MessageBox.Show("API Scheduled 2 - " + subservices.Sub_service_name.ToString());

                                service.LogService(subservices.Sub_service_authorization_code, subservices.Sub_service_name, totalLogicResult, subservices.Id.ToString(), subservices.Execute_now, serviceRunType);
                                logFlg = true;
                            }
                        }

                    }
                }

                

                //foreach (BaseService service in servicesToRun)
                //{
                //    int totalLogicResult = service.GetTotalLogicResult();
                //    if (dicEventServices.TryGetValue(service.GetType().Name.ToString(), out SubservicesData subservices))
                //    {                         
                //        if (globalDict.DictionaryService.ContainsKey(service.GetType().Name.ToString()) && !globalDict.DictionaryService[service.GetType().Name.ToString()])
                //        {                            
                //            service.LogService(subservices.Sub_service_authorization_code, subservices.Sub_service_name, totalLogicResult, subservices.Id.ToString(), subservices.Execute_now, serviceRunType);
                //            globalDict.DictionaryService[service.GetType().Name] = true;
                //            logFlg = true;
                //        }
                //        else if (serviceRunType != "E")
                //        {
                //            service.LogService(subservices.Sub_service_authorization_code, subservices.Sub_service_name, totalLogicResult, subservices.Id.ToString(), subservices.Execute_now, serviceRunType);
                //            logFlg = true;
                //        }
                //    }
                //}
                if (logFlg)
                {
                    result = true;
                }

            }
            catch (Exception ex)
            {
                result = false;
            }
            return result;
        }





    }
}
