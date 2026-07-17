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

    public Task<string> WriteAsync(
        string workspacePath,
        string relativePath,
        string contentMarkdown,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string normalizedRelativePath = relativePath.Replace('\\', '/');
        string fullDirectory = Path.Combine(workspacePath, ".orchi");
        string fullPath = Path.Combine(workspacePath, normalizedRelativePath.Replace('/', Path.DirectorySeparatorChar));

        Directory.CreateDirectory(fullDirectory);
        File.WriteAllText(fullPath, contentMarkdown);

        return Task.FromResult(normalizedRelativePath);
    }
}
