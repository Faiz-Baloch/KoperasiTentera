using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoperasiTentera.Service.Responses.Registration
{
    public class RegistrationResponse
    {
        public Guid? RegistrationId { get; set; }
        public string Status { get; set; } = string.Empty; 
        public string NextStep { get; set; } = string.Empty;
        public string? MaskedMobile { get; set; }
        public string? MaskedEmail { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
