using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents;

namespace Orchi.Api.Features.Chats.CloseChat;

public static class CloseChat
{
    public sealed record Command(Guid ChatId) : ICommand;

    internal sealed class Handler(AgentSessionManager sessionManager) : ICommandHandler<Command>
    {
        public Task<Result> Handle(Command command, CancellationToken cancellationToken) =>
            Task.FromResult(sessionManager.CloseSession(command.ChatId));
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("/chats/{chatId:guid}", Handle)
                .WithName("CloseChat")
                .WithTags("Chats");
        }

        private static async Task<IResult> Handle(
            Guid chatId,
            ICommandHandler<Command> handler,
            CancellationToken cancellationToken)
        {
            Result result = await handler.Handle(new Command(chatId), cancellationToken);
            return result.ToProblem();
        }
    }
}
