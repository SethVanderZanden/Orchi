using Orchi.Api.Entities;

namespace Orchi.Api.Infrastructure.Git.Hosting;

public sealed class GitHostingFacade(IGitHostAdapterFactory adapterFactory) : IGitHostingFacade
{
    public IReadOnlyList<GitHostProviderInfo> ListProviders() =>
        adapterFactory.ListAdapters()
            .Select(adapter => new GitHostProviderInfo(
                adapter.ProviderId,
                adapter.DisplayName,
                adapter.RequiredCli,
                adapter.ConfigureHint))
            .ToArray();

    public async Task<GitHostReadiness> GetReadinessAsync(
        GitHostProvider provider,
        string? workspacePath,
        CancellationToken cancellationToken)
    {
        IGitHostAdapter adapter = adapterFactory.GetAdapter(provider);
        return await adapter.GetReadinessAsync(workspacePath, cancellationToken);
    }

    public async Task<CreatePullRequestResult> CreatePullRequestAsync(
        GitHostProvider provider,
        CreatePullRequestRequest request,
        CancellationToken cancellationToken)
    {
        IGitHostAdapter adapter = adapterFactory.GetAdapter(provider);
        GitHostReadiness readiness = await adapter.GetReadinessAsync(request.WorkspacePath, cancellationToken);
        if (readiness.Status != GitHostReadinessStatus.Ready)
        {
            throw new InvalidOperationException(
                $"{adapter.DisplayName} is not ready: {readiness.Message} {adapter.ConfigureHint}");
        }

        return await adapter.CreatePullRequestAsync(request, cancellationToken);
    }
}
