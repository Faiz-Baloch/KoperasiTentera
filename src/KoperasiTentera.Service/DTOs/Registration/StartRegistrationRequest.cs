using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoperasiTentera.Service.DTOs.Registration
{
    public record StartRegistrationRequest(
         string CustomerName,
         string ICNumber,
         string MobileNumber,
         string Email);
}
