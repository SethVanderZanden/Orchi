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
using Orchi.Api.Infrastructure.Agents.Persistence;

namespace Orchi.Api.Features.Chats.KickOffReview;

public static class KickOffReview
{
    public sealed record Command(Guid ImplementationChildChatId) : ICommand<KickOffReviewResponse>;

    internal sealed class Handler(
        AgentSessionManager sessionManager,
        IChatStore chatStore,
        IPlanStore planStore,
        IOrchiArtifactWriterFactory artifactWriterFactory)
        : ICommandHandler<Command, KickOffReviewResponse>
    {
        public async Task<Result<KickOffReviewResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            ChatSession? implementationChild = await sessionManager.GetOrLoadSessionAsync(
                command.ImplementationChildChatId,
                cancellationToken);

            if (implementationChild is null)
            {
                return Result.Failure<KickOffReviewResponse>(
                    Error.NotFound($"Chat '{command.ImplementationChildChatId}' was not found."));
            }

            if (implementationChild.ParentChatId is null)
            {
                return Result.Failure<KickOffReviewResponse>(
                    Error.Validation("Parent.Missing", "Review kick-off requires an implementation child chat."));
            }

            string? planId = PlanMarkdownParser.TryExtractPlanIdFromPath(implementationChild.PlanFilePath);
            if (planId is null)
            {
                return Result.Failure<KickOffReviewResponse>(
                    Error.Validation("PlanFilePath.Invalid", "Implementation child must have a plan file path."));
            }

            ChatSession? parent = await chatStore.GetAsync(
                implementationChild.ParentChatId.Value,
                cancellationToken);

            if (parent is null)
            {
                return Result.Failure<KickOffReviewResponse>(
                    Error.NotFound($"Parent chat '{implementationChild.ParentChatId}' was not found."));
            }

            if (!string.Equals(parent.Mode, OrchestrationAgentModeStrategy.Mode, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure<KickOffReviewResponse>(
                    Error.Validation("Mode.Invalid", "Review kick-off is only available for orchestration parent chats."));
            }

            IReadOnlyList<ChatSession> siblings =
                await sessionManager.ListChildSessionsAsync(parent.Id, cancellationToken);
            IOrchiArtifactWriterStrategy reviewWriter =
                artifactWriterFactory.GetStrategy(OrchiArtifactKind.Review);
            string expectedReviewPath = reviewWriter.BuildRelativePath(planId);

            if (siblings.Any(chat =>
                    chat.ParentChatId == parent.Id &&
                    string.Equals(chat.PlanFilePath, expectedReviewPath, StringComparison.OrdinalIgnoreCase)))
            {
                return Result.Failure<KickOffReviewResponse>(
                    Error.Validation("Review.Exists", $"A review child already exists for plan '{planId}'."));
            }

            StoredPlan? plan = await planStore.GetAsync(parent.Id, planId, cancellationToken);
            if (plan is null)
            {
                return Result.Failure<KickOffReviewResponse>(
                    Error.Validation(
                        "Plan.NotFound",
                        $"Original plan '{planId}' was not found."));
            }

            string reviewBrief = ReviewBriefBuilder.Build(
                planId,
                plan.ContentMarkdown,
                implementationChild.Id,
                parent.Id);

            string reviewFilePath;
            try
            {
                reviewFilePath = await reviewWriter.WriteAsync(
                    parent.WorkspacePath,
                    planId,
                    reviewBrief,
                    cancellationToken);
            }
            catch (ArgumentException ex)
            {
                return Result.Failure<KickOffReviewResponse>(Error.Validation("PlanId.Invalid", ex.Message));
            }

            if (parent.WorkspaceId is null)
            {
                return Result.Failure<KickOffReviewResponse>(
                    Error.Validation("Workspace.Missing", "Parent chat has no workspace."));
            }

            Result<ChatSession> reviewChildResult = await sessionManager.CreateSessionAsync(
                parent.WorkspaceId.Value,
                mode: ReviewAgentModeStrategy.Mode,
                parentChatId: parent.Id,
                planFilePath: reviewFilePath,
                modelId: parent.ModelId,
                contextSizeId: parent.ContextSizeId,
                reasoningEffortId: parent.ReasoningEffortId,
                approvalPolicyId: parent.ApprovalPolicyId,
                cancellationToken: cancellationToken);

            if (reviewChildResult.IsFailure)
            {
                return Result.Failure<KickOffReviewResponse>(reviewChildResult.Error);
            }

            ChatSession reviewChild = reviewChildResult.Value;
            string initialPrompt = ReviewPlanTask.Build(reviewFilePath);

            return Result.Success(new KickOffReviewResponse(
                reviewChild.Id,
                reviewFilePath,
                initialPrompt));
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(command => command.ImplementationChildChatId)
                .NotEmpty()
                .WithMessage("Implementation child chat id is required.");
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/chats/{implementationChildChatId:guid}/review/kickoff", Handle)
                .WithName("KickOffReview")
                .WithTags("Chats")
                .Produces<KickOffReviewResponse>(StatusCodes.Status201Created);
        }

        private static async Task<IResult> Handle(
            Guid implementationChildChatId,
            ICommandHandler<Command, KickOffReviewResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<KickOffReviewResponse> result = await handler.Handle(
                new Command(implementationChildChatId),
                cancellationToken);

            if (result.IsSuccess)
            {
                KickOffReviewResponse response = result.Value;
                return Results.Created($"/chats/{response.ReviewChildChatId}", response);
            }

            return result.ToProblem();
        }
    }
}
