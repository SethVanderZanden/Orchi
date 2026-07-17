using FluentValidation;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Entities;
using Orchi.Api.Infrastructure.UserPreferences;

namespace Orchi.Api.Features.UserPreferences.UpdateUserPreferences;

public static class UpdateUserPreferences
{
    public sealed record Command(
        PostMessageBehavior? PostMessageBehavior,
        IReadOnlyList<string>? EnabledAgentIds) : ICommand<Response>;

    public sealed record Response(
        PostMessageBehavior PostMessageBehavior,
        IReadOnlyList<string> EnabledAgentIds,
        DateTimeOffset UpdatedAt);

    internal sealed class Handler(IUserPreferenceService preferenceService)
        : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command command, CancellationToken cancellationToken)
        {
            Result<UserPreferenceDto> result = await preferenceService.UpdateAsync(
                command.PostMessageBehavior,
                command.EnabledAgentIds,
                cancellationToken);

            if (result.IsFailure)
            {
                return Result.Failure<Response>(result.Error);
            }

            UserPreferenceDto dto = result.Value;
            return Result.Success(new Response(dto.PostMessageBehavior, dto.EnabledAgentIds, dto.UpdatedAt));
        }
    }

    public sealed record Request(
        PostMessageBehavior? PostMessageBehavior = null,
        IReadOnlyList<string>? EnabledAgentIds = null);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(command => command)
                .Must(command => command.PostMessageBehavior is not null || command.EnabledAgentIds is not null)
                .WithMessage("Provide postMessageBehavior and/or enabledAgentIds.");

            When(
                command => command.PostMessageBehavior is not null,
                () =>
                {
                    RuleFor(command => command.PostMessageBehavior!.Value)
                        .IsInEnum()
                        .WithMessage("Post-message behavior is invalid.");
                });

            When(
                command => command.EnabledAgentIds is not null,
                () =>
                {
                    RuleFor(command => command.EnabledAgentIds!)
                        .Must(ids => ids.Count > 0)
                        .WithMessage("Select at least one agent (Cursor or Codex).");
                });
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPatch("/user-preferences", Handle)
                .WithName("UpdateUserPreferences")
                .WithTags("UserPreferences")
                .Produces<Response>();
        }

        private static async Task<IResult> Handle(
            Request request,
            ICommandHandler<Command, Response> handler,
            CancellationToken cancellationToken)
        {
            Result<Response> result = await handler.Handle(
                new Command(request.PostMessageBehavior, request.EnabledAgentIds),
                cancellationToken);

            return result.ToProblem();
        }
    }
}
