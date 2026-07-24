namespace Orchi.Api.Infrastructure.Agents.Workspace;

public sealed record WorkspaceDiffStatsEntry(string Path, int Added, int Removed);

public sealed record WorkspaceDiffStats(
    IReadOnlyList<WorkspaceDiffStatsEntry> Files,
    int TotalAdded,
    int TotalRemoved,
    string Source)
{
    public int FileCount => Files.Count;
}
