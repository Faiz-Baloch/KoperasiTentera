using KoperasiTentera.Api.Extensions;
using KoperasiTentera.Api.Swagger;
using Microsoft.OpenApi.Models;
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
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "KoperasiTentera API",
            Version = "v1",
            Description = """
                # 🧪 Manual Testing Guide

                This Swagger page is designed for both technical and non-technical users.

                ## Quick start
                1. Open **Development Test Data → POST /api/development/test-data/seed** and click **Execute**.
                2. For a new registration flow, use **POST /api/development/test-data/registration** and copy the returned `registrationId`.
                3. Use **POST /api/development/test-data/registration/{registrationId}/prepare-otp?channel=Mobile** to prepare the known OTP `1234`.
                4. Use **POST /api/registration/verify-otp** with that `registrationId`, OTP `1234`, and channel `Mobile`.
                5. Repeat `prepare-otp` with `channel=Email` when you reach email verification.
                6. Accept privacy policy and set PIN `123456` / `123456`.

                ## Ready-made sample data
                - Existing customer IC: `880214566831`
                - New customer IC: `880214566832`
                - Mobile: `0173386676`
                - Email: `mariam.new@example.com`
                - Development OTP: `1234`
                - Development PIN: `123456`

                ## Important
                Development test-data endpoints are intended only for the **Development** environment. Do not expose them publicly.

                Every Registration request below also contains a ready-to-use example. Click **Try it out → Execute** and replace only the `registrationId` when required.
                """
        });

        options.OperationFilter<RegistrationSwaggerExamplesOperationFilter>();
    });

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Server=.;Database=KoperasiTentera;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

    builder.Services.AddInfrastructure(connectionString);
    builder.Services.AddServiceLayer();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
       // await DevelopmentTestData.SeedAsync(app.Services);
    }

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