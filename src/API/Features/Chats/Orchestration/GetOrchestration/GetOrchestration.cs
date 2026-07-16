using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents.Orchestration;

namespace Orchi.Api.Features.Chats.Orchestration.GetOrchestration;

public static class GetOrchestration
{
    public sealed record Query(Guid ParentChatId) : IQuery<OrchestrationStateResponse>;

    internal sealed class Handler(IOrchestrationWorkflowService workflowService)
        : IQueryHandler<Query, OrchestrationStateResponse>
    {
        public async Task<Result<OrchestrationStateResponse>> Handle(
            Query query,
            CancellationToken cancellationToken)
        {
            Result<OrchestrationSnapshot> result =
                await workflowService.GetSnapshotAsync(query.ParentChatId, cancellationToken);

            if (result.IsFailure)
            {
                return Result.Failure<OrchestrationStateResponse>(result.Error);
            }

            return Result.Success(OrchestrationMapper.ToResponse(result.Value));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/chats/{parentChatId:guid}/orchestration", Handle)
                .WithName("GetOrchestration")
                .WithTags("Chats")
                .Produces<OrchestrationStateResponse>(StatusCodes.Status200OK);
        }

        private static async Task<IResult> Handle(
            Guid parentChatId,
            IQueryHandler<Query, OrchestrationStateResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<OrchestrationStateResponse> result =
                await handler.Handle(new Query(parentChatId), cancellationToken);

            return result.ToProblem();
        }
    }
}
