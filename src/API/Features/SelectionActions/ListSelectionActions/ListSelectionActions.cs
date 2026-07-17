using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.SelectionActions.Shared;
using Orchi.Api.Infrastructure.SelectionActions;

namespace Orchi.Api.Features.SelectionActions.ListSelectionActions;

public static class ListSelectionActions
{
    public sealed record Query : IQuery<IReadOnlyList<SelectionActionResponse>>;

    internal sealed class Handler(ISelectionActionStore store)
        : IQueryHandler<Query, IReadOnlyList<SelectionActionResponse>>
    {
        public async Task<Result<IReadOnlyList<SelectionActionResponse>>> Handle(
            Query query,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<StoredSelectionAction> actions = await store.ListAsync(cancellationToken);
            IReadOnlyList<SelectionActionResponse> response = actions
                .Select(SelectionActionMapper.ToResponse)
                .ToArray();

            return Result.Success(response);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/selection-actions", Handle)
                .WithName("ListSelectionActions")
                .WithTags("SelectionActions")
                .Produces<IReadOnlyList<SelectionActionResponse>>();
        }

        private static async Task<IResult> Handle(
            IQueryHandler<Query, IReadOnlyList<SelectionActionResponse>> handler,
            CancellationToken cancellationToken)
        {
            Result<IReadOnlyList<SelectionActionResponse>> result =
                await handler.Handle(new Query(), cancellationToken);

            return result.ToProblem();
        }
    }
}
