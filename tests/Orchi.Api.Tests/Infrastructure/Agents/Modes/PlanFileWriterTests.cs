using Orchi.Api.Infrastructure.Agents.Plans;

namespace Orchi.Api.Tests.Infrastructure.Agents.Modes;

public class PlanFileWriterTests : IDisposable
{
    private readonly string _workspacePath;
    private readonly PlanFileWriter _writer = new();

    public PlanFileWriterTests()
    {
        _workspacePath = Path.Combine(Path.GetTempPath(), $"orchi-plan-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_workspacePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_workspacePath))
        {
            Directory.Delete(_workspacePath, recursive: true);
        }
    }

    [Fact]
    public async Task WritePlanAsync_CreatesOrchiDirectoryAndFile()
    {
        const string content = "# Auth refactor\n\nDo the thing.";

        string relativePath = await _writer.WritePlanAsync(_workspacePath, "auth-refactor", content);

        Assert.Equal(".orchi/plan-auth-refactor.md", relativePath);

        string fullPath = Path.Combine(_workspacePath, ".orchi", "plan-auth-refactor.md");
        Assert.True(File.Exists(fullPath));
        Assert.Equal(content, await File.ReadAllTextAsync(fullPath));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Bad Plan Id")]
    [InlineData("snake_case")]
    public void SanitizePlanId_RejectsInvalidIds(string planId)
    {
        Assert.Throws<ArgumentException>(() => PlanFileWriter.SanitizePlanId(planId));
    }

    [Fact]
    public void SanitizePlanId_AcceptsKebabCase()
    {
        string sanitized = PlanFileWriter.SanitizePlanId("auth-refactor-v2");

        Assert.Equal("auth-refactor-v2", sanitized);
    }
}
