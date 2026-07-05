using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Orchestration;
using Orchi.Api.Infrastructure.Agents.Orchestration.Handlers;
using Orchi.Api.Infrastructure.Agents.Orchestration.Persistence;
using Orchi.Api.Infrastructure.Agents.Plans;

namespace Orchi.Api.Tests.Infrastructure.Agents.Orchestration;

public class SequentialAdvanceStepHandlerTests
{
    private readonly SequentialAdvanceStepHandler _handler = new();

    [Fact]
    public async Task HandleAsync_OnError_ReturnsPauseWorkflow()
    {
        var parent = CreateChat(Guid.NewGuid(), OrchestrationAgentModeStrategy.Mode);
        var implementation = CreateChat(Guid.NewGuid(), ImplementationAgentModeStrategy.Mode, parent.Id, ".orchi/plan-first.md");
        var workflow = new OrchestrationWorkflowRecord(
            parent.Id,
            OrchestrationWorkflowStatus.Running,
            ["first", "second"],
            NextSequenceIndex: 1,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        var context = new OrchestrationStepContext(
            parent.Id,
            parent,
            implementation,
            "first",
            Succeeded: false,
            workflow,
            [new PlanMarkdownParser.ParsedPlan("first", "First", "body")],
            [implementation]);

        OrchestrationStepResult? result = await _handler.HandleAsync(context, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Contains(
            result.Actions,
            action => action.Kind == OrchestrationStepActionKind.PauseWorkflow);
    }

    [Fact]
    public async Task HandleAsync_OnSuccess_ReturnsNextPlanKickoff()
    {
        var parent = CreateChat(Guid.NewGuid(), OrchestrationAgentModeStrategy.Mode);
        var implementation = CreateChat(Guid.NewGuid(), ImplementationAgentModeStrategy.Mode, parent.Id, ".orchi/plan-first.md");
        var workflow = new OrchestrationWorkflowRecord(
            parent.Id,
            OrchestrationWorkflowStatus.Running,
            ["first", "second"],
            NextSequenceIndex: 1,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        var context = new OrchestrationStepContext(
            parent.Id,
            parent,
            implementation,
            "first",
            Succeeded: true,
            workflow,
            [
                new PlanMarkdownParser.ParsedPlan("first", "First", "body"),
                new PlanMarkdownParser.ParsedPlan("second", "Second", "body")
            ],
            [implementation]);

        OrchestrationStepResult? result = await _handler.HandleAsync(context, CancellationToken.None);

        Assert.NotNull(result);
        OrchestrationStepAction action = Assert.Single(result.Actions);
        Assert.Equal(OrchestrationStepActionKind.KickOffNextPlan, action.Kind);
        Assert.Equal("second", action.Plan?.PlanId);
    }

    private static ChatSession CreateChat(
        Guid id,
        string mode,
        Guid? parentChatId = null,
        string? planFilePath = null) =>
        new()
        {
            Id = id,
            AgentId = "cursor",
            WorkspacePath = "/workspace",
            Mode = mode,
            ParentChatId = parentChatId,
            PlanFilePath = planFilePath
        };
}
