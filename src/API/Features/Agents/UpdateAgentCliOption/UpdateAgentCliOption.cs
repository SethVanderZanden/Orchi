using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents.Models;

namespace Orchi.Api.Features.Agents.UpdateAgentCliOption;

public static class UpdateAgentCliOption
{
    public sealed record CliOptionResponse(
        string Kind,
        string Id,
        string Label,
        string CliValue,
        bool IsEnabled,
        string Source);

    public sealed record Command(
        string AgentId,
        string Kind,
        string OptionId,
        bool IsEnabled) : ICommand<CliOptionResponse>;

    internal sealed class Handler(IAgentCliOptionCatalogService catalogService)
        : ICommandHandler<Command, CliOptionResponse>
    {
        public async Task<Result<CliOptionResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            try
            {
                Result<AgentCliOptionDto> result = await catalogService.UpdateEnabledAsync(
                    command.AgentId,
                    command.Kind,
                    command.OptionId,
                    command.IsEnabled,
                    cancellationToken);

                if (result.IsFailure)
                {
                    return Result.Failure<CliOptionResponse>(result.Error);
                }

                AgentCliOptionDto option = result.Value;
                return Result.Success(new CliOptionResponse(
                    option.Kind,
                    option.Id,
                    option.Label,
                    option.CliValue,
                    option.IsEnabled,
                    option.Source));
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure<CliOptionResponse>(Error.Validation("Agent.Unsupported", ex.Message));
            }
            catch (ArgumentException ex)
            {
                return Result.Failure<CliOptionResponse>(Error.Validation("CliOption.Kind", ex.Message));
            }
        }
    }

    public sealed record Request(bool IsEnabled);

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPatch("/agents/{agentId}/cli-options/{kind}/{optionId}", Handle)
                .WithName("UpdateAgentCliOption")
                .WithTags("Agents")
                .Produces<CliOptionResponse>();
        }

        private static async Task<IResult> Handle(
            string agentId,
            string kind,
            string optionId,
            Request request,
            ICommandHandler<Command, CliOptionResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<CliOptionResponse> result = await handler.Handle(
                new Command(agentId, kind, optionId, request.IsEnabled),
                cancellationToken);

            return result.ToProblem();
        }
    }
}
