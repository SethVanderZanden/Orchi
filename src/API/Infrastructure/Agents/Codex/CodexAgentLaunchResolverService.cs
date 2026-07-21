using Orchi.Api.Infrastructure.Agents.Cli;

namespace Orchi.Api.Infrastructure.Agents.Codex;

internal sealed class CodexAgentLaunchResolverService : IAgentLaunchResolver
{
    private readonly CodexAgentOptions _options;

    public CodexAgentLaunchResolverService(Microsoft.Extensions.Options.IOptions<CodexAgentOptions> options)
    {
        _options = options.Value;
    }

    public string AgentId => AgentIds.Codex;

    public ValueTask<AgentLaunchResolveResult> ResolveAsync(CancellationToken cancellationToken)
    {
        CodexAgentExecutableResolver.ResolveResult result =
            CodexAgentExecutableResolver.Resolve(_options);

        AgentLaunchResolveResult mapped = result.Success && result.Launch is not null
            ? new AgentLaunchResolveResult(true, ToSharedLaunchSpec(result.Launch), null)
            : new AgentLaunchResolveResult(false, null, result.ErrorMessage);

        return ValueTask.FromResult(mapped);
    }

    private static AgentLaunchSpec ToSharedLaunchSpec(CodexAgentLaunchSpec launch) =>
        new(launch.ExecutablePath, launch.EntryScript);
}
