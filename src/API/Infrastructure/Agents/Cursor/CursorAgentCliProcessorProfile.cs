using Microsoft.Extensions.Options;
using Orchi.Api.Infrastructure.Agents.Cli;

namespace Orchi.Api.Infrastructure.Agents.Cursor;

internal sealed class CursorAgentCliProcessorProfile(
    CursorAgentLaunchResolverService launchResolver,
    CursorCliArgumentBuilder argumentBuilder,
    IOptions<CursorAgentOptions> options) : IAgentCliProcessorProfile
{
    public string AgentId => AgentIds.Cursor;
    public string DisplayName => "Cursor";
    public int TimeoutSeconds => options.Value.TimeoutSeconds;
    public bool SurfaceStderrWhenNoParsedEvents => false;
    public bool UseWindowsCmdShim => false;
    public IAgentLaunchResolver LaunchResolver => launchResolver;
    public IAgentCliArgumentBuilder ArgumentBuilder => argumentBuilder;

    public IAgentStreamLineParser CreateLineParser() => new CursorNdjsonParser();
}
