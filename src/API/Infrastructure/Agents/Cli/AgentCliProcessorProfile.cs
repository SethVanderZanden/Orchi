namespace Orchi.Api.Infrastructure.Agents.Cli;

internal sealed class AgentCliProcessorProfile(
    string agentId,
    string displayName,
    int timeoutSeconds,
    bool surfaceStderrWhenNoParsedEvents,
    bool useWindowsCmdShim,
    IAgentLaunchResolver launchResolver,
    IAgentCliArgumentBuilder argumentBuilder,
    Func<IAgentStreamLineParser> lineParserFactory) : IAgentCliProcessorProfile
{
    public string AgentId { get; } = agentId;
    public string DisplayName { get; } = displayName;
    public int TimeoutSeconds { get; } = timeoutSeconds;
    public bool SurfaceStderrWhenNoParsedEvents { get; } = surfaceStderrWhenNoParsedEvents;
    public bool UseWindowsCmdShim { get; } = useWindowsCmdShim;
    public IAgentLaunchResolver LaunchResolver { get; } = launchResolver;
    public IAgentCliArgumentBuilder ArgumentBuilder { get; } = argumentBuilder;

    public IAgentStreamLineParser CreateLineParser() => lineParserFactory();
}
