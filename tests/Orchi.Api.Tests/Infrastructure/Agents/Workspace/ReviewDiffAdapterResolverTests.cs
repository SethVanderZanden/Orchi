using Orchi.Api.Infrastructure.Agents.Modes.Prompt;
using Orchi.Api.Infrastructure.Agents.Plans;
using Orchi.Api.Infrastructure.Agents.Workspace;
using Orchi.Api.Tests.Infrastructure.Agents.Modes;

namespace Orchi.Api.Tests.Infrastructure.Agents.Workspace;

public class ReviewDiffAdapterResolverTests : IDisposable
{
    private readonly string _workspacePath;

    public ReviewDiffAdapterResolverTests()
    {
        _workspacePath = Path.Combine(Path.GetTempPath(), $"orchi-review-diff-adapter-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_workspacePath);
        Directory.CreateDirectory(Path.Combine(_workspacePath, ".orchi"));
    }

    public void Dispose()
    {
        if (!Directory.Exists(_workspacePath))
        {
            return;
        }

        try
        {
            Directory.Delete(_workspacePath, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    [Fact]
    public void Resolve_WhenBranchMarkerPresent_UsesBranchPairAdapter()
    {
        string reviewPath = ".orchi/review-branch-feature.md";
        File.WriteAllText(
            Path.Combine(_workspacePath, ".orchi", "review-branch-feature.md"),
            ReviewBriefBuilder.BuildForBranchReview("branch-feature", "feature", "main"));

        var provider = new FakeWorkspaceDiffProvider
        {
            Diff = "workspace-head-diff",
            BranchDiff = "branch-pair-diff",
        };

        var resolver = new ReviewDiffAdapterResolver([
            new BranchPairReviewDiffAdapter(provider),
            new WorkspaceHeadReviewDiffAdapter(provider),
        ]);

        ReviewDiffPayload? payload = resolver.Resolve(new PromptBuildContext
        {
            ModeId = "review",
            UserContent = "Begin review.",
            WorkspacePath = _workspacePath,
            PlanFilePath = reviewPath,
        });

        Assert.NotNull(payload);
        Assert.Contains("main...feature", payload.Intro);
        Assert.Contains("branch-pair-diff", payload.Diff);
        Assert.DoesNotContain("workspace-head-diff", payload.Diff);
    }

    [Fact]
    public void Resolve_WhenNoBranchMarker_UsesWorkspaceHeadAdapter()
    {
        string reviewPath = ".orchi/review-auth.md";
        File.WriteAllText(
            Path.Combine(_workspacePath, ".orchi", "review-auth.md"),
            ReviewBriefBuilder.Build("auth", "# plan", Guid.NewGuid(), Guid.NewGuid()));

        var provider = new FakeWorkspaceDiffProvider
        {
            Diff = "workspace-head-diff",
            BranchDiff = "branch-pair-diff",
        };

        var resolver = new ReviewDiffAdapterResolver([
            new BranchPairReviewDiffAdapter(provider),
            new WorkspaceHeadReviewDiffAdapter(provider),
        ]);

        ReviewDiffPayload? payload = resolver.Resolve(new PromptBuildContext
        {
            ModeId = "review",
            UserContent = "Begin review.",
            WorkspacePath = _workspacePath,
            PlanFilePath = reviewPath,
        });

        Assert.NotNull(payload);
        Assert.Contains("Implementation changes", payload.Intro);
        Assert.Equal("workspace-head-diff", payload.Diff);
    }

    [Fact]
    public void Resolve_WhenNotReviewPath_ReturnsNull()
    {
        var resolver = new ReviewDiffAdapterResolver([
            new BranchPairReviewDiffAdapter(new FakeWorkspaceDiffProvider()),
            new WorkspaceHeadReviewDiffAdapter(new FakeWorkspaceDiffProvider()),
        ]);

        ReviewDiffPayload? payload = resolver.Resolve(new PromptBuildContext
        {
            ModeId = "review",
            UserContent = "Begin review.",
            WorkspacePath = _workspacePath,
            PlanFilePath = ".orchi/plan-auth.md",
        });

        Assert.Null(payload);
    }
}
