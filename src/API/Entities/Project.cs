namespace Orchi.Api.Entities;

public class Project
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    /// <summary>Default base branch for worktrees and PRs (e.g. main, staging).</summary>
    public string DefaultBaseBranch { get; set; } = "main";

    /// <summary>
    /// Branch name pattern for new worktrees. Tokens: {date}, {time}, {shortId}, {chatId}, {mode}.
    /// </summary>
    public string DefaultWorktreeBranchPattern { get; set; } = "orchi/{date}-{shortId}";

    public GitHostProvider GitHostProvider { get; set; } = GitHostProvider.GitHub;

    /// <summary>When true, plan kickoff provisions an isolated git worktree workspace.</summary>
    public bool UseWorktreeOnKickoff { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public List<Workspace> Workspaces { get; set; } = [];
}
