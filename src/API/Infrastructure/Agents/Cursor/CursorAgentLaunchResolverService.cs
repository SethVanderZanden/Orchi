using Microsoft.Extensions.Options;
using Orchi.Api.Infrastructure.Agents.Cli;
using Orchi.Api.Infrastructure.Caching;

namespace Orchi.Api.Infrastructure.Agents.Cursor;

internal sealed class CursorAgentLaunchResolverService(
    IOptions<CursorAgentOptions> options,
    OrchiHybridCacheService cache) : IAgentLaunchResolver
{
    public string AgentId => AgentIds.Cursor;

    public async ValueTask<AgentLaunchResolveResult> ResolveAsync(CancellationToken cancellationToken)
    {
        CursorAgentOptions config = options.Value;
        string cacheKey = OrchiCacheKeys.CursorExecutable(BuildExecutableConfigFingerprint(config));

        CachedCursorExecutableResolution cached = await cache.GetOrCreateAsync(
            cacheKey,
            _ => ValueTask.FromResult(
                CachedCursorExecutableResolution.From(CursorAgentExecutableResolver.Resolve(config))),
            cache.CreateCursorExecutableEntryOptions(),
            cancellationToken);

        CursorAgentExecutableResolver.ResolveResult result = cached.ToResolveResult();
        if (!result.Success || result.Launch is null)
        {
            return new AgentLaunchResolveResult(false, null, result.ErrorMessage);
        }

        return new AgentLaunchResolveResult(true, result.Launch, null);
    }

    private static string BuildExecutableConfigFingerprint(CursorAgentOptions config)
    {
        var parts = new List<string> { config.Executable };

        if (config.AdditionalSearchPaths is { Length: > 0 })
        {
            parts.AddRange(config.AdditionalSearchPaths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Order(StringComparer.OrdinalIgnoreCase));
        }

        return string.Join('\u001f', parts);
    }
}
