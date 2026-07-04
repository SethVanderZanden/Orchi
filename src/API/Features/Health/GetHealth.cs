using Orchi.Api.Common.Abstractions;

namespace Orchi.Api.Features.Health;

public static class GetHealth
{
    public sealed record Response(string Status);

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/health", Handle)
                .WithName("GetHealth")
                .WithTags("Health")
                .Produces<Response>();
        }

        private static IResult Handle()
        {
            return Results.Ok(new Response("healthy"));
        }
    }
}
