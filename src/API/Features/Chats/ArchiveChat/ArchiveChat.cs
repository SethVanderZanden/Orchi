using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents;

namespace Orchi.Api.Features.Chats.ArchiveChat;

public static class ArchiveChat
{
    public sealed record Command(Guid ChatId) : ICommand;

    internal sealed class Handler(AgentSessionManager sessionManager) : ICommandHandler<Command>
    {
        public Task<Result> Handle(Command command, CancellationToken cancellationToken) =>
            sessionManager.ArchiveSessionAsync(command.ChatId, cancellationToken);
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/chats/{chatId:guid}/archive", Handle)
                .WithName("ArchiveChat")
                .WithTags("Chats");
        }

        private static async Task<IResult> Handle(
            Guid chatId,
            ICommandHandler<Command> handler,
            CancellationToken cancellationToken)
        {
            Result result = await handler.Handle(new Command(chatId), cancellationToken);
            if (result.IsSuccess)
            {
                return Results.NoContent();
            }

            return result.ToProblem();
        }
    }
}
