using Orchi.Api.Common.Results;

namespace Orchi.Api.Common.Http;

public static class SseResultExtensions
{
    public static async Task WriteErrorAsync(
        this HttpResponse response,
        Error error,
        CancellationToken cancellationToken)
    {
        switch (error)
        {
            case ValidationError validationError:
                response.StatusCode = StatusCodes.Status400BadRequest;
                await response.WriteAsJsonAsync(
                    new
                    {
                        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                        Title = validationError.Code,
                        Status = StatusCodes.Status400BadRequest,
                        Errors = validationError.Errors
                            .GroupBy(e => e.Code)
                            .ToDictionary(g => g.Key, g => g.Select(e => e.Message).ToArray())
                    },
                    cancellationToken);
                break;

            case Error { Code: "NotFound" }:
                response.StatusCode = StatusCodes.Status404NotFound;
                await response.WriteAsJsonAsync(
                    new { error.Code, error.Message },
                    cancellationToken);
                break;

            default:
                response.StatusCode = StatusCodes.Status400BadRequest;
                await response.WriteAsJsonAsync(
                    new
                    {
                        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                        Title = error.Code,
                        Status = StatusCodes.Status400BadRequest,
                        Detail = error.Message
                    },
                    cancellationToken);
                break;
        }
    }
}
