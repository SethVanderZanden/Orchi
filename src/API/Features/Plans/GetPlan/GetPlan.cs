using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents.Modes.Plan;

namespace Orchi.Api.Features.Plans.GetPlan;

public static class GetPlan
{
    public sealed record Query(Guid PlanId) : IQuery<PlanResponse>;

    internal sealed class Handler(IPlanStore planStore) : IQueryHandler<Query, PlanResponse>
    {
        public Task<Result<PlanResponse>> Handle(Query query, CancellationToken cancellationToken)
        {
            PlanArtifact? plan = planStore.Get(query.PlanId);
            if (plan is null)
            {
                return Task.FromResult(Result.Failure<PlanResponse>(Error.NotFound($"Plan '{query.PlanId}' was not found.")));
            }

            return Task.FromResult(Result.Success(PlanMapper.ToResponse(plan)));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/plans/{planId:guid}", Handle)
                .WithName("GetPlan")
                .WithTags("Plans")
                .Produces<PlanResponse>();
        }

        private static async Task<IResult> Handle(
            Guid planId,
            IQueryHandler<Query, PlanResponse> handler,
            CancellationToken cancellationToken)
        {
            Result<PlanResponse> result = await handler.Handle(new Query(planId), cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
        }
    }
}
