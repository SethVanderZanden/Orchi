using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents.Orchestration;

namespace Orchi.Api.Features.Chats.Orchestration.KickOffAll;

public static class KickOffAll
{
    public sealed record Command(Guid ParentChatId) : ICommand<OrchestrationStateResponse>;

    internal sealed class Handler(IOrchestrationWorkflowService workflowService)
        : ICommandHandler<Command, OrchestrationStateResponse>
    {
        public async Task<Result<OrchestrationStateResponse>> Handle(
            Command command,
            CancellationToken cancellationToken)
        {
            Result<OrchestrationSnapshot> result =
                await workflowService.StartKickoffAllAsync(command.ParentChatId, cancellationToken);

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
            app.MapPost("/chats/{parentChatId:guid}/orchestration/kickoff-all", Handle)
                .WithName("KickOffAllOrchestrationPlans")
                .WithTags("Chats")
                .Produces<OrchestrationStateResponse>(StatusCodes.Status200OK);
        }

        private static async Task<IResult> Handle(
            Guid parentChatId,
            ICommandHandler<Command, OrchestrationStateResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<OrchestrationStateResponse> result =
                await handler.Handle(new Command(parentChatId), cancellationToken);

            return result.ToProblem();
        }
    }
}
