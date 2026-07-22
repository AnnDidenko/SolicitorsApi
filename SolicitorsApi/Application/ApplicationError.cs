namespace SolicitorsApi.Application;

public class ApplicationError
{
    public ApplicationError(string code, string message, string? field = null)
    {
        Code = code;
        Message = message;
        Field = field;
    }

    public string Code { get; }

    public string Message { get; }

    public string? Field { get; }
}
