using System.Text;

namespace Orchi.Api.Infrastructure.Agents.Workspace;

public static class WorkspaceDiffStatsMarkdownFormatter
{
    internal const string Marker = "<!-- orchi-workspace-diff-stats -->";

    public static string Format(WorkspaceDiffStats stats)
    {
        var builder = new StringBuilder();
        builder.AppendLine(Marker);
        builder.AppendLine();
        builder.AppendLine("### Workspace changes");
        builder.AppendLine();
        builder.AppendLine("| File | Added | Removed |");
        builder.AppendLine("| --- | ---: | ---: |");

        string totalLabel = stats.FileCount == 1
            ? "**Total (1 file)**"
            : $"**Total ({stats.FileCount} files)**";
        builder.AppendLine(
            $"| {totalLabel} | **+{stats.TotalAdded}** | **-{stats.TotalRemoved}** |");

        foreach (WorkspaceDiffStatsEntry file in stats.Files)
        {
            string path = EscapeTableCell(FormatPath(file.Path));
            builder.AppendLine($"| {path} | +{file.Added} | -{file.Removed} |");
        }

        builder.AppendLine();
        builder.AppendLine($"_{stats.Source}_");

        return builder.ToString().TrimEnd();
    }

    private static string FormatPath(string path) => $"`{path}`";

    private static string EscapeTableCell(string value) =>
        value.Replace("|", "\\|", StringComparison.Ordinal);
}
