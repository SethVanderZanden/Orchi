namespace Orchi.Api.Infrastructure.Agents;

internal sealed class AgentSessionShutdownService(AgentSessionManager sessionManager) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await sessionManager.CloseAllSessionsAsync(cancellationToken);
        }
        catch (ObjectDisposedException)
        {
        }
    }
}
