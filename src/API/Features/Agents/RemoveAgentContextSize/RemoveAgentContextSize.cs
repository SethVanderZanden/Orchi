using FluentValidation;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents.Models;

namespace Orchi.Api.Features.Agents.RemoveAgentContextSize;

public static class RemoveAgentContextSize
{
    public sealed record Command(string AgentId, string SizeId) : ICommand;

    internal sealed class Handler(IAgentContextSizeCatalogService catalogService)
        : ICommandHandler<Command>
    {
        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            try
            {
                return await catalogService.RemoveAsync(command.AgentId, command.SizeId, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure(Error.Validation("Agent.Unsupported", ex.Message));
            }
            catch (ArgumentException ex)
            {
                return Result.Failure(Error.Validation("Agent.Required", ex.Message));
            }
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(command => command.SizeId)
                .NotEmpty()
                .WithMessage("Context size id is required.");
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("/agents/{agentId}/context-sizes/{sizeId}", Handle)
                .WithName("RemoveAgentContextSize")
                .WithTags("Agents")
                .Produces(StatusCodes.Status204NoContent);
        }

        private static async Task<IResult> Handle(
            string agentId,
            string sizeId,
            ICommandHandler<Command> handler,
            CancellationToken cancellationToken)
        {
            Result result = await handler.Handle(new Command(agentId, sizeId), cancellationToken);

            if (result.IsSuccess)
            {
                return Results.NoContent();
            }

            return result.ToProblem();
        }
    }
}
