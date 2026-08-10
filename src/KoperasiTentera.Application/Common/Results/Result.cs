using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System;

namespace KoperasiTentera.Application.Common.Results
{
    public class Result
    {
        public bool IsSuccess { get; }
        public string Code { get; }
        public string Message { get; }
        public IReadOnlyList<ApiError> Errors { get; }

        protected Result(bool isSuccess, string code, string message, IEnumerable<ApiError>? errors = null)
        {
            IsSuccess = isSuccess;
            Code = code;
            Message = message;
            Errors = errors?.ToList();//?? Array.Empty<ApiError>();
        }

        public static Result Success(string code = "SUCCESS", string message = "Success")
            => new(true, code, message);

        public static Result Failure(string code, string message, IEnumerable<ApiError>? errors = null)
            => new(false, code, message, errors);

        public static Result NotFound(string message = "Resource not found")
            => Failure("NOT_FOUND", message);

        public static Result ValidationFailed(IEnumerable<ApiError> errors)
            => Failure("VALIDATION_FAILED", "Validation failed.", errors);

        public static Result Conflict(string message = "Conflict occurred")
            => Failure("CONFLICT", message);
    }

    public class Result<T> : Result
    {
        public T? Value { get; }

        private Result(bool isSuccess, T? value, string code, string message, IEnumerable<ApiError>? errors = null)
            : base(isSuccess, code, message, errors)
        {
            Value = value;
        }

        public static Result<T> Success(T value, string code = "SUCCESS", string message = "Success")
            => new(true, value, code, message);

        public static new Result<T> Failure(string code, string message, IEnumerable<ApiError>? errors = null)
            => new(false, default, code, message, errors);

        public static new Result<T> NotFound(string message = "Resource not found")
            => Failure("NOT_FOUND", message);

        public static new Result<T> ValidationFailed(IEnumerable<ApiError> errors)
            => Failure("VALIDATION_FAILED", "Validation failed.", errors);
    }
}
