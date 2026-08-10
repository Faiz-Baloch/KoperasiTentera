using KoperasiTentera.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KoperasiTentera.Infrastructure.DATA
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Registrations> Registrations => Set<Registrations>();
        public DbSet<OtpVerification> OtpVerifications => Set<OtpVerification>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}
