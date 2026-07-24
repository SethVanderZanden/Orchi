using Orchi.Api.Infrastructure.Agents.Workspace;

namespace Orchi.Api.Tests.Infrastructure.Agents.Workspace;

public class WorkspaceDiffStatsMarkdownFormatterTests
{
    [Fact]
    public void Format_IncludesMarkerHeadingTotalsAndFiles()
    {
        var stats = new WorkspaceDiffStats(
            [
                new WorkspaceDiffStatsEntry("src/foo.ts", 42, 7),
                new WorkspaceDiffStatsEntry("src/bar.ts", 10, 0),
            ],
            52,
            7,
            "git diff --numstat HEAD");

        string markdown = WorkspaceDiffStatsMarkdownFormatter.Format(stats);

        Assert.Contains(WorkspaceDiffStatsMarkdownFormatter.Marker, markdown);
        Assert.Contains("### Workspace changes", markdown);
        Assert.Contains("**Total (2 files)**", markdown);
        Assert.Contains("**+52**", markdown);
        Assert.Contains("**-7**", markdown);
        Assert.Contains("`src/foo.ts`", markdown);
        Assert.Contains("| +42 | -7 |", markdown);
        Assert.Contains("| +10 | -0 |", markdown);
        Assert.Contains("_git diff --numstat HEAD_", markdown);
    }
}
