using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System;

namespace KoperasiTentera.Application.Common.Results
{
    public class ApiResponse
    {
        public bool Success { get; init; }
        public int Status { get; init; }
        public string Code { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public object? Data { get; init; }
        public IReadOnlyList<ApiError> Errors { get; init; } = Array.Empty<ApiError>();
        public ApiMeta Meta { get; init; } = new();

        public static ApiResponse Ok(object? data = null, string code = "SUCCESS", string message = "Success")
            => new()
            {
                Success = true,
                Status = 200,
                Code = code,
                Message = message,
                Data = data
            };

        public static ApiResponse Fail(int status, string code, string message, IEnumerable<ApiError>? errors = null)
            => new()
            {
                Success = false,
                Status = status,
                Code = code,
                Message = message,
                Errors = errors?.ToList() ?? new List<ApiError>()
            };
    }
    public class ApiResponse<T> : ApiResponse
    {
        public new T? Data { get; init; }

        public static ApiResponse<T> Ok(T data, string code = "SUCCESS", string message = "Success")
            => new()
            {
                Success = true,
                Status = 200,
                Code = code,
                Message = message,
                Data = data
            };

        public static new ApiResponse<T> Fail(int status, string code, string message, IEnumerable<ApiError>? errors = null)
            => new()
            {
                Success = false,
                Status = status,
                Code = code,
                Message = message
            };
    }

}
