using KoperasiTentera.Api.Extensions;
using KoperasiTentera.Infrastructure.Extensions;
using KoperasiTentera.Service.Extensions;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithThreadId()
        .WriteTo.Console()
        .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day));

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Server=.;Database=KoperasiTentera;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

    builder.Services.AddInfrastructure(connectionString);
    builder.Services.AddServiceLayer();

    var app = builder.Build();

    // Swagger should be registered early (before exception middleware)
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "KoperasiTentera API v1");
            options.RoutePrefix = "swagger";
        });
    }

    // Custom middleware
    app.UseEnterpriseMiddleware();          // CorrelationId + ExceptionHandling
    app.UseStaticFiles();
    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }