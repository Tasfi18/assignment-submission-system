namespace AssignmentSystem.Application.Common.Exceptions;

/// <summary>Base type for every expected (non bug) failure. Mapped to HTTP status codes by the API middleware.</summary>
public abstract class AppException : Exception
{
    protected AppException(string message) : base(message)
    {
    }
}

/// <summary>The requested resource does not exist. -&gt; 404</summary>
public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string entity, Guid id) : base($"{entity} '{id}' was not found.")
    {
    }
}

/// <summary>Input failed validation. -&gt; 400</summary>
public class ValidationException : AppException
{
    public ValidationException(string message) : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(string field, string message) : base(message)
    {
        Errors = new Dictionary<string, string[]> { [field] = new[] { message } };
    }

    public IDictionary<string, string[]> Errors { get; }
}

/// <summary>A business rule refused the operation. -&gt; 422</summary>
public class BusinessRuleException : AppException
{
    public BusinessRuleException(string message) : base(message)
    {
    }
}

/// <summary>The caller is authenticated but not allowed to touch this resource. -&gt; 403</summary>
public class ForbiddenException : AppException
{
    public ForbiddenException(string message) : base(message)
    {
    }
}

/// <summary>Credentials were rejected. -&gt; 401</summary>
public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message) : base(message)
    {
    }
}

/// <summary>The operation conflicts with existing data (duplicate email, duplicate submission, ...). -&gt; 409</summary>
public class ConflictException : AppException
{
    public ConflictException(string message) : base(message)
    {
    }
}
