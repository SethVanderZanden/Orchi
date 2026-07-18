using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Scripts;

namespace Orchi.Api.Features.Scripts.DeleteScript;

public static class DeleteScript
{
    public sealed record Command(string Id) : ICommand;

    internal sealed class Handler(IScriptStore store) : ICommandHandler<Command>
    {
        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            bool deleted = await store.DeleteAsync(command.Id, cancellationToken);
            if (!deleted)
            {
                return Result.Failure(Error.NotFound($"Script '{command.Id}' was not found."));
            }

            return Result.Success();
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("/scripts/{id}", Handle)
                .WithName("DeleteScript")
                .WithTags("Scripts")
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