using Orchi.Api.Infrastructure.Git.Workspace;

namespace Orchi.Api.Tests.Infrastructure.Git.Workspace;

public class GitWorktreePathResolverTests
{
    [Fact]
    public void ResolveWorktreePath_UsesShortExternalRoot()
    {
        string repoRoot = Path.Combine(Path.GetTempPath(), "orchi-deep", "nested", "repo");

        string path = GitWorktreePathResolver.ResolveWorktreePath(repoRoot, "feature-auth");

        string expectedRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Orchi",
            "worktrees");

        Assert.StartsWith(expectedRoot, path);
        Assert.EndsWith($"{Path.DirectorySeparatorChar}feature-auth", path);
        Assert.DoesNotContain(".orchi", path);
        Assert.True(path.Length < 200, $"Worktree path should stay short but was {path.Length} chars.");
    }

    [Fact]
    public void ResolveWorktreePath_HashesLongSegmentIds()
    {
        string repoRoot = Path.Combine(Path.GetTempPath(), $"orchi-repo-{Guid.NewGuid():N}");
        string longId = new('a', GitWorktreePathResolver.MaxSegmentLength + 10);

        string path = GitWorktreePathResolver.ResolveWorktreePath(repoRoot, longId);
        string segment = Path.GetFileName(path);

        Assert.Equal(12, segment.Length);
        Assert.DoesNotContain("aaaa", segment);
    }

    [Fact]
    public void NewOpaqueWorktreeSegmentId_ReturnsTwelveCharHex()
    {
        string id = GitWorktreePathResolver.NewOpaqueWorktreeSegmentId();

        Assert.Equal(12, id.Length);
        Assert.Matches("^[0-9a-f]{12}$", id);
    }
}
