using FluentValidation;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Entities;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Plans;
using Orchi.Api.Infrastructure.Agents.Plans.Artifacts;
using Orchi.Api.Infrastructure.Agents.Plans.Persistence;
using Orchi.Api.Infrastructure.Git.Workspace;
using Orchi.Api.Infrastructure.Projects;

namespace Orchi.Api.Features.Chats.KickOffPlan;

public static class KickOffPlan
{
    public sealed record Command(
        Guid ParentChatId,
        string PlanId,
        string Title,
        string ContentMarkdown,
        string? BaseBranch) : ICommand<KickOffPlanResponse>;

    internal sealed class Handler(
        AgentSessionManager sessionManager,
        IPlanStore planStore,
        IOrchiArtifactWriterFactory artifactWriterFactory,
        IProjectStore projectStore,
        IGitWorkspaceService gitWorkspaceService)
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

            if (parent.WorkspaceId is null || parent.ProjectId is null)
            {
                return Result.Failure<KickOffPlanResponse>(
                    Error.Validation("Workspace.Missing", "Parent chat has no project workspace."));
            }

            Project? project = await projectStore.GetProjectAsync(parent.ProjectId.Value, cancellationToken);
            if (project is null)
            {
                return Result.Failure<KickOffPlanResponse>(
                    Error.NotFound($"Project '{parent.ProjectId}' was not found."));
            }

            Guid childWorkspaceId = parent.WorkspaceId.Value;
            string childWorkspacePath = parent.WorkspacePath;

            if (project.UseWorktreeOnKickoff)
            {
                Result<(Guid WorkspaceId, string Path)> worktreeResult = await ProvisionWorktreeAsync(
                    project,
                    parent.WorkspacePath,
                    command.PlanId,
                    command.BaseBranch,
                    cancellationToken);

                if (worktreeResult.IsFailure)
                {
                    return Result.Failure<KickOffPlanResponse>(worktreeResult.Error);
                }

                childWorkspaceId = worktreeResult.Value.WorkspaceId;
                childWorkspacePath = worktreeResult.Value.Path;
            }

            string planFilePath;
            try
            {
                planFilePath = await artifactWriterFactory
                    .GetStrategy(OrchiArtifactKind.Plan)
                    .WriteAsync(
                        childWorkspacePath,
                        command.PlanId,
                        command.ContentMarkdown,
                        cancellationToken);
            }
            catch (ArgumentException ex)
            {
                return Result.Failure<KickOffPlanResponse>(Error.Validation("PlanId.Invalid", ex.Message));
            }

            Result<ChatSession> childResult = await sessionManager.CreateSessionAsync(
                childWorkspaceId,
                mode: ImplementationAgentModeStrategy.Mode,
                parentChatId: parent.Id,
                planFilePath: planFilePath,
                modelId: parent.ModelId,
                contextSizeId: parent.ContextSizeId,
                reasoningEffortId: parent.ReasoningEffortId,
                approvalPolicyId: parent.ApprovalPolicyId,
                cancellationToken: cancellationToken);

            if (childResult.IsFailure)
            {
                return Result.Failure<KickOffPlanResponse>(childResult.Error);
            }

            ChatSession child = childResult.Value;

            string initialPrompt = PlanImplementationTask.Build(planFilePath);
            const string kickoffMessage = "Begin implementation.";

            return Result.Success(new KickOffPlanResponse(
                child.Id,
                planFilePath,
                initialPrompt,
                kickoffMessage));
        }

        private async Task<Result<(Guid WorkspaceId, string Path)>> ProvisionWorktreeAsync(
            Project project,
            string repositoryPath,
            string planId,
            string? baseBranchOverride,
            CancellationToken cancellationToken)
        {
            string baseBranch = string.IsNullOrWhiteSpace(baseBranchOverride)
                ? project.DefaultBaseBranch
                : baseBranchOverride.Trim();

            string branchName = WorktreeBranchPattern.Resolve(
                project.DefaultWorktreeBranchPattern,
                chatId: Guid.NewGuid(),
                mode: "implementation");

            try
            {
                GitWorktreeCreateResult created = await gitWorkspaceService.CreateWorktreeAsync(
                    repositoryPath,
                    planId,
                    baseBranch,
                    branchName,
                    cancellationToken);

                WorkspaceCreateResult? workspace = await projectStore.CreateWorkspaceAsync(
                    project.Id,
                    created.Path,
                    created.Branch,
                    WorkspaceKind.Worktree,
                    created.Branch,
                    created.BaseBranch,
                    cancellationToken);

                if (workspace is null)
                {
                    return Result.Failure<(Guid, string)>(
                        Error.NotFound($"Project '{project.Id}' was not found."));
                }

                return Result.Success((workspace.Workspace.Id, workspace.Workspace.Path));
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure<(Guid, string)>(Error.Validation("Worktree.Create", ex.Message));
            }
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
                new Command(
                    parentChatId,
                    request.PlanId,
                    request.Title,
                    request.ContentMarkdown,
                    request.BaseBranch),
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
