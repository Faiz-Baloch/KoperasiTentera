using KoperasiTentera.Application.Common.Results;
using System.Text.Json;
using FluentValidation;
using System.Net;

namespace KoperasiTentera.Api.ExceptionHandling
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger,
            IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var correlationId = context.Items["CorrelationId"]?.ToString() ?? "unknown";
            var traceId = context.TraceIdentifier;

            _logger.LogError(exception,
                "Unhandled exception. CorrelationId: {CorrelationId}, TraceId: {TraceId}",
                correlationId, traceId);

            var (status, code, message, errors) = exception switch
            {
                ValidationException validationEx => (
                    (int)HttpStatusCode.BadRequest,
                    "VALIDATION_FAILED",
                    "Validation failed.",
                    validationEx.Errors.Select(e => new ApiError
                    {
                        Field = e.PropertyName,
                        Code = e.ErrorCode,
                        Message = e.ErrorMessage
                    }).ToList()
                ),

                KeyNotFoundException => (
                    (int)HttpStatusCode.NotFound,
                    "NOT_FOUND",
                    exception.Message,
                    new List<ApiError>()
                ),

                UnauthorizedAccessException => (
                    (int)HttpStatusCode.Unauthorized,
                    "UNAUTHORIZED",
                    "Unauthorized access.",
                    new List<ApiError>()
                ),

                _ => (
                    (int)HttpStatusCode.InternalServerError,
                    "INTERNAL_ERROR",
                    _env.IsDevelopment() ? exception.Message : "An unexpected error occurred.",
                    new List<ApiError>()
                )
            };

            var response = new ApiResponse
            {
                Success = false,
                Status = status,
                Code = code,
                Message = message,
                //Errors = errors,
                Meta = new ApiMeta
                {
                    TraceId = traceId,
                    CorrelationId = correlationId
                }
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = status;

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
        }
    }
}
