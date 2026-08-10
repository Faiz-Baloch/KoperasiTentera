using Microsoft.EntityFrameworkCore.Metadata.Builders;
using KoperasiTentera.Domain.Entities;
using Microsoft.EntityFrameworkCore; 

namespace KoperasiTentera.Infrastructure.Persistence.Configurations
{
    public class RegistrationConfiguration : IEntityTypeConfiguration<Registrations>
    {
        public void Configure(EntityTypeBuilder<Registrations> builder)
        {
            builder.ToTable("Registrations");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.CustomerName).HasMaxLength(100).IsRequired();
            builder.Property(r => r.ICNumber).HasMaxLength(12).IsRequired();
            builder.Property(r => r.MobileNumber).HasMaxLength(15).IsRequired();
            builder.Property(r => r.Email).HasMaxLength(150).IsRequired();
            builder.Property(r => r.Status).HasMaxLength(50).IsRequired();
            builder.Property(r => r.PinHash).HasMaxLength(256); 
            builder.HasIndex(r => r.ICNumber);
            builder.HasIndex(r => new { r.ICNumber, r.Status });
        }
    }
} 