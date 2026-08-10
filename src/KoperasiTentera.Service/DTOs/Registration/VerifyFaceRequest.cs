using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoperasiTentera.Service.DTOs.Registration
{
    public class VerifyFaceRequest
    {
        public Guid RegistrationId { get; set; }
        public string FaceImagePath { get; set; } = string.Empty;
    }
}
