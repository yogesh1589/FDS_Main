using FDS.DTO.Responses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms.Design;

namespace FDS.Common
{
    class DataModel
    {
        public string SubService { get; set; }
        public DateTime ExecutionDateTime { get; set; }
    }
    public class ReadWriteFile
    {

        public static bool WriteJsonFile(Dictionary<SubservicesData, DateTime> lstCron, string jsonCronFile)
        {
            if (lstCron.Count > 0)
            {
                List<DataModel> dataList = new List<DataModel>();
                // Loop to simulate receiving data
                foreach (var key in lstCron)
                {
                    SubservicesData SubservicesData = key.Key;
                    // Create a new DataModel instance with the values
                    var newData = new DataModel { SubService = SubservicesData.Sub_service_name, ExecutionDateTime = key.Value };
                    dataList.Add(newData);
                }

                // Serialize the list of DataModel objects to JSON
                string jsonData = System.Text.Json.JsonSerializer.Serialize(dataList, new JsonSerializerOptions { WriteIndented = true });

                // Write the JSON data to the file
                File.WriteAllText(jsonCronFile, jsonData);
                return true;
            }
            return false;

        }

        public static DateTime GetExecutionTime(string serviceName, string jsonCronFile)
        {
            try
            {
                 
                string jsonContent1 = File.ReadAllText(jsonCronFile);
                JArray jsonArray = JArray.Parse(jsonContent1);

                foreach (JToken item in jsonArray)
                {
                    // Access specific properties of each item
                    string itemName = item["SubService"].ToString();
                    DateTime itemValue = (DateTime)item["ExecutionDateTime"];

                    if (serviceName == itemName)
                    {
                        return itemValue;
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
            return DateTime.MinValue;
        }


        public static bool RemoveService(string serviceName, string jsonCronFile)
        {

            // Read the JSON file content
            string jsonContent = File.ReadAllText(jsonCronFile);

            // Parse the JSON content into a JArray
            JArray jsonArray = JArray.Parse(jsonContent);

            int indexToRemove = 0;
            foreach (JToken item in jsonArray)
            {
                indexToRemove = indexToRemove + 1;
                // Access specific properties of each item
                string itemName = item["SubService"].ToString();
                DateTime itemValue = (DateTime)item["ExecutionDateTime"];

                if (serviceName == itemName)
                {
                    // Remove the item from the JArray
                    if (indexToRemove >= 0 && indexToRemove < jsonArray.Count)
                    {
                        jsonArray.RemoveAt(indexToRemove);
                    }
                }
            }           

            // Convert the modified JArray back to a string
            string modifiedJsonContent = jsonArray.ToString(Formatting.Indented);
            // Write the modified JSON content back to the file
            File.WriteAllText(jsonCronFile, modifiedJsonContent);
            return true;
        }

    }

}
