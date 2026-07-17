using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents.Models;

namespace Orchi.Api.Features.Agents.RemoveAgentCliOption;

public static class RemoveAgentCliOption
{
    public sealed record Command(string AgentId, string Kind, string OptionId) : ICommand;

    internal sealed class Handler(IAgentCliOptionCatalogService catalogService)
        : ICommandHandler<Command>
    {
        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            try
            {
                return await catalogService.RemoveAsync(
                    command.AgentId,
                    command.Kind,
                    command.OptionId,
                    cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure(Error.Validation("Agent.Unsupported", ex.Message));
            }
            catch (ArgumentException ex)
            {
                return Result.Failure(Error.Validation("CliOption.Kind", ex.Message));
            }
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("/agents/{agentId}/cli-options/{kind}/{optionId}", Handle)
                .WithName("RemoveAgentCliOption")
                .WithTags("Agents")
                .Produces(StatusCodes.Status204NoContent);
        }

        private static async Task<IResult> Handle(
            string agentId,
            string kind,
            string optionId,
            ICommandHandler<Command> handler,
            CancellationToken cancellationToken)
        {
            Result result = await handler.Handle(new Command(agentId, kind, optionId), cancellationToken);
            if (result.IsSuccess)
            {
                return Results.NoContent();
            }

            return result.ToProblem();
        }
    }
}
