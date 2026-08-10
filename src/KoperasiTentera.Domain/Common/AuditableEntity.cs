namespace KoperasiTentera.Domain.Common;
public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }
}
