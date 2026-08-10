using KoperasiTentera.Application.Abstractions.Persistence.Repositories;
using KoperasiTentera.Infrastructure.Persistence.Repositories;
using KoperasiTentera.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection; 
using KoperasiTentera.Application.Persistence;
using KoperasiTentera.Infrastructure.DATA;
using Microsoft.EntityFrameworkCore;

namespace KoperasiTentera.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            string connectionString)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString, sql =>
                {
                    sql.EnableRetryOnFailure(3);
                    sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                }));

            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddHealthChecks()
                .AddDbContextCheck<ApplicationDbContext>();

            return services;
        }
    }
}
