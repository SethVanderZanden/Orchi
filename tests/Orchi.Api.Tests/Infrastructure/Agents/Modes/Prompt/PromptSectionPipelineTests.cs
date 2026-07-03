using Orchi.Api.Infrastructure.Agents.Modes.Prompt;
using Orchi.Api.Tests.Infrastructure.Agents.Modes;

namespace Orchi.Api.Tests.Infrastructure.Agents.Modes.Prompt;

public class PromptSectionPipelineTests
{
    private readonly PromptSectionPipeline _pipeline = PromptTestHelpers.CreatePipeline();

    [Fact]
    public void Build_MergesGlobalRulesIntoDocument()
    {
        var context = new PromptBuildContext
        {
            ModeId = "default",
            UserContent = "hi",
            WorkspacePath = "/workspace",
        };

        OrchiPromptDocument document = _pipeline.Build(context);

        Assert.Contains(GlobalPromptRules.MetaRule.Trim(), document.Rules);
        Assert.Equal("hi", document.Message);
    }

    [Fact]
    public void Build_AddsWorkspaceToContext()
    {
        var context = new PromptBuildContext
        {
            ModeId = "default",
            UserContent = "hi",
            WorkspacePath = "/path/to/project",
        };

        OrchiPromptDocument document = _pipeline.Build(context);

        Assert.Contains("Workspace: /path/to/project", document.Context);
    }

    [Fact]
    public void Build_AddsTaskWhenPlanFilePathPresent()
    {
        var context = new PromptBuildContext
        {
            ModeId = "default",
            UserContent = "go",
            WorkspacePath = "/workspace",
            PlanFilePath = ".orchi/plan-auth.md",
        };

        OrchiPromptDocument document = _pipeline.Build(context);

        Assert.Contains("Implement the plan at `.orchi/plan-auth.md`", document.Task);
    }
}
