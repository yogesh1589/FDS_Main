using FDS.DTO.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDS.Services.Interface
{
    public interface IService
    {
        //int RunService();

        bool RunService(SubservicesData subservices, string serviceTypeDetails);
    }
}
