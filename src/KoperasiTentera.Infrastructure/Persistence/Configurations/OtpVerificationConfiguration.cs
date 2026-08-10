using Microsoft.EntityFrameworkCore.Metadata.Builders;
using KoperasiTentera.Domain.Entities;
using Microsoft.EntityFrameworkCore; 

namespace KoperasiTentera.Infrastructure.Persistence.Configurations
{
    public   class OtpVerificationConfiguration
    : IEntityTypeConfiguration<OtpVerification>
    {
        public void Configure(EntityTypeBuilder<OtpVerification> builder)
        {
            builder.ToTable("OtpVerifications");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.RegistrationId)
                .IsRequired();

            builder.Property(x => x.Channel)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.OtpHash)
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(x => x.ExpiresAtUtc)
                .IsRequired();

            builder.Property(x => x.Attempts)
                .IsRequired();

            builder.Property(x => x.IsUsed)
                .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();

            // Registration ke OTP records jaldi find karne ke liye
            builder.HasIndex(x => new
            {
                x.RegistrationId,
                x.Channel,
                x.IsUsed
            });

            builder.HasIndex(x => x.ExpiresAtUtc);
        }
    }
}
