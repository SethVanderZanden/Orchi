using FluentValidation;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Scripts.Shared;
using Orchi.Api.Infrastructure.Scripts;

namespace Orchi.Api.Features.Scripts.UpdateScript;

public static class UpdateScript
{
    public sealed record Command(
        string Id,
        string Name,
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

            StoredScript? updated = await store.UpdateAsync(
                command.Id,
                command.Name,
                command.StepsJson,
                bindings,
                cancellationToken);

            if (updated is null)
            {
                return Result.Failure<ScriptResponse>(Error.NotFound($"Script '{command.Id}' was not found."));
            }

            return Result.Success(ScriptMapper.ToResponse(updated));
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(command => command.Id).NotEmpty();
            RuleFor(command => command.Name)
                .NotEmpty()
                .MaximumLength(128);
            RuleFor(command => command.StepsJson)
                .NotEmpty()
                .MaximumLength(32_000);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("/scripts/{id}", Handle)
                .WithName("UpdateScript")
                .WithTags("Scripts")
                .Produces<ScriptResponse>();
        }

        private static async Task<IResult> Handle(
            string id,
            UpdateScriptRequest request,
            ICommandHandler<Command, ScriptResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<ScriptResponse> result = await handler.Handle(
                new Command(id, request.Name, request.StepsJson, request.Bindings),
                cancellationToken);

            return result.ToProblem();
        }
    }
}
