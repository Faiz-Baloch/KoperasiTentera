using KoperasiTentera.Service.Validators.Registration;
using KoperasiTentera.Service.Services.Registration;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
 

namespace KoperasiTentera.Service.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddServiceLayer(this IServiceCollection services)
        {
            services.AddScoped<IRegistrationService, RegistrationService>();
            // FluentValidation
        
            services.AddValidatorsFromAssemblyContaining<SendOtpRequestValidator>();
         
            // AutoMapper
            services.AddAutoMapper(typeof(ServiceCollectionExtensions).Assembly);

            return services;
        }
    }
}
