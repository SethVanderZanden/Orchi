using Orchi.Api.Infrastructure.Cli;

namespace Orchi.Api.Infrastructure.Git.Hosting;

public sealed class AzureDevOpsHostAdapter(IProcessRunner processRunner) : IGitHostAdapter
{
    public string ProviderId => GitHostProviderIds.AzureDevOps;

    public string DisplayName => "Azure DevOps";

    public string RequiredCli => "az";

    public string ConfigureHint =>
        "Install Azure CLI with the Azure DevOps extension, then run `az login` and `az devops configure --defaults`.";

    public async Task<GitHostReadiness> GetReadinessAsync(
        string? workspacePath,
        CancellationToken cancellationToken)
    {
        ProcessRunResult version = await processRunner.RunAsync(
            "az",
            ["--version"],
            workspacePath,
            cancellationToken,
            timeoutMs: 15_000);

        if (!version.Succeeded)
        {
            return new GitHostReadiness(
                ProviderId,
                GitHostReadinessStatus.MissingCli,
                "Azure CLI (az) was not found on PATH.",
                RequiredCli);
        }

        ProcessRunResult account = await processRunner.RunAsync(
            "az",
            ["account", "show"],
            workspacePath,
            cancellationToken,
            timeoutMs: 20_000);

        if (!account.Succeeded)
        {
            return new GitHostReadiness(
                ProviderId,
                GitHostReadinessStatus.NotAuthenticated,
                "Azure CLI is installed but not authenticated. Run `az login`.",
                RequiredCli);
        }

        ProcessRunResult devops = await processRunner.RunAsync(
            "az",
            ["devops", "project", "show"],
            workspacePath,
            cancellationToken,
            timeoutMs: 20_000);

        if (!devops.Succeeded)
        {
            return new GitHostReadiness(
                ProviderId,
                GitHostReadinessStatus.RepoNotDetected,
                "Azure DevOps defaults are not configured for this workspace. Run `az devops configure --defaults`.",
                RequiredCli);
        }

        return new GitHostReadiness(
            ProviderId,
            GitHostReadinessStatus.Ready,
            "Azure DevOps CLI is ready.",
            RequiredCli);
    }

    public async Task<CreatePullRequestResult> CreatePullRequestAsync(
        CreatePullRequestRequest request,
        CancellationToken cancellationToken)
    {
        var args = new List<string>
        {
            "repos",
            "pr",
            "create",
            "--title",
            request.Title,
            "--description",
            request.Body,
            "--source-branch",
            request.HeadBranch,
            "--target-branch",
            request.BaseBranch,
            "--output",
            "json"
        };

        ProcessRunResult result = await processRunner.RunAsync(
            "az",
            args,
            request.WorkspacePath,
            cancellationToken,
            timeoutMs: 60_000);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"az repos pr create failed: {result.CombinedOutput}");
        }

        string output = result.StdOut.Trim();
        string? url = TryExtractUrl(output);
        if (string.IsNullOrWhiteSpace(url))
        {
            url = output;
        }

        return new CreatePullRequestResult(url);
    }

    private static string? TryExtractUrl(string json)
    {
        const string key = "\"url\":";
        int index = json.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return null;
        }

        int start = json.IndexOf('"', index + key.Length);
        if (start < 0)
        {
            return null;
        }

        int end = json.IndexOf('"', start + 1);
        if (end < 0)
        {
            return null;
        }

        return json[(start + 1)..end];
    }
}
