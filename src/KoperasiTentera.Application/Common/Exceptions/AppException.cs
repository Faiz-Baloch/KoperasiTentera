namespace KoperasiTentera.Application.Common.Exceptions;

public abstract class AppException : Exception
{
    public int StatusCode { get; }
    protected AppException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}

public sealed class NotFoundException : AppException
{
    public NotFoundException(string message) : base(404, message)
    {
    }
}

public sealed class ConflictException : AppException
{
    public ConflictException(string message) : base(409, message)
    {
    }
}

public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string message) : base(403, message)
    {
    }
}

public sealed class UnauthorizedException : AppException
{
    public UnauthorizedException(string message) : base(401, message)
    {
    }
}
public sealed class ValidationAppException : AppException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationAppException(IDictionary<string, string[]> errors)
        : base(400, "One or more fields are invalid.")
    {
        Errors = errors;
    }
}
