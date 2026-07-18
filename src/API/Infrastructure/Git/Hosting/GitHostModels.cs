namespace Orchi.Api.Infrastructure.Git.Hosting;

public enum GitHostReadinessStatus
{
    Ready = 0,
    MissingCli = 1,
    NotAuthenticated = 2,
    RepoNotDetected = 3
}

public sealed record GitHostReadiness(
    string ProviderId,
    GitHostReadinessStatus Status,
    string Message,
    string? RequiredCli = null);

public sealed record CreatePullRequestRequest(
    string WorkspacePath,
    string Title,
    string Body,
    string HeadBranch,
    string BaseBranch);

public sealed record CreatePullRequestResult(
    string Url,
    string? Id = null);

public sealed record GitHostProviderInfo(
    string ProviderId,
    string DisplayName,
    string RequiredCli,
    string ConfigureHint);
