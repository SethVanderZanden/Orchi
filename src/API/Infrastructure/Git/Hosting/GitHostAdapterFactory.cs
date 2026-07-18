using Orchi.Api.Entities;

namespace Orchi.Api.Infrastructure.Git.Hosting;

public sealed class GitHostAdapterFactory(IEnumerable<IGitHostAdapter> adapters) : IGitHostAdapterFactory
{
    private readonly Dictionary<string, IGitHostAdapter> _byId =
        adapters.ToDictionary(adapter => adapter.ProviderId, StringComparer.OrdinalIgnoreCase);

    public IGitHostAdapter GetAdapter(GitHostProvider provider) =>
        GetAdapter(provider switch
        {
            GitHostProvider.GitHub => GitHostProviderIds.GitHub,
            GitHostProvider.AzureDevOps => GitHostProviderIds.AzureDevOps,
            _ => GitHostProviderIds.GitHub
        });

    public IGitHostAdapter GetAdapter(string providerId)
    {
        if (_byId.TryGetValue(providerId, out IGitHostAdapter? adapter))
        {
            return adapter;
        }

        throw new InvalidOperationException($"No git host adapter registered for '{providerId}'.");
    }

    public IReadOnlyList<IGitHostAdapter> ListAdapters() => _byId.Values.ToArray();
}
