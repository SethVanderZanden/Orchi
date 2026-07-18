using Orchi.Api.Infrastructure.Cli;

namespace Orchi.Api.Infrastructure.Git.Hosting;

public sealed class GitHubHostAdapter(IProcessRunner processRunner) : IGitHostAdapter
{
    public string ProviderId => GitHostProviderIds.GitHub;

    public string DisplayName => "GitHub";

    public string RequiredCli => "gh";

    public string ConfigureHint => "Install GitHub CLI and run `gh auth login`.";

    public async Task<GitHostReadiness> GetReadinessAsync(
        string? workspacePath,
        CancellationToken cancellationToken)
    {
        ProcessRunResult version = await processRunner.RunAsync(
            "gh",
            ["--version"],
            workspacePath,
            cancellationToken,
            timeoutMs: 10_000);

        if (!version.Succeeded)
        {
            return new GitHostReadiness(
                ProviderId,
                GitHostReadinessStatus.MissingCli,
                "GitHub CLI (gh) was not found on PATH.",
                RequiredCli);
        }

        ProcessRunResult auth = await processRunner.RunAsync(
            "gh",
            ["auth", "status"],
            workspacePath,
            cancellationToken,
            timeoutMs: 15_000);

        if (!auth.Succeeded)
        {
            return new GitHostReadiness(
                ProviderId,
                GitHostReadinessStatus.NotAuthenticated,
                "GitHub CLI is installed but not authenticated. Run `gh auth login`.",
                RequiredCli);
        }

        if (!string.IsNullOrWhiteSpace(workspacePath))
        {
            ProcessRunResult repo = await processRunner.RunAsync(
                "gh",
                ["repo", "view", "--json", "nameWithOwner", "-q", ".nameWithOwner"],
                workspacePath,
                cancellationToken,
                timeoutMs: 15_000);

            if (!repo.Succeeded || string.IsNullOrWhiteSpace(repo.StdOut))
            {
                return new GitHostReadiness(
                    ProviderId,
                    GitHostReadinessStatus.RepoNotDetected,
                    "Could not detect a GitHub repository for this workspace.",
                    RequiredCli);
            }
        }

        return new GitHostReadiness(ProviderId, GitHostReadinessStatus.Ready, "GitHub CLI is ready.", RequiredCli);
    }

    public async Task<CreatePullRequestResult> CreatePullRequestAsync(
        CreatePullRequestRequest request,
        CancellationToken cancellationToken)
    {
        var args = new List<string>
        {
            "pr",
            "create",
            "--title",
            request.Title,
            "--body",
            request.Body,
            "--base",
            request.BaseBranch,
            "--head",
            request.HeadBranch
        };

        ProcessRunResult result = await processRunner.RunAsync(
            "gh",
            args,
            request.WorkspacePath,
            cancellationToken,
            timeoutMs: 60_000);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"gh pr create failed: {result.CombinedOutput}");
        }

        string url = result.StdOut.Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException("gh pr create succeeded but returned no URL.");
        }

        return new CreatePullRequestResult(url);
    }
}
