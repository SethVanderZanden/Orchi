using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Entities;
using Orchi.Api.Infrastructure.UserPreferences;

namespace Orchi.Api.Features.UserPreferences.GetUserPreferences;

public static class GetUserPreferences
{
    public sealed record Query : IQuery<Response>;

    public sealed record Response(
        PostMessageBehavior PostMessageBehavior,
        IReadOnlyList<string> EnabledAgentIds,
        DateTimeOffset UpdatedAt);

    internal sealed class Handler(IUserPreferenceService preferenceService)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken cancellationToken)
        {
            UserPreferenceDto dto = await preferenceService.GetAsync(cancellationToken);
            return Result.Success(new Response(dto.PostMessageBehavior, dto.EnabledAgentIds, dto.UpdatedAt));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/user-preferences", Handle)
                .WithName("GetUserPreferences")
                .WithTags("UserPreferences")
                .Produces<Response>();
        }

        private static async Task<IResult> Handle(
            IQueryHandler<Query, Response> handler,
            CancellationToken cancellationToken)
        {
            Result<Response> result = await handler.Handle(new Query(), cancellationToken);
            return result.ToProblem();
        }
    }
}
