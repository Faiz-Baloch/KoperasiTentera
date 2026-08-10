using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoperasiTentera.Service.DTOs.Registration
{
    public record SetPinRequest(Guid RegistrationId, string Pin, string ConfirmPin);
}
