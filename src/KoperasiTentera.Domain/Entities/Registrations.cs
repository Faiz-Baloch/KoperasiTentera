using KoperasiTentera.Domain.Common; 

namespace KoperasiTentera.Domain.Entities
{  
    public class Registrations : AuditableEntity
    {
        public string CustomerName { get; set; } = string.Empty;
        public string ICNumber { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string Status { get; set; } = RegistrationStatuses.PendingOtpMobile;

        public bool IsMobileVerified { get; set; }
        public bool IsEmailVerified { get; set; }
        public bool IsPrivacyAccepted { get; set; }
        public bool IsFaceVerified { get; set; }
        public string? FaceImagePath { get; set; }

       
        public string? PinHash { get; set; }
    }

}
