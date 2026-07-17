using FluentValidation;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents.Models;

namespace Orchi.Api.Features.Agents.AddAgentCliOption;

public static class AddAgentCliOption
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
        string Label,
        string? CliValue) : ICommand<CliOptionResponse>;

    internal sealed class Handler(IAgentCliOptionCatalogService catalogService)
        : ICommandHandler<Command, CliOptionResponse>
    {
        public async Task<Result<CliOptionResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            try
            {
                Result<AgentCliOptionDto> result = await catalogService.AddManualAsync(
                    command.AgentId,
                    command.Kind,
                    command.OptionId,
                    command.Label,
                    command.CliValue,
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

    public sealed record Request(string OptionId, string? Label, string? CliValue);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(command => command.OptionId)
                .NotEmpty()
                .WithMessage("Option id is required.");
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/agents/{agentId}/cli-options/{kind}", Handle)
                .WithName("AddAgentCliOption")
                .WithTags("Agents")
                .Produces<CliOptionResponse>(StatusCodes.Status201Created);
        }

        private static async Task<IResult> Handle(
            string agentId,
            string kind,
            Request request,
            ICommandHandler<Command, CliOptionResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<CliOptionResponse> result = await handler.Handle(
                new Command(
                    agentId,
                    kind,
                    request.OptionId,
                    request.Label ?? request.OptionId,
                    request.CliValue),
                cancellationToken);

            if (result.IsSuccess)
            {
                CliOptionResponse response = result.Value;
                return Results.Created(
                    $"/agents/{agentId}/cli-options/{Uri.EscapeDataString(kind)}/{Uri.EscapeDataString(response.Id)}",
                    response);
            }

            return result.ToProblem();
        }
    }
}
