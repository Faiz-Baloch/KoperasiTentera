using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace KoperasiTentera.Api.Swagger;

/// <summary>
/// Adds ready-to-use request examples to the Registration endpoints so that
/// both technical and non-technical users can test the API from Swagger UI.
/// </summary>
public sealed class RegistrationSwaggerExamplesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = context.ApiDescription.RelativePath?.Trim('/').ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("api/registration/"))
            return;

        if (operation.RequestBody is null)
            return;

        var example = path switch
        {
            "api/registration/check-ic" => new OpenApiObject
            {
                ["icNumber"] = new OpenApiString("880214566831")
            },
            "api/registration/start" => new OpenApiObject
            {
                ["customerName"] = new OpenApiString("Mariam Abdul Rashid"),
                ["icNumber"] = new OpenApiString("880214566832"),
                ["mobileNumber"] = new OpenApiString("0173386676"),
                ["email"] = new OpenApiString("mariam.new@example.com")
            },
            "api/registration/send-otp" => new OpenApiObject
            {
                ["registrationId"] = new OpenApiString("00000000-0000-0000-0000-000000000000"),
                ["channel"] = new OpenApiString("Mobile")
            },
            "api/registration/verify-otp" => new OpenApiObject
            {
                ["registrationId"] = new OpenApiString("00000000-0000-0000-0000-000000000000"),
                ["otp"] = new OpenApiString("1234"),
                ["channel"] = new OpenApiString("Mobile")
            },
            "api/registration/change-email" => new OpenApiObject
            {
                ["registrationId"] = new OpenApiString("00000000-0000-0000-0000-000000000000"),
                ["email"] = new OpenApiString("new.email@example.com")
            },
            "api/registration/accept-privacy-policy" => new OpenApiObject
            {
                ["registrationId"] = new OpenApiString("00000000-0000-0000-0000-000000000000"),
                ["accepted"] = new OpenApiBoolean(true)
            },
            "api/registration/set-pin" => new OpenApiObject
            {
                ["registrationId"] = new OpenApiString("00000000-0000-0000-0000-000000000000"),
                ["pin"] = new OpenApiString("123456"),
                ["confirmPin"] = new OpenApiString("123456")
            },
            _ => null
        };

        if (example is null)
            return;

        foreach (var content in operation.RequestBody.Content.Values)
            content.Example = example;
    }
}
