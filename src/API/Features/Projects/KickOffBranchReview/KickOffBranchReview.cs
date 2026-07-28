using FluentValidation;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Entities;
using Orchi.Api.Features.Projects.Shared;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Plans;
using Orchi.Api.Infrastructure.Agents.Plans.Artifacts;
using Orchi.Api.Infrastructure.Git.Workspace;
using Orchi.Api.Infrastructure.Projects;

namespace Orchi.Api.Features.Projects.KickOffBranchReview;

public static class KickOffBranchReview
{
    public sealed record Command(
        Guid ProjectId,
        string HeadBranch,
        string? BaseBranch,
        bool Fetch,
        Guid? WorkspaceId) : ICommand<KickOffBranchReviewResponse>;

    internal sealed class Handler(
        IProjectStore projectStore,
        IGitWorkspaceService gitWorkspaceService,
        IOrchiArtifactWriterFactory artifactWriterFactory,
        AgentSessionManager sessionManager)
        : ICommandHandler<Command, KickOffBranchReviewResponse>
    {
        public async Task<Result<KickOffBranchReviewResponse>> Handle(
            Command command,
            CancellationToken cancellationToken)
        {
            Project? project = await projectStore.GetProjectAsync(command.ProjectId, cancellationToken);
            if (project is null)
            {
                return Result.Failure<KickOffBranchReviewResponse>(
                    Error.NotFound($"Project '{command.ProjectId}' was not found."));
            }

            Workspace? primary = project.Workspaces.FirstOrDefault(workspace => workspace.IsDefault)
                ?? project.Workspaces.FirstOrDefault();

            if (primary is null)
            {
                return Result.Failure<KickOffBranchReviewResponse>(
                    Error.Validation("Workspace.Missing", "Project has no workspace."));
            }

            Workspace? reviewWorkspace = primary;
            if (command.WorkspaceId is Guid requestedWorkspaceId)
            {
                reviewWorkspace = project.Workspaces.FirstOrDefault(workspace => workspace.Id == requestedWorkspaceId);
                if (reviewWorkspace is null)
                {
                    return Result.Failure<KickOffBranchReviewResponse>(
                        Error.Validation(
                            "Workspace.NotFound",
                            $"Workspace '{requestedWorkspaceId}' was not found for this project."));
                }
            }

            string headBranch = command.HeadBranch.Trim();
            string baseBranch = string.IsNullOrWhiteSpace(command.BaseBranch)
                ? project.DefaultBaseBranch
                : command.BaseBranch.Trim();

            if (string.Equals(headBranch, baseBranch, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure<KickOffBranchReviewResponse>(
                    Error.Validation(
                        "Branch.Same",
                        "Head branch and base branch must be different."));
            }

            try
            {
                if (command.Fetch)
                {
                    await gitWorkspaceService.FetchAsync(reviewWorkspace.Path, cancellationToken);
                }

                string reviewId = ReviewBriefBuilder.ToBranchReviewId(headBranch);

                string reviewBrief = ReviewBriefBuilder.BuildForBranchReview(
                    reviewId,
                    headBranch,
                    baseBranch);

                IOrchiArtifactWriterStrategy reviewWriter =
                    artifactWriterFactory.GetStrategy(OrchiArtifactKind.Review);

                string reviewFilePath = await reviewWriter.WriteAsync(
                    reviewWorkspace.Path,
                    reviewId,
                    reviewBrief,
                    cancellationToken);

                Result<ChatSession> reviewChatResult = await sessionManager.CreateSessionAsync(
                    reviewWorkspace.Id,
                    mode: ReviewAgentModeStrategy.Mode,
                    planFilePath: reviewFilePath,
                    cancellationToken: cancellationToken);

                if (reviewChatResult.IsFailure)
                {
                    return Result.Failure<KickOffBranchReviewResponse>(reviewChatResult.Error);
                }

                ChatSession reviewChat = reviewChatResult.Value;
                string initialPrompt = ReviewPlanTask.Build(reviewFilePath);
                const string kickoffMessage = "Begin review.";

                return Result.Success(new KickOffBranchReviewResponse(
                    reviewChat.Id,
                    reviewFilePath,
                    headBranch,
                    baseBranch,
                    initialPrompt,
                    kickoffMessage));
            }
            catch (ArgumentException ex)
            {
                return Result.Failure<KickOffBranchReviewResponse>(
                    Error.Validation("ReviewId.Invalid", ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure<KickOffBranchReviewResponse>(
                    Error.Validation("BranchReview.Create", ex.Message));
            }
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(command => command.ProjectId)
                .NotEmpty()
                .WithMessage("Project id is required.");

            RuleFor(command => command.HeadBranch)
                .NotEmpty()
                .WithMessage("Head branch is required.");
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/projects/{projectId:guid}/reviews/from-branches", Handle)
                .WithName("KickOffBranchReview")
                .WithTags("Projects", "Chats")
                .Produces<KickOffBranchReviewResponse>(StatusCodes.Status201Created);
        }

        private static async Task<IResult> Handle(
            Guid projectId,
            KickOffBranchReviewRequest request,
            ICommandHandler<Command, KickOffBranchReviewResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<KickOffBranchReviewResponse> result = await handler.Handle(
                new Command(
                    projectId,
                    request.HeadBranch,
                    request.BaseBranch,
                    request.Fetch ?? true,
                    request.WorkspaceId),
                cancellationToken);

            if (result.IsSuccess)
            {
                KickOffBranchReviewResponse response = result.Value;
                return Results.Created($"/chats/{response.ReviewChatId}", response);
            }

            return result.ToProblem();
        }
    }
}
