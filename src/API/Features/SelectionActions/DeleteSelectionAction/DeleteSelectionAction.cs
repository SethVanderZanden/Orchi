using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.SelectionActions;

namespace Orchi.Api.Features.SelectionActions.DeleteSelectionAction;

public static class DeleteSelectionAction
{
    public sealed record Command(string Id) : ICommand;

    internal sealed class Handler(ISelectionActionStore store) : ICommandHandler<Command>
    {
        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            bool deleted = await store.DeleteAsync(command.Id, cancellationToken);
            if (!deleted)
            {
                return Result.Failure(Error.NotFound("Selection action not found."));
            }

            return Result.Success();
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("/selection-actions/{id}", Handle)
                .WithName("DeleteSelectionAction")
                .WithTags("SelectionActions")
                .Produces(StatusCodes.Status204NoContent);
        }

        private static async Task<IResult> Handle(
            string id,
            ICommandHandler<Command> handler,
            CancellationToken cancellationToken)
        {
            Result result = await handler.Handle(new Command(id), cancellationToken);
            if (result.IsFailure)
            {
                return result.ToProblem();
            }

            return Results.NoContent();
        }
    }
}
