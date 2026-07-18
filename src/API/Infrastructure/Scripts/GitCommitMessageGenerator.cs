using Orchi.Api.Infrastructure.Cli;

namespace Orchi.Api.Infrastructure.Scripts;

public interface IGitCommitMessageGenerator
{
    Task<string?> GenerateAsync(string workspacePath, CancellationToken cancellationToken);
}

/// <summary>
/// Builds a concise commit message from the working tree diff summary.
/// Keeps message generation off the agent turn path to avoid recursive script hooks.
/// </summary>
public sealed class GitCommitMessageGenerator(IProcessRunner processRunner) : IGitCommitMessageGenerator
{
    public async Task<string?> GenerateAsync(string workspacePath, CancellationToken cancellationToken)
    {
        ProcessRunResult status = await processRunner.RunAsync(
            "git",
            ["status", "--porcelain"],
            workspacePath,
            cancellationToken);

        if (!status.Succeeded || string.IsNullOrWhiteSpace(status.StdOut))
        {
            return null;
        }

        string[] lines = status.StdOut.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        int fileCount = lines.Length;
        string firstPath = lines[0].Length > 3 ? lines[0][3..].Trim() : "workspace";

        ProcessRunResult shortStat = await processRunner.RunAsync(
            "git",
            ["diff", "--stat"],
            workspacePath,
            cancellationToken);

        string summary = shortStat.Succeeded && !string.IsNullOrWhiteSpace(shortStat.StdOut)
            ? shortStat.StdOut.Trim().Split('\n').LastOrDefault()?.Trim() ?? $"{fileCount} files"
            : $"{fileCount} files";

        return $"chore: update {firstPath} ({summary})";
    }
}
