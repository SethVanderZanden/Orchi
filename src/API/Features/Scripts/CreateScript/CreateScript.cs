using FluentValidation;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Scripts.Shared;
using Orchi.Api.Infrastructure.Scripts;

namespace Orchi.Api.Features.Scripts.CreateScript;

public static class CreateScript
{
    public sealed record Command(
        string Name,
        Guid? ProjectId,
        string StepsJson,
        IReadOnlyList<ScriptBindingRequest>? Bindings) : ICommand<ScriptResponse>;

    internal sealed class Handler(IScriptStore store)
        : ICommandHandler<Command, ScriptResponse>
    {
        public async Task<Result<ScriptResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            if (!ScriptStepsSerializer.TryValidate(command.StepsJson, out string? stepsError))
            {
                return Result.Failure<ScriptResponse>(Error.Validation("Script.Steps", stepsError!));
            }

            ScriptUpsertBinding[] bindings;
            try
            {
                bindings = ScriptMapper.ToUpsertBindings(command.Bindings);
            }
            catch (ArgumentException ex)
            {
                return Result.Failure<ScriptResponse>(Error.Validation("Script.Binding", ex.Message));
            }

            try
            {
                StoredScript created = await store.CreateAsync(
                    command.Name,
                    command.ProjectId,
                    command.StepsJson,
                    bindings,
                    cancellationToken);

                return Result.Success(ScriptMapper.ToResponse(created));
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure<ScriptResponse>(Error.NotFound(ex.Message));
            }
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(command => command.Name)
                .NotEmpty()
                .MaximumLength(128)
                .WithMessage("Name is required (max 128 characters).");

            RuleFor(command => command.StepsJson)
                .NotEmpty()
                .MaximumLength(32_000)
                .WithMessage("Steps JSON is required.");
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/scripts", Handle)
                .WithName("CreateScript")
                .WithTags("Scripts")
                .Produces<ScriptResponse>(StatusCodes.Status201Created);
        }

        private static async Task<IResult> Handle(
            CreateScriptRequest request,
            ICommandHandler<Command, ScriptResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<ScriptResponse> result = await handler.Handle(
                new Command(request.Name, request.ProjectId, request.StepsJson, request.Bindings),
                cancellationToken);

            if (result.IsFailure)
            {
                return result.ToProblem();
            }

            ScriptResponse response = result.Value;
            return Results.Created($"/scripts/{response.Id}", response);
        }
    }
}
