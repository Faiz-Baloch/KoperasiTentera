using KoperasiTentera.Domain.Common; 

namespace KoperasiTentera.Domain.Entities
{
    public class OtpVerification : BaseEntity
    {
        public Guid RegistrationId { get; set; }
        public string Channel { get; set; } = string.Empty;
        public string OtpHash { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
        public int Attempts { get; set; }
        public bool IsUsed { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
