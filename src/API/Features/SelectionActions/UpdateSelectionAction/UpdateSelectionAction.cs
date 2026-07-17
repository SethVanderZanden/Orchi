using FluentValidation;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.SelectionActions.Shared;
using Orchi.Api.Infrastructure.SelectionActions;

namespace Orchi.Api.Features.SelectionActions.UpdateSelectionAction;

public static class UpdateSelectionAction
{
    public sealed record Request(string Label, string Template, int? SortOrder = null);

    public sealed record Command(string Id, string Label, string Template, int? SortOrder)
        : ICommand<SelectionActionResponse>;

    internal sealed class Handler(ISelectionActionStore store)
        : ICommandHandler<Command, SelectionActionResponse>
    {
        public async Task<Result<SelectionActionResponse>> Handle(
            Command command,
            CancellationToken cancellationToken)
        {
            StoredSelectionAction? updated = await store.UpdateAsync(
                command.Id,
                command.Label,
                command.Template,
                command.SortOrder,
                cancellationToken);

            if (updated is null)
            {
                return Result.Failure<SelectionActionResponse>(
                    Error.NotFound("Selection action not found."));
            }

            return Result.Success(SelectionActionMapper.ToResponse(updated));
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(command => command.Id).NotEmpty();

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
            app.MapPut("/selection-actions/{id}", Handle)
                .WithName("UpdateSelectionAction")
                .WithTags("SelectionActions")
                .Produces<SelectionActionResponse>();
        }

        private static async Task<IResult> Handle(
            string id,
            Request request,
            ICommandHandler<Command, SelectionActionResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<SelectionActionResponse> result = await handler.Handle(
                new Command(id, request.Label, request.Template, request.SortOrder),
                cancellationToken);

            return result.ToProblem();
        }
    }
}
