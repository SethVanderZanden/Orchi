namespace Orchi.Api.Common.Results;

public record Error(string Code, string Message)
{
    public static Error None { get; } = new(string.Empty, string.Empty);

    public static Error Validation(string code, string message) => new(code, message);

    public static Error NotFound(string message) => new("NotFound", message);
}
