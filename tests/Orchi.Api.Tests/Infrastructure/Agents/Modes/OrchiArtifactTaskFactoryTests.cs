using Orchi.Api.Infrastructure.Agents.Plans.Artifacts;

namespace Orchi.Api.Tests.Infrastructure.Agents.Modes;

public class OrchiArtifactTaskFactoryTests
{
    private readonly OrchiArtifactTaskFactory _factory;

    public OrchiArtifactTaskFactoryTests()
    {
        var fileStore = new OrchiArtifactFileStore();
        var writerFactory = new OrchiArtifactWriterFactory([
            new ImplementationPlanWriterStrategy(fileStore),
            new ReviewBriefWriterStrategy(fileStore)
        ]);

        _factory = new OrchiArtifactTaskFactory([
            new ImplementationPlanTaskStrategy(),
            new ReviewPlanTaskStrategy()
        ], writerFactory);
    }

    [Fact]
    public void ResolveTaskFromPath_PlanPath_ReturnsImplementationTask()
    {
        string? task = _factory.ResolveTaskFromPath(".orchi/plan-auth-refactor.md");

        Assert.NotNull(task);
        Assert.Contains("Implement the plan at `.orchi/plan-auth-refactor.md`", task);
        Assert.Contains("delete `.orchi/plan-auth-refactor.md`", task);
    }

    [Fact]
    public void ResolveTaskFromPath_ReviewPath_ReturnsReviewTask()
    {
        string? task = _factory.ResolveTaskFromPath(".orchi/review-auth-refactor.md");

        Assert.NotNull(task);
        Assert.Contains("Review `.orchi/review-auth-refactor.md`", task);
        Assert.Contains("delete `.orchi/review-auth-refactor.md`", task);
    }

    [Fact]
    public void ResolveTaskFromPath_ReviewPath_MentionsGitDiffInContext()
    {
        string? task = _factory.ResolveTaskFromPath(".orchi/review-auth-refactor.md");

        Assert.NotNull(task);
        Assert.Contains("git diff", task, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveTaskFromPath_UnknownPath_ReturnsNull()
    {
        Assert.Null(_factory.ResolveTaskFromPath(".orchi/other.md"));
        Assert.Null(_factory.ResolveTaskFromPath(null));
    }
}
