using Orchi.Api.Common.Results;

namespace Orchi.Api.Common.Http;

public static class HttpResultExtensions
{
    public static IResult ToProblem(this Result result) =>
        result.Match(
            () => Microsoft.AspNetCore.Http.Results.NoContent(),
            ToProblemResult);

    public static IResult ToProblem<T>(this Result<T> result) =>
        result.Match(
            Microsoft.AspNetCore.Http.Results.Ok,
            ToProblemResult);

    private static IResult ToProblemResult(Error error) =>
        error switch
        {
            ValidationError validationError => Microsoft.AspNetCore.Http.Results.ValidationProblem(
                validationError.Errors
                    .GroupBy(e => e.Code)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.Message).ToArray())),
            _ when error.Code == "NotFound" => Microsoft.AspNetCore.Http.Results.NotFound(new { error.Code, error.Message }),
            _ => Microsoft.AspNetCore.Http.Results.Problem(
                detail: error.Message,
                title: error.Code,
                statusCode: StatusCodes.Status400BadRequest)
        };
}
