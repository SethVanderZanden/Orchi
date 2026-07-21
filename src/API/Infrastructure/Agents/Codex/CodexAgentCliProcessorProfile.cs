using Microsoft.Extensions.Options;
using Orchi.Api.Infrastructure.Agents.Cli;

namespace Orchi.Api.Infrastructure.Agents.Codex;

internal sealed class CodexAgentCliProcessorProfile(
    CodexAgentLaunchResolverService launchResolver,
    CodexCliArgumentBuilder argumentBuilder,
    IOptions<CodexAgentOptions> options) : IAgentCliProcessorProfile
{
    public string AgentId => AgentIds.Codex;
    public string DisplayName => "Codex";
    public int TimeoutSeconds => options.Value.TimeoutSeconds;
    public bool SurfaceStderrWhenNoParsedEvents => true;
    public bool UseWindowsCmdShim => true;
    public IAgentLaunchResolver LaunchResolver => launchResolver;
    public IAgentCliArgumentBuilder ArgumentBuilder => argumentBuilder;

    public IAgentStreamLineParser CreateLineParser() => new CodexNdjsonParser();
}
