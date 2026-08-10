using KoperasiTentera.Api.ExceptionHandling;
using KoperasiTentera.Api.Middleware;

namespace KoperasiTentera.Api.Extensions
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseEnterpriseMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware<CorrelationIdMiddleware>();
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            return app;
        }
    }
}
