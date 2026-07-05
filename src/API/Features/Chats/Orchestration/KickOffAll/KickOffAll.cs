using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents.Orchestration;

namespace Orchi.Api.Features.Chats.Orchestration.KickOffAll;

public static class KickOffAll
{
    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/chats/{parentChatId:guid}/orchestration/kickoff-all", Handle)
                .WithName("KickOffAllOrchestrationPlans")
                .WithTags("Chats")
                .Produces<OrchestrationStateResponse>(StatusCodes.Status200OK);
        }

        private static async Task<IResult> Handle(
            Guid parentChatId,
            IOrchestrationWorkflowService workflowService,
            CancellationToken cancellationToken)
        {
            Result<OrchestrationSnapshot> result =
                await workflowService.StartKickoffAllAsync(parentChatId, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(OrchestrationMapper.ToResponse(result.Value))
                : result.ToProblem();
        }
    }
}
