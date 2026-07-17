using FluentValidation;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.SelectionActions.Shared;
using Orchi.Api.Infrastructure.SelectionActions;

namespace Orchi.Api.Features.SelectionActions.CreateSelectionAction;

public static class CreateSelectionAction
{
    public sealed record Request(string Label, string Template);

    public sealed record Command(string Label, string Template) : ICommand<SelectionActionResponse>;

    internal sealed class Handler(ISelectionActionStore store)
        : ICommandHandler<Command, SelectionActionResponse>
    {
        public async Task<Result<SelectionActionResponse>> Handle(
            Command command,
            CancellationToken cancellationToken)
        {
            StoredSelectionAction created = await store.CreateAsync(
                command.Label,
                command.Template,
                cancellationToken);

            return Result.Success(SelectionActionMapper.ToResponse(created));
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(command => command.Label)
                .NotEmpty()
                .MaximumLength(128)
                .WithMessage("Label is required (max 128 characters).");

            RuleFor(command => command.Template)
                .NotEmpty()
                .MaximumLength(4000)
                .WithMessage("Template is required (max 4000 characters).")
                .Must(SelectionActionTemplate.ContainsSelectedTextPlaceholder)
                .WithMessage($"Template must include {SelectionActionTemplate.SelectedTextPlaceholder}.");
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/selection-actions", Handle)
                .WithName("CreateSelectionAction")
                .WithTags("SelectionActions")
                .Produces<SelectionActionResponse>(StatusCodes.Status201Created);
        }

        private static async Task<IResult> Handle(
            Request request,
            ICommandHandler<Command, SelectionActionResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<SelectionActionResponse> result = await handler.Handle(
                new Command(request.Label, request.Template),
                cancellationToken);

            if (result.IsFailure)
            {
                return result.ToProblem();
            }

            SelectionActionResponse response = result.Value;
            return Results.Created($"/selection-actions/{response.Id}", response);
        }
    }
}
