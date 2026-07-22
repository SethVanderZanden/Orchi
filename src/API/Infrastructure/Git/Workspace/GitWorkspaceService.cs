using System.Text.RegularExpressions;
using Orchi.Api.Infrastructure.Cli;

namespace Orchi.Api.Infrastructure.Git.Workspace;

public sealed partial class GitWorkspaceService(IProcessRunner processRunner) : IGitWorkspaceService
{
    public async Task<bool> IsGitRepositoryAsync(string workspacePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(workspacePath) || !Directory.Exists(workspacePath))
        {
            return false;
        }

        ProcessRunResult result = await RunGitAsync(
            workspacePath,
            ["rev-parse", "--is-inside-work-tree"],
            cancellationToken);

        return result.Succeeded
            && string.Equals(result.StdOut.Trim(), "true", StringComparison.OrdinalIgnoreCase);
    }

    public async Task FetchAsync(string workspacePath, CancellationToken cancellationToken)
    {
        if (!await IsGitRepositoryAsync(workspacePath, cancellationToken))
        {
            throw new InvalidOperationException($"Not a git repository: {workspacePath}");
        }

        // Best-effort: offline remotes should not block local branch listing.
        _ = await RunGitAsync(workspacePath, ["fetch", "--prune", "--all"], cancellationToken);
    }

    public async Task<IReadOnlyList<GitBranchInfo>> ListBranchesAsync(
        string workspacePath,
        CancellationToken cancellationToken,
        bool includeRemotes = true)
    {
        var args = new List<string> { "branch" };
        if (includeRemotes)
        {
            args.Add("--all");
        }

        args.Add("--format=%(refname)%09%(refname:short)%09%(HEAD)");

        ProcessRunResult result = await RunGitAsync(workspacePath, args, cancellationToken);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(result.CombinedOutput);
        }

        var branches = new List<GitBranchInfo>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string line in result.StdOut.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string[] parts = line.Split('\t');
            if (parts.Length < 2)
            {
                continue;
            }

            string fullRef = parts[0];
            string name = parts[1];
            if (string.Equals(name, "HEAD", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith("/HEAD", StringComparison.OrdinalIgnoreCase) ||
                fullRef.EndsWith("/HEAD", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!seen.Add(name))
            {
                continue;
            }

            bool isCurrent = parts.Length > 2 && parts[2].Contains('*', StringComparison.Ordinal);
            bool isRemote = fullRef.StartsWith("refs/remotes/", StringComparison.OrdinalIgnoreCase);

            branches.Add(new GitBranchInfo(name, isCurrent, isRemote));
        }

        return branches
            .OrderBy(branch => branch.IsRemote)
            .ThenBy(branch => branch.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<string?> GetCurrentBranchAsync(string workspacePath, CancellationToken cancellationToken)
    {
        ProcessRunResult result = await RunGitAsync(
            workspacePath,
            ["branch", "--show-current"],
            cancellationToken);

        if (!result.Succeeded)
        {
            return null;
        }

        string branch = result.StdOut.Trim();
        return string.IsNullOrWhiteSpace(branch) ? null : branch;
    }

    public async Task<string?> ResolveBranchRefAsync(
        string workspacePath,
        string branchName,
        CancellationToken cancellationToken)
    {
        string trimmed = branchName.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return null;
        }

        ProcessRunResult local = await RunGitAsync(
            workspacePath,
            ["rev-parse", "--verify", "--quiet", $"refs/heads/{trimmed}"],
            cancellationToken);
        if (local.Succeeded)
        {
            return trimmed;
        }

        ProcessRunResult asIs = await RunGitAsync(
            workspacePath,
            ["rev-parse", "--verify", "--quiet", trimmed],
            cancellationToken);
        if (asIs.Succeeded)
        {
            return trimmed;
        }

        ProcessRunResult remote = await RunGitAsync(
            workspacePath,
            ["rev-parse", "--verify", "--quiet", $"refs/remotes/{trimmed}"],
            cancellationToken);
        if (remote.Succeeded)
        {
            return trimmed;
        }

        // Allow bare name that only exists as origin/<name>
        ProcessRunResult origin = await RunGitAsync(
            workspacePath,
            ["rev-parse", "--verify", "--quiet", $"refs/remotes/origin/{trimmed}"],
            cancellationToken);
        if (origin.Succeeded)
        {
            return $"origin/{trimmed}";
        }

        return null;
    }

    public async Task CommitAsync(string workspacePath, string message, CancellationToken cancellationToken)
    {
        ProcessRunResult status = await RunGitAsync(workspacePath, ["status", "--porcelain"], cancellationToken);
        if (!status.Succeeded)
        {
            throw new InvalidOperationException(status.CombinedOutput);
        }

        if (string.IsNullOrWhiteSpace(status.StdOut))
        {
            return;
        }

        ProcessRunResult add = await RunGitAsync(workspacePath, ["add", "-A"], cancellationToken);
        EnsureSuccess(add, "git add");

        ProcessRunResult commit = await RunGitAsync(
            workspacePath,
            ["commit", "-m", message],
            cancellationToken);
        EnsureSuccess(commit, "git commit");
    }

    public async Task PushAsync(string workspacePath, bool setUpstream, CancellationToken cancellationToken)
    {
        string? branch = await GetCurrentBranchAsync(workspacePath, cancellationToken);
        var args = new List<string> { "push" };
        if (setUpstream && !string.IsNullOrWhiteSpace(branch))
        {
            args.Add("-u");
            args.Add("origin");
            args.Add(branch);
        }

        ProcessRunResult result = await RunGitAsync(workspacePath, args, cancellationToken);
        EnsureSuccess(result, "git push");
    }

    public async Task MergeAsync(
        string workspacePath,
        string sourceBranch,
        string targetBranch,
        CancellationToken cancellationToken)
    {
        ProcessRunResult checkout = await RunGitAsync(workspacePath, ["checkout", targetBranch], cancellationToken);
        EnsureSuccess(checkout, $"git checkout {targetBranch}");

        ProcessRunResult merge = await RunGitAsync(
            workspacePath,
            ["merge", "--no-ff", sourceBranch, "-m", $"Merge branch '{sourceBranch}' into {targetBranch}"],
            cancellationToken);
        EnsureSuccess(merge, $"git merge {sourceBranch}");
    }

    public async Task<GitWorktreeCreateResult> CreateWorktreeAsync(
        string repositoryPath,
        string planId,
        string baseBranch,
        string? branchName,
        CancellationToken cancellationToken)
    {
        if (!await IsGitRepositoryAsync(repositoryPath, cancellationToken))
        {
            throw new InvalidOperationException($"Not a git repository: {repositoryPath}");
        }

        string safePlanId = SanitizeSegment(planId);
        string branch = string.IsNullOrWhiteSpace(branchName)
            ? $"orchi/{safePlanId}"
            : branchName.Trim();

        string worktreesRoot = Path.Combine(repositoryPath, ".orchi", "worktrees");
        Directory.CreateDirectory(worktreesRoot);
        string worktreePath = Path.Combine(worktreesRoot, safePlanId);

        if (Directory.Exists(worktreePath))
        {
            throw new InvalidOperationException($"Worktree path already exists: {worktreePath}");
        }

        ProcessRunResult fetch = await RunGitAsync(repositoryPath, ["fetch", "origin", baseBranch], cancellationToken);
        _ = fetch; // Best-effort; local base branch may still exist.

        ProcessRunResult create = await RunGitAsync(
            repositoryPath,
            ["worktree", "add", "-b", branch, worktreePath, baseBranch],
            cancellationToken);

        if (!create.Succeeded)
        {
            ProcessRunResult retry = await RunGitAsync(
                repositoryPath,
                ["worktree", "add", worktreePath, branch],
                cancellationToken);
            EnsureSuccess(retry, "git worktree add");
        }

        return new GitWorktreeCreateResult(worktreePath, branch, baseBranch);
    }

    public async Task<GitWorktreeCreateResult> CreateWorktreeForExistingBranchAsync(
        string repositoryPath,
        string worktreeId,
        string headBranch,
        string baseBranch,
        CancellationToken cancellationToken)
    {
        if (!await IsGitRepositoryAsync(repositoryPath, cancellationToken))
        {
            throw new InvalidOperationException($"Not a git repository: {repositoryPath}");
        }

        string? headRef = await ResolveBranchRefAsync(repositoryPath, headBranch, cancellationToken);
        if (headRef is null)
        {
            throw new InvalidOperationException($"Branch '{headBranch}' was not found.");
        }

        string? baseRef = await ResolveBranchRefAsync(repositoryPath, baseBranch, cancellationToken);
        if (baseRef is null)
        {
            throw new InvalidOperationException($"Base branch '{baseBranch}' was not found.");
        }

        string safeId = SanitizeSegment(worktreeId);
        string worktreesRoot = Path.Combine(repositoryPath, ".orchi", "worktrees");
        Directory.CreateDirectory(worktreesRoot);
        string worktreePath = Path.Combine(worktreesRoot, safeId);

        if (Directory.Exists(worktreePath))
        {
            throw new InvalidOperationException($"Worktree path already exists: {worktreePath}");
        }

        // Prefer attaching an existing local branch; otherwise create a local review branch from the ref.
        ProcessRunResult attachLocal = await RunGitAsync(
            repositoryPath,
            ["worktree", "add", worktreePath, headRef],
            cancellationToken);

        if (!attachLocal.Succeeded)
        {
            string localReviewBranch = $"orchi/review-{safeId}";
            ProcessRunResult createFromRef = await RunGitAsync(
                repositoryPath,
                ["worktree", "add", "-b", localReviewBranch, worktreePath, headRef],
                cancellationToken);
            EnsureSuccess(createFromRef, "git worktree add");
            return new GitWorktreeCreateResult(worktreePath, localReviewBranch, baseRef);
        }

        string? checkedOut = await GetCurrentBranchAsync(worktreePath, cancellationToken);
        return new GitWorktreeCreateResult(worktreePath, checkedOut ?? headRef, baseRef);
    }

    public async Task<string> GetStatusPorcelainAsync(string workspacePath, CancellationToken cancellationToken)
    {
        ProcessRunResult result = await RunGitAsync(workspacePath, ["status", "--porcelain"], cancellationToken);
        EnsureSuccess(result, "git status");
        return result.StdOut.Trim();
    }

    private async Task<ProcessRunResult> RunGitAsync(
        string workspacePath,
        IReadOnlyList<string> args,
        CancellationToken cancellationToken) =>
        await processRunner.RunAsync("git", args, workspacePath, cancellationToken);

    private static void EnsureSuccess(ProcessRunResult result, string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        throw new InvalidOperationException($"{operation} failed: {result.CombinedOutput}");
    }

    private static string SanitizeSegment(string value)
    {
        string trimmed = value.Trim();
        string sanitized = InvalidPathChars().Replace(trimmed, "-");
        return string.IsNullOrWhiteSpace(sanitized) ? Guid.NewGuid().ToString("N")[..8] : sanitized;
    }

    [GeneratedRegex(@"[^a-zA-Z0-9._-]+")]
    private static partial Regex InvalidPathChars();
}
