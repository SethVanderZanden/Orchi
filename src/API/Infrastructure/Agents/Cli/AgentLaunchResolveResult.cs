namespace Orchi.Api.Infrastructure.Agents.Cli;

public sealed record AgentLaunchResolveResult(
    bool Success,
    AgentLaunchSpec? Launch,
    string? ErrorMessage);
