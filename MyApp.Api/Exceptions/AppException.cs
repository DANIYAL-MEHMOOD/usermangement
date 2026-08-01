namespace MyApp.Api.Exceptions;

public class AppException : Exception
{
    public int StatusCode { get; }

    public AppException(string message, int statusCode = 400) : base(message)
    {
        StatusCode = statusCode;
    }
}

public class NotFoundException : AppException
{
    public NotFoundException(string message = "Resource not found.") : base(message, 404) { }
}

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "Unauthorized access.") : base(message, 401) { }
}

public class ForbiddenException : AppException
{
    public ForbiddenException(string message = "Access forbidden.") : base(message, 403) { }
}

public class ValidationException : AppException
{
    public IEnumerable<string> Errors { get; }

    public ValidationException(string message, IEnumerable<string>? errors = null) : base(message, 400)
    {
        Errors = errors ?? Array.Empty<string>();
    }
}
