using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDS.Services.AbstractClass
{
   interface IBaseBrowsersService
    {
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

        void ExecuteChromeLogic();
        void ExecuteEdgeLogic();
       void ExecuteFirefoxLogic();
    }
}
