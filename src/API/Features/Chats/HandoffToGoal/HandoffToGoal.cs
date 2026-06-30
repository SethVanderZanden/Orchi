using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Modes;

namespace Orchi.Api.Features.Chats.HandoffToGoal;

public static class HandoffToGoal
{
    public sealed record Command(Guid OrchestratorChatId) : ICommand<HandoffToGoalResponse>;

    internal sealed class Handler(AgentSessionManager sessionManager)
        : ICommandHandler<Command, HandoffToGoalResponse>
    {
        public async Task<Result<HandoffToGoalResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            ChatSession? orchestrator = await sessionManager.GetOrLoadSessionAsync(command.OrchestratorChatId, cancellationToken);
            if (orchestrator is null)
            {
                return Result.Failure<HandoffToGoalResponse>(
                    Error.NotFound($"Chat '{command.OrchestratorChatId}' was not found."));
            }

            if (orchestrator.Mode != ChatMode.Orchestrate)
            {
                return Result.Failure<HandoffToGoalResponse>(
                    Error.Validation("Handoff.ModeInvalid", "Only orchestrate chats can hand off to goal mode."));
            }

            if (orchestrator.GoalChatId is not null)
            {
                return Result.Success(new HandoffToGoalResponse(orchestrator.GoalChatId.Value));
            }

            Result<ChatSession> goalResult = await sessionManager.CreateSessionAsync(
                orchestrator.AgentId,
                orchestrator.WorkspacePath,
                ChatMode.Goal,
                cancellationToken: cancellationToken);

            if (goalResult.IsFailure)
            {
                return Result.Failure<HandoffToGoalResponse>(goalResult.Error);
            }

            Result linkResult = await sessionManager.SetGoalChatIdAsync(orchestrator.Id, goalResult.Value.Id, cancellationToken);
            if (linkResult.IsFailure)
            {
                return Result.Failure<HandoffToGoalResponse>(linkResult.Error);
            }

            return Result.Success(new HandoffToGoalResponse(goalResult.Value.Id));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/chats/{chatId:guid}/handoff-to-goal", Handle)
                .WithName("HandoffToGoal")
                .WithTags("Chats")
                .Produces<HandoffToGoalResponse>(StatusCodes.Status201Created);
        }

        private static async Task<IResult> Handle(
            Guid chatId,
            ICommandHandler<Command, HandoffToGoalResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<HandoffToGoalResponse> result = await handler.Handle(new Command(chatId), cancellationToken);

            if (result.IsSuccess)
            {
                return Results.Created($"/chats/{result.Value.GoalChatId}", result.Value);
            }

            return result.ToProblem();
        }
    }
}
