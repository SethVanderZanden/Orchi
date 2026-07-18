using Orchi.Api.Entities;

namespace Orchi.Api.Infrastructure.Git.Hosting;

public interface IGitHostingFacade
{
    IReadOnlyList<GitHostProviderInfo> ListProviders();

    Task<GitHostReadiness> GetReadinessAsync(
        GitHostProvider provider,
        string? workspacePath,
        CancellationToken cancellationToken);

    Task<CreatePullRequestResult> CreatePullRequestAsync(
        GitHostProvider provider,
        CreatePullRequestRequest request,
        CancellationToken cancellationToken);
}
