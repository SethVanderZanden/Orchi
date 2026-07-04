using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents.Models;

namespace Orchi.Api.Features.Agents.SyncAgentModels;

public static class SyncAgentModels
{
    public sealed record ModelResponse(
        string Id,
        string Label,
        bool IsDefault,
        bool IsCurrent,
        bool IsEnabled,
        string Source);

    public sealed record Response(
        IReadOnlyList<ModelResponse> Models,
        DateTimeOffset SyncedAt);

    public sealed record Command(string AgentId) : ICommand<Response>;

    internal sealed class Handler(IAgentModelCatalogService catalogService)
        : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command command, CancellationToken cancellationToken)
        {
            try
            {
                Result<AgentModelSyncResult> result = await catalogService.SyncAsync(
                    command.AgentId,
                    cancellationToken);

                if (result.IsFailure)
                {
                    return Result.Failure<Response>(result.Error);
                }

                AgentModelSyncResult syncResult = result.Value;
                return Result.Success(new Response(
                    syncResult.Models.Select(ToResponse).ToArray(),
                    syncResult.SyncedAt));
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure<Response>(Error.Validation("Agent.Unsupported", ex.Message));
            }
            catch (ArgumentException ex)
            {
                return Result.Failure<Response>(Error.Validation("Agent.Required", ex.Message));
            }
        }

        private static ModelResponse ToResponse(AgentModelDto model) =>
            new(model.Id, model.Label, model.IsDefault, model.IsCurrent, model.IsEnabled, model.Source);
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/agents/{agentId}/models/sync", Handle)
                .WithName("SyncAgentModels")
                .WithTags("Agents")
                .Produces<Response>();
        }

        private static async Task<IResult> Handle(
            string agentId,
            ICommandHandler<Command, Response> handler,
            CancellationToken cancellationToken)
        {
            Result<Response> result = await handler.Handle(new Command(agentId), cancellationToken);
            return result.ToProblem();
        }
    }
}
