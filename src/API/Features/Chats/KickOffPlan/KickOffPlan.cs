using FluentValidation;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Plans;
using Orchi.Api.Infrastructure.Agents.Plans.Artifacts;
using Orchi.Api.Infrastructure.Agents.Plans.Persistence;

namespace Orchi.Api.Features.Chats.KickOffPlan;

public static class KickOffPlan
{
    public sealed record Command(
        Guid ParentChatId,
        string PlanId,
        string Title,
        string ContentMarkdown) : ICommand<KickOffPlanResponse>;

    internal sealed class Handler(
        AgentSessionManager sessionManager,
        IPlanStore planStore,
        IOrchiArtifactWriterFactory artifactWriterFactory)
        : ICommandHandler<Command, KickOffPlanResponse>
    {
        public async Task<Result<KickOffPlanResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            ChatSession? parent = await sessionManager.GetOrLoadSessionAsync(command.ParentChatId, cancellationToken);
            if (parent is null)
            {
                return Result.Failure<KickOffPlanResponse>(Error.NotFound($"Chat '{command.ParentChatId}' was not found."));
            }

            if (!string.Equals(parent.Mode, OrchestrationAgentModeStrategy.Mode, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure<KickOffPlanResponse>(
                    Error.Validation("Mode.Invalid", "Plan kick-off is only available for orchestration chats."));
            }

            try
            {
                await planStore.UpsertAsync(
                    new PlanUpsertModel(
                        command.PlanId,
                        command.ParentChatId,
                        command.Title,
                        command.ContentMarkdown),
                    cancellationToken);
            }
            catch (ArgumentException ex)
            {
                return Result.Failure<KickOffPlanResponse>(Error.Validation("PlanId.Invalid", ex.Message));
            }

            string planFilePath;
            try
            {
                planFilePath = await artifactWriterFactory
                    .GetStrategy(OrchiArtifactKind.Plan)
                    .WriteAsync(
                        parent.WorkspacePath,
                        command.PlanId,
                        command.ContentMarkdown,
                        cancellationToken);
            }
            catch (ArgumentException ex)
            {
                return Result.Failure<KickOffPlanResponse>(Error.Validation("PlanId.Invalid", ex.Message));
            }

            Result<ChatSession> childResult = await sessionManager.CreateSessionAsync(
                parent.AgentId,
                parent.WorkspacePath,
                DefaultAgentModeStrategy.Mode,
                parentChatId: parent.Id,
                planFilePath: planFilePath,
                cancellationToken: cancellationToken);

            if (childResult.IsFailure)
            {
                return Result.Failure<KickOffPlanResponse>(childResult.Error);
            }

            ChatSession child = childResult.Value;

             string path = planFilePath.Trim();
            
            string initialPrompt =$"Implement the plan at `{path}`. Follow the plan precisely. Do not replan unless blocked. " +
                $"After the plan is fully implemented and validated, delete `{path}`. " +
                "If blocked, keep the plan file.";

            return Result.Success(new KickOffPlanResponse(
                child.Id,
                planFilePath,
                initialPrompt));
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(command => command.ParentChatId)
                .NotEmpty()
                .WithMessage("Parent chat id is required.");

            RuleFor(command => command.PlanId)
                .NotEmpty()
                .WithMessage("Plan id is required.");

            RuleFor(command => command.ContentMarkdown)
                .NotEmpty()
                .WithMessage("Plan content is required.");
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/chats/{parentChatId:guid}/plans/kickoff", Handle)
                .WithName("KickOffPlan")
                .WithTags("Chats")
                .Produces<KickOffPlanResponse>(StatusCodes.Status201Created);
        }

        private static async Task<IResult> Handle(
            Guid parentChatId,
            KickOffPlanRequest request,
            ICommandHandler<Command, KickOffPlanResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<KickOffPlanResponse> result = await handler.Handle(
                new Command(parentChatId, request.PlanId, request.Title, request.ContentMarkdown),
                cancellationToken);

            if (result.IsSuccess)
            {
                KickOffPlanResponse response = result.Value;
                return Results.Created($"/chats/{response.ChildChatId}", response);
            }

            return result.ToProblem();
        }
    }
}
