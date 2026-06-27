namespace Orchi.Api.Infrastructure.Agents;

internal sealed class AgentSessionShutdownService(AgentSessionManager sessionManager) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
    {
        sessionManager.CloseAllSessions();
        return Task.CompletedTask;
    }
}
