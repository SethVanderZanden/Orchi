using Orchi.Api.Common.Abstractions;
using Orchi.Api.Infrastructure.Agents;

namespace Orchi.Api.Features.Chats.ShutdownChats;

public static class ShutdownChats
{
    public sealed record Command : ICommand;

    internal sealed class Handler(AgentSessionManager sessionManager) : ICommandHandler<Command>
    {
        public async Task<Common.Results.Result> Handle(Command command, CancellationToken cancellationToken)
        {
            await sessionManager.CloseAllSessionsAsync(cancellationToken);
            return Common.Results.Result.Success();
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/chats/shutdown", Handle)
                .WithName("ShutdownChats")
                .WithTags("Chats");
        }

        private static async Task<IResult> Handle(
            ICommandHandler<Command> handler,
            CancellationToken cancellationToken)
        {
            Common.Results.Result result = await handler.Handle(new Command(), cancellationToken);
            return result.IsSuccess ? Results.NoContent() : Results.Problem(result.Error.Message);
        }
    }
}
