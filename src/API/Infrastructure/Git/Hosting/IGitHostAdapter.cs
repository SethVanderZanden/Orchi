namespace Orchi.Api.Infrastructure.Git.Hosting;

public interface IGitHostAdapter
{
    string ProviderId { get; }

    string DisplayName { get; }

    string RequiredCli { get; }

    string ConfigureHint { get; }

    Task<GitHostReadiness> GetReadinessAsync(string? workspacePath, CancellationToken cancellationToken);

    Task<CreatePullRequestResult> CreatePullRequestAsync(
        CreatePullRequestRequest request,
        CancellationToken cancellationToken);
}
