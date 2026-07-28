using FluentValidation;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents;

namespace Orchi.Api.Features.Chats.BulkCloseChats;

public static class BulkCloseChats
{
    public sealed record Request(IReadOnlyList<Guid>? ChatIds);

    public sealed record Command(IReadOnlyList<Guid> ChatIds) : ICommand;

    internal sealed class Handler(AgentSessionManager sessionManager) : ICommandHandler<Command>
    {
        public Task<Result> Handle(Command command, CancellationToken cancellationToken) =>
            sessionManager.CloseSessionsAsync(command.ChatIds, cancellationToken);
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(command => command.ChatIds)
                .NotEmpty()
                .WithMessage("At least one chat id is required.");

            RuleForEach(command => command.ChatIds)
                .NotEmpty()
                .WithMessage("Chat id must not be empty.");
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/chats/bulk-delete", Handle)
                .WithName("BulkCloseChats")
                .WithTags("Chats")
                .Produces(StatusCodes.Status204NoContent);
        }

        private static async Task<IResult> Handle(
            Request request,
            ICommandHandler<Command> handler,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<Guid> chatIds = request.ChatIds ?? Array.Empty<Guid>();
            Result result = await handler.Handle(new Command(chatIds), cancellationToken);
            return result.ToProblem();
        }
    }
}
