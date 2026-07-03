using System.Text.RegularExpressions;

namespace Orchi.Api.Infrastructure.Agents.Plans;

public sealed partial class PlanFileWriter : IPlanFileWriter
{
    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex PlanIdPattern();

    public Task<string> WritePlanAsync(
        string workspacePath,
        string planId,
        string contentMarkdown,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string sanitizedPlanId = SanitizePlanId(planId);
        string relativePath = $".orchi/plan-{sanitizedPlanId}.md";
        string fullDirectory = Path.Combine(workspacePath, ".orchi");
        string fullPath = Path.Combine(workspacePath, relativePath);

        Directory.CreateDirectory(fullDirectory);
        File.WriteAllText(fullPath, contentMarkdown);

        return Task.FromResult(relativePath.Replace('\\', '/'));
    }

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
}
