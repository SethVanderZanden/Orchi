using System.Text.RegularExpressions;

namespace Orchi.Api.Infrastructure.Agents.Plans.Artifacts;

public sealed partial class OrchiArtifactFileStore
{
    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex PlanIdPattern();

    public static string SanitizePlanId(string planId)
    {
        if (string.IsNullOrWhiteSpace(planId))
        {
            throw new ArgumentException("Plan id is required.", nameof(planId));
        }

        string normalized = planId.Trim().ToLowerInvariant();
        if (!PlanIdPattern().IsMatch(normalized))
        {
            throw new ArgumentException(
                "Plan id must be kebab-case (lowercase letters, numbers, and hyphens).",
                nameof(planId));
        }

        return normalized;
    }

    public const string PlanSequenceRelativePath = ".orchi/plan-sequence.txt";

    public Task<string> WriteAsync(
        string workspacePath,
        string relativePath,
        string contentMarkdown,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string normalizedRelativePath = relativePath.Replace('\\', '/');
        string fullPath = ResolveFullPath(workspacePath, normalizedRelativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, contentMarkdown);

        return Task.FromResult(normalizedRelativePath);
    }

    public Task<string?> TryReadAsync(
        string workspacePath,
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string normalizedRelativePath = relativePath.Replace('\\', '/');
        string fullPath = ResolveFullPath(workspacePath, normalizedRelativePath);

        if (!File.Exists(fullPath))
        {
            return Task.FromResult<string?>(null);
        }

        return Task.FromResult<string?>(File.ReadAllText(fullPath));
    }

    public Task<bool> TryDeleteAsync(
        string workspacePath,
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string normalizedRelativePath = relativePath.Replace('\\', '/');
        string fullPath = ResolveFullPath(workspacePath, normalizedRelativePath);

        if (!File.Exists(fullPath))
        {
            return Task.FromResult(false);
        }

        try
        {
            File.Delete(fullPath);
            return Task.FromResult(true);
        }
        catch (IOException)
        {
            return Task.FromResult(false);
        }
        catch (UnauthorizedAccessException)
        {
            return Task.FromResult(false);
        }
    }

    private static string ResolveFullPath(string workspacePath, string normalizedRelativePath) =>
        Path.Combine(workspacePath, normalizedRelativePath.Replace('/', Path.DirectorySeparatorChar));
}
