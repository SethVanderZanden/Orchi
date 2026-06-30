using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Http;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Modes.Plan;

namespace Orchi.Api.Features.Plans.DispatchSubPlan;

public static class DispatchSubPlan
{
    public sealed record Command(Guid PlanId, Guid SubPlanId, ChatMode ChildMode) : ICommand<DispatchSubPlanResponse>;

    internal sealed class Handler(
        AgentSessionManager sessionManager,
        IPlanStore planStore,
        PlanManager planManager)
        : ICommandHandler<Command, DispatchSubPlanResponse>
    {
        public async Task<Result<DispatchSubPlanResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            PlanArtifact? plan = planStore.Get(command.PlanId);
            if (plan is null)
            {
                return Result.Failure<DispatchSubPlanResponse>(
                    Error.NotFound($"Plan '{command.PlanId}' was not found."));
            }

            SubPlan? subPlan = plan.SubPlans.FirstOrDefault(sub => sub.Id == command.SubPlanId);
            if (subPlan is null)
            {
                return Result.Failure<DispatchSubPlanResponse>(
                    Error.NotFound($"Sub-plan '{command.SubPlanId}' was not found."));
            }

            ChatSession? sourceChat = await sessionManager.GetOrLoadSessionAsync(plan.SourceChatId, cancellationToken);
            if (sourceChat is null)
            {
                return Result.Failure<DispatchSubPlanResponse>(
                    Error.NotFound($"Source chat '{plan.SourceChatId}' was not found."));
            }

            Guid parentChatId = sourceChat.GoalChatId ?? sourceChat.Id;
            Guid attachedPlanId = subPlan.Id;

            if (command.ChildMode == ChatMode.Implement)
            {
                attachedPlanId = subPlan.Id;
            }
            else if (command.ChildMode == ChatMode.Plan)
            {
                Result<PlanArtifact> childPlan = planManager.CreatePlan(
                    plan.SourceChatId,
                    subPlan.Title,
                    subPlan.ContentMarkdown);

                if (childPlan.IsFailure)
                {
                    return Result.Failure<DispatchSubPlanResponse>(childPlan.Error);
                }

                attachedPlanId = childPlan.Value.Id;
            }

            Result<ChatSession> childResult = await sessionManager.CreateSessionAsync(
                sourceChat.AgentId,
                sourceChat.WorkspacePath,
                command.ChildMode,
                parentChatId,
                command.ChildMode == ChatMode.Implement ? attachedPlanId : null,
                cancellationToken);

            if (childResult.IsFailure)
            {
                return Result.Failure<DispatchSubPlanResponse>(childResult.Error);
            }

            ChatSession child = childResult.Value;
            await planManager.MarkSubPlanDispatchedAsync(subPlan.Id, child.Id, cancellationToken);
            plan.Status = PlanStatus.Dispatched;
            await planManager.SavePlanAsync(plan, cancellationToken);
            await sessionManager.SeedInitialMessageAsync(child.Id, subPlan.ContentMarkdown, cancellationToken);

            return Result.Success(new DispatchSubPlanResponse(child.Id, subPlan.Id));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/plans/{planId:guid}/dispatch", Handle)
                .WithName("DispatchSubPlan")
                .WithTags("Plans")
                .Produces<DispatchSubPlanResponse>(StatusCodes.Status201Created);
        }

        private static async Task<IResult> Handle(
            Guid planId,
            DispatchSubPlanRequest request,
            ICommandHandler<Command, DispatchSubPlanResponse> handler,
            CancellationToken cancellationToken)
        {
            if (!ChatModeParser.TryParse(request.ChildMode, out ChatMode childMode))
            {
                return Results.BadRequest(new
                {
                    Code = "Mode.Invalid",
                    Message = $"Invalid child mode '{request.ChildMode}'."
                });
            }

            if (childMode is not (ChatMode.Implement or ChatMode.Plan))
            {
                return Results.BadRequest(new
                {
                    Code = "Mode.Invalid",
                    Message = "Child mode must be plan or implement."
                });
            }

            Result<DispatchSubPlanResponse> result = await handler.Handle(
                new Command(planId, request.SubPlanId, childMode),
                cancellationToken);

            if (result.IsSuccess)
            {
                return Results.Created($"/chats/{result.Value.ChildChatId}", result.Value);
            }

            return result.ToProblem();
        }
    }
}
