using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoperasiTentera.Service.DTOs.Registration
{
    /// <param name="Channel">"Mobile" or "Email".</param>
    public record VerifyOtpRequest(Guid RegistrationId, string Otp, string Channel);
}
