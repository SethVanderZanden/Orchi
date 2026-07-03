using Orchi.Api.Infrastructure.Agents.Plans.Artifacts;

namespace Orchi.Api.Tests.Infrastructure.Agents.Modes;

public class OrchiArtifactWriterStrategyTests : IDisposable
{
    private readonly string _workspacePath;
    private readonly OrchiArtifactFileStore _fileStore = new();
    private readonly ImplementationPlanWriterStrategy _planStrategy;
    private readonly ReviewBriefWriterStrategy _reviewStrategy;
    private readonly OrchiArtifactWriterFactory _factory;

    public OrchiArtifactWriterStrategyTests()
    {
        _workspacePath = Path.Combine(Path.GetTempPath(), $"orchi-artifact-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_workspacePath);
        _planStrategy = new ImplementationPlanWriterStrategy(_fileStore);
        _reviewStrategy = new ReviewBriefWriterStrategy(_fileStore);
        _factory = new OrchiArtifactWriterFactory([_planStrategy, _reviewStrategy]);
    }

    public void Dispose()
    {
        if (Directory.Exists(_workspacePath))
        {
            Directory.Delete(_workspacePath, recursive: true);
        }
    }

    [Fact]
    public async Task PlanStrategy_WriteAsync_CreatesPlanFile()
    {
        const string content = "# Auth refactor\n\nDo the thing.";

        string relativePath = await _planStrategy.WriteAsync(_workspacePath, "auth-refactor", content);

        Assert.Equal(".orchi/plan-auth-refactor.md", relativePath);

        string fullPath = Path.Combine(_workspacePath, ".orchi", "plan-auth-refactor.md");
        Assert.True(File.Exists(fullPath));
        Assert.Equal(content, await File.ReadAllTextAsync(fullPath));
    }

    [Fact]
    public async Task ReviewStrategy_WriteAsync_CreatesReviewFile()
    {
        const string content = "# Review brief\n\nReview the auth refactor.";

        string relativePath = await _reviewStrategy.WriteAsync(_workspacePath, "auth-refactor", content);

        Assert.Equal(".orchi/review-auth-refactor.md", relativePath);

        string fullPath = Path.Combine(_workspacePath, ".orchi", "review-auth-refactor.md");
        Assert.True(File.Exists(fullPath));
        Assert.Equal(content, await File.ReadAllTextAsync(fullPath));
    }

    [Fact]
    public void Factory_GetStrategy_ResolvesPlanAndReview()
    {
        Assert.Equal(OrchiArtifactKind.Plan, _factory.GetStrategy(OrchiArtifactKind.Plan).Kind);
        Assert.Equal(OrchiArtifactKind.Review, _factory.GetStrategy(OrchiArtifactKind.Review).Kind);
    }

    [Fact]
    public void Factory_TryGetStrategyFromPath_ResolvesFromRelativePath()
    {
        Assert.Equal(OrchiArtifactKind.Plan, _factory.TryGetStrategyFromPath(".orchi/plan-auth-refactor.md")?.Kind);
        Assert.Equal(OrchiArtifactKind.Review, _factory.TryGetStrategyFromPath(".orchi/review-auth-refactor.md")?.Kind);
        Assert.Null(_factory.TryGetStrategyFromPath(".orchi/other.md"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Bad Plan Id")]
    [InlineData("snake_case")]
    public void SanitizePlanId_RejectsInvalidIds(string planId)
    {
        Assert.Throws<ArgumentException>(() => OrchiArtifactFileStore.SanitizePlanId(planId));
    }

    [Fact]
    public void SanitizePlanId_AcceptsKebabCase()
    {
        string sanitized = OrchiArtifactFileStore.SanitizePlanId("auth-refactor-v2");

        Assert.Equal("auth-refactor-v2", sanitized);
    }
}
