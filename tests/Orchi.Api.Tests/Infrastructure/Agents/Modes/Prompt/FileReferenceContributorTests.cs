using Orchi.Api.Infrastructure.Agents.Modes.Prompt;

namespace Orchi.Api.Tests.Infrastructure.Agents.Modes.Prompt;

public class FileReferenceContributorTests
{
    private readonly FileReferenceContributor _contributor = new();

    [Fact]
    public void Contribute_AddsFileReferenceRuleWhenWorkspacePresent()
    {
        var document = new OrchiPromptDocument();
        var context = new PromptBuildContext
        {
            ModeId = "default",
            UserContent = "hi",
            WorkspacePath = "/workspace/project",
        };

        _contributor.Contribute(context, document);

        Assert.Contains("<orchi-open-editor>", document.Rules);
        Assert.Contains("code /workspace/project -g {relativePath}:{line}", document.Rules);
        Assert.Contains("Do not substitute a primary/main checkout", document.Rules);
    }

    [Fact]
    public void Contribute_OmitsRuleWhenWorkspaceMissing()
    {
        var document = new OrchiPromptDocument();
        var context = new PromptBuildContext
        {
            ModeId = "default",
            UserContent = "hi",
            WorkspacePath = null!,
        };

        _contributor.Contribute(context, document);

        Assert.Null(document.Rules);
    }
}
