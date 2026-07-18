using Orchi.Api.Entities;

namespace Orchi.Api.Infrastructure.Git.Hosting;

public interface IGitHostAdapterFactory
{
    IGitHostAdapter GetAdapter(GitHostProvider provider);

    IGitHostAdapter GetAdapter(string providerId);

    IReadOnlyList<IGitHostAdapter> ListAdapters();
}
