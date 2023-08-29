using FDS.DTO.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDS.Services.AbstractClass
{
    public interface IService
    {
        void RunService(SubservicesData subservices);
    }
}
