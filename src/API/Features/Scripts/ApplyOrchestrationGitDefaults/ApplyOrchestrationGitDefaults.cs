using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Entities;
using Orchi.Api.Features.Scripts.Shared;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Scripts;

namespace Orchi.Api.Features.Scripts.ApplyOrchestrationGitDefaults;

public static class ApplyOrchestrationGitDefaults
{
    public sealed record Command(Guid? ProjectId) : ICommand<IReadOnlyList<ScriptResponse>>;

    internal sealed class Handler(IScriptStore store) : ICommandHandler<Command, IReadOnlyList<ScriptResponse>>
    {
        public async Task<Result<IReadOnlyList<ScriptResponse>>> Handle(
            Command command,
            CancellationToken cancellationToken)
        {
            string worktreeStepsJson = ScriptStepsSerializer.Serialize(
            [
                new ScriptStepDto(ScriptStepKinds.GitWorktree)
            ]);

            StoredScript worktreeOnStart = await store.CreateAsync(
                "Orchestration: worktree on agent start",
                command.ProjectId,
                worktreeStepsJson,
                [
                    new ScriptUpsertBinding(
                        ScriptEventKind.AgentStart,
                        ModeFilter: null,
                        Order: 0,
                        Enabled: true,
                        ScriptOnError.Continue)
                ],
                cancellationToken);

            string finishStepsJson = ScriptStepsSerializer.Serialize(
            [
                new ScriptStepDto(ScriptStepKinds.GitCommit, GenerateMessage: true),
                new ScriptStepDto(ScriptStepKinds.GitPush, SetUpstream: true),
                new ScriptStepDto(ScriptStepKinds.GitCreatePullRequest)
            ]);

            StoredScript finishFlow = await store.CreateAsync(
                "Orchestration: commit, push, PR",
                command.ProjectId,
                finishStepsJson,
                [
                    new ScriptUpsertBinding(
                        ScriptEventKind.AgentFinish,
                        AgentModeIds.Implementation,
                        Order: 0,
                        Enabled: true,
                        ScriptOnError.Continue)
                ],
                cancellationToken);

            return Result.Success<IReadOnlyList<ScriptResponse>>(
            [
                ScriptMapper.ToResponse(worktreeOnStart),
                ScriptMapper.ToResponse(finishFlow)
            ]);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/scripts/templates/orchestration-git-defaults", Handle)
                .WithName("ApplyOrchestrationGitDefaults")
                .WithTags("Scripts")
                .Produces<IReadOnlyList<ScriptResponse>>(StatusCodes.Status201Created);
        }

        private static async Task<IResult> Handle(
            ApplyRequest? request,
            ICommandHandler<Command, IReadOnlyList<ScriptResponse>> handler,
            CancellationToken cancellationToken)
        {
            Result<IReadOnlyList<ScriptResponse>> result = await handler.Handle(
                new Command(request?.ProjectId),
                cancellationToken);

            if (result.IsFailure)
            {
                return result.ToProblem();
            }

            return Results.Created("/scripts", result.Value);
        }
    }

    public sealed record ApplyRequest(Guid? ProjectId = null);
}
