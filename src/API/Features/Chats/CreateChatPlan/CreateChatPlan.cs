using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Modes.Plan;

namespace Orchi.Api.Features.Chats.CreateChatPlan;

public static class CreateChatPlan
{
    public sealed record Command(Guid ChatId, string Title, string ContentMarkdown) : ICommand<PlanResponse>;

    internal sealed class Handler(AgentSessionManager sessionManager, PlanManager planManager)
        : ICommandHandler<Command, PlanResponse>
    {
        public async Task<Result<PlanResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            ChatSession? session = await sessionManager.GetOrLoadSessionAsync(command.ChatId, cancellationToken);
            if (session is null)
            {
                return Result.Failure<PlanResponse>(Error.NotFound($"Chat '{command.ChatId}' was not found."));
            }

            if (session.Mode is not (ChatMode.Plan or ChatMode.Orchestrate))
            {
                return Result.Failure<PlanResponse>(
                    Error.Validation("Plan.ModeInvalid", "Plans can only be created from plan or orchestrate chats."));
            }

            Result<PlanArtifact> result = planManager.CreatePlan(command.ChatId, command.Title, command.ContentMarkdown);
            if (result.IsFailure)
            {
                return Result.Failure<PlanResponse>(result.Error);
            }

            return Result.Success(PlanMapper.ToResponse(result.Value));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/chats/{chatId:guid}/plans", Handle)
                .WithName("CreateChatPlan")
                .WithTags("Chats")
                .Produces<PlanResponse>(StatusCodes.Status201Created);
        }

        private static async Task<IResult> Handle(
            Guid chatId,
            CreatePlanRequest request,
            ICommandHandler<Command, PlanResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<PlanResponse> result = await handler.Handle(
                new Command(chatId, request.Title, request.ContentMarkdown),
                cancellationToken);

            if (result.IsSuccess)
            {
                return Results.Created($"/plans/{result.Value.Id}", result.Value);
            }

            return result.ToProblem();
        }
    }
}
