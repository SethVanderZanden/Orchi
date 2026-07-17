using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents.Models;

namespace Orchi.Api.Features.Agents.RemoveAgentModel;

public static class RemoveAgentModel
{
    public sealed record Command(string AgentId, string ModelId) : ICommand;

    internal sealed class Handler(IAgentModelCatalogService catalogService)
        : ICommandHandler<Command>
    {
        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            try
            {
                return await catalogService.RemoveAsync(
                    command.AgentId,
                    command.ModelId,
                    cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure(Error.Validation("Agent.Unsupported", ex.Message));
            }
        }
    }

    public sealed record Request(string ModelId);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(command => command.AgentId)
                .NotEmpty()
                .WithMessage("Agent id is required.");

            RuleFor(command => command.ModelId)
                .NotEmpty()
                .WithMessage("Model id is required.");
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            // Body carries modelId so values with spaces/ANSI/brackets are not mangled in the path.
            app.MapPost("/agents/{agentId}/models/remove", Handle)
                .WithName("RemoveAgentModel")
                .WithTags("Agents");
        }

        private static async Task<IResult> Handle(
            string agentId,
            [FromBody] Request request,
            ICommandHandler<Command> handler,
            CancellationToken cancellationToken)
        {
            Result result = await handler.Handle(
                new Command(agentId, request.ModelId),
                cancellationToken);

            return result.IsSuccess ? Results.NoContent() : result.ToProblem();
        }
    }
}
