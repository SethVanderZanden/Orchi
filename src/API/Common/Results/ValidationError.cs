namespace Orchi.Api.Common.Results;

public sealed record ValidationError(IReadOnlyList<Error> Errors) : Error("Validation", "One or more validation errors occurred.")
{
    public static ValidationError FromErrors(IEnumerable<Error> errors) =>
        new(errors.ToList());
}
