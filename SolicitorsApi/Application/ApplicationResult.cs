namespace SolicitorsApi.Application;

public class ApplicationResult<T>
{
    private ApplicationResult(T? value, IReadOnlyList<ApplicationError> errors, int statusCode)
    {
        Value = value;
        Errors = errors;
        StatusCode = statusCode;
    }

    public T? Value { get; }

    public IReadOnlyList<ApplicationError> Errors { get; }

    public int StatusCode { get; }

    public bool IsSuccess => StatusCode is >= 200 and < 300;

    public static ApplicationResult<T> Ok(T value)
    {
        return new ApplicationResult<T>(value, [], StatusCodes.Status200OK);
    }

    public static ApplicationResult<T> Validation(IReadOnlyList<ApplicationError> errors)
    {
        return new ApplicationResult<T>(default, errors, StatusCodes.Status400BadRequest);
    }

    public static ApplicationResult<T> NotFound(string message)
    {
        return new ApplicationResult<T>(
            default,
            [new ApplicationError("notFound", message)],
            StatusCodes.Status404NotFound);
    }

    public static ApplicationResult<T> FailedDependency(IReadOnlyList<ApplicationError> errors)
    {
        return new ApplicationResult<T>(default, errors, StatusCodes.Status424FailedDependency);
    }

    public static ApplicationResult<T> NotImplemented(string message)
    {
        return new ApplicationResult<T>(
            default,
            [new ApplicationError("notImplemented", message)],
            StatusCodes.Status501NotImplemented);
    }
}
