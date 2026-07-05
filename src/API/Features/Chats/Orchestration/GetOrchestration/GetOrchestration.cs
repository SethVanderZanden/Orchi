using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents.Orchestration;

namespace Orchi.Api.Features.Chats.Orchestration.GetOrchestration;

public static class GetOrchestration
{
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
            IOrchestrationWorkflowService workflowService,
            CancellationToken cancellationToken)
        {
            Result<OrchestrationSnapshot> result =
                await workflowService.GetSnapshotAsync(parentChatId, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(OrchestrationMapper.ToResponse(result.Value))
                : result.ToProblem();
        }
    }
}
