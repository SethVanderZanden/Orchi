using System.Reflection;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Plans;

namespace Orchi.Api.Infrastructure.Agents.Orchestration;

public sealed class OrchiKickoffExecutor(
    AgentSessionManager sessionManager,
    OrchestrationEventHub eventHub,
    IServiceScopeFactory scopeFactory,
    ILogger<OrchiKickoffExecutor> logger) : IOrchiKickoffExecutor
{
    public async Task KickOffPlanAndRunAsync(
        Guid parentChatId,
        PlanMarkdownParser.ParsedPlan plan,
        CancellationToken cancellationToken)
    {
        ChatSession? parent = await sessionManager.GetOrLoadSessionAsync(parentChatId, cancellationToken);
        if (parent is null)
        {
            return;
        }

        Result<PlanKickoffResult> result = await KickoffHandlerInvoker.InvokePlanKickoffAsync(
            scopeFactory,
            parent.Id,
            plan.PlanId,
            plan.Title,
            plan.ContentMarkdown,
            cancellationToken);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Plan kickoff failed for {PlanId} on chat {ParentChatId}: {Error}",
                plan.PlanId,
                parent.Id,
                result.Error.Message);
            return;
        }

        PlanKickoffResult response = result.Value;

        await eventHub.PublishAsync(
            parent.Id,
            new OrchestrationChatCreatedEvent(
                response.ChildChatId,
                ImplementationAgentModeStrategy.Mode,
                parent.Id,
                plan.PlanId,
                response.PlanFilePath),
            cancellationToken);

        await AppendParentStatusMessageAsync(
            parentChatId,
            $"Started implementation for plan `{plan.PlanId}`.",
            cancellationToken);

        RunAgentTurnInBackground(parentChatId, response.ChildChatId, response.KickoffMessage);
    }

    public async Task KickOffReviewAndRunAsync(
        Guid parentChatId,
        Guid implementationChildChatId,
        CancellationToken cancellationToken)
    {
        Result<ReviewKickoffResult> result = await KickoffHandlerInvoker.InvokeReviewKickoffAsync(
            scopeFactory,
            implementationChildChatId,
            cancellationToken);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Review kickoff failed for implementation chat {ImplementationChildChatId}: {Error}",
                implementationChildChatId,
                result.Error.Message);
            return;
        }

        ReviewKickoffResult response = result.Value;
        string? planId = PlanMarkdownParser.TryExtractPlanIdFromPath(
            (await sessionManager.GetOrLoadSessionAsync(implementationChildChatId, cancellationToken))?.PlanFilePath);

        await eventHub.PublishAsync(
            parentChatId,
            new OrchestrationChatCreatedEvent(
                response.ReviewChildChatId,
                ReviewAgentModeStrategy.Mode,
                parentChatId,
                planId,
                response.ReviewFilePath),
            cancellationToken);

        await AppendParentStatusMessageAsync(
            parentChatId,
            planId is null
                ? "Started review."
                : $"Started review for plan `{planId}`.",
            cancellationToken);

        RunAgentTurnInBackground(parentChatId, response.ReviewChildChatId, response.InitialPrompt);
    }

    public void RunAgentTurnInBackground(Guid parentChatId, Guid childChatId, string content)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using IServiceScope scope = scopeFactory.CreateScope();
                OrchestrationAgentRunner runner =
                    scope.ServiceProvider.GetRequiredService<OrchestrationAgentRunner>();

                await runner.RunTurnAsync(parentChatId, childChatId, content, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Background agent run failed for child chat {ChildChatId}",
                    childChatId);
            }
        }, CancellationToken.None);
    }

    private async Task AppendParentStatusMessageAsync(
        Guid parentChatId,
        string content,
        CancellationToken cancellationToken)
    {
        var message = new ChatMessage(
            Guid.NewGuid(),
            "assistant",
            content,
            DateTimeOffset.UtcNow,
            Status: "complete");

        ChatSession? parent = await sessionManager.GetOrLoadSessionAsync(parentChatId, cancellationToken);
        if (parent is not null)
        {
            lock (parent.Sync)
            {
                parent.Messages.Add(message);
            }
        }

        await sessionManager.SaveAssistantStatusMessageAsync(parentChatId, message, cancellationToken);

        await eventHub.PublishAsync(
            parentChatId,
            new OrchestrationParentMessageEvent(message.Id, message.Role, message.Content),
            cancellationToken);
    }

    private sealed record PlanKickoffResult(Guid ChildChatId, string PlanFilePath, string KickoffMessage);

    private sealed record ReviewKickoffResult(
        Guid ReviewChildChatId,
        string ReviewFilePath,
        string InitialPrompt);

    private static class KickoffHandlerInvoker
    {
        private static readonly Assembly ApiAssembly = typeof(OrchiKickoffExecutor).Assembly;

        private static readonly Type PlanCommandType = ApiAssembly.GetType(
            "Orchi.Api.Features.Chats.KickOffPlan.KickOffPlan+Command",
            throwOnError: true)!;

        private static readonly Type PlanResponseType = ApiAssembly.GetType(
            "Orchi.Api.Features.Chats.Shared.KickOffPlanResponse",
            throwOnError: true)!;

        private static readonly Type ReviewCommandType = ApiAssembly.GetType(
            "Orchi.Api.Features.Chats.KickOffReview.KickOffReview+Command",
            throwOnError: true)!;

        private static readonly Type ReviewResponseType = ApiAssembly.GetType(
            "Orchi.Api.Features.Chats.Shared.KickOffReviewResponse",
            throwOnError: true)!;

        public static async Task<Result<PlanKickoffResult>> InvokePlanKickoffAsync(
            IServiceScopeFactory scopeFactory,
            Guid parentChatId,
            string planId,
            string title,
            string contentMarkdown,
            CancellationToken cancellationToken)
        {
            object command = Activator.CreateInstance(
                PlanCommandType,
                parentChatId,
                planId,
                title,
                contentMarkdown)!;

            Result<object?> result = await InvokeHandlerAsync(
                scopeFactory,
                PlanCommandType,
                PlanResponseType,
                command,
                cancellationToken);

            return MapPlanResult(result);
        }

        public static async Task<Result<ReviewKickoffResult>> InvokeReviewKickoffAsync(
            IServiceScopeFactory scopeFactory,
            Guid implementationChildChatId,
            CancellationToken cancellationToken)
        {
            object command = Activator.CreateInstance(ReviewCommandType, implementationChildChatId)!;

            Result<object?> result = await InvokeHandlerAsync(
                scopeFactory,
                ReviewCommandType,
                ReviewResponseType,
                command,
                cancellationToken);

            return MapReviewResult(result);
        }

        private static Result<PlanKickoffResult> MapPlanResult(Result<object?> result)
        {
            if (result.IsFailure)
            {
                return Result.Failure<PlanKickoffResult>(result.Error);
            }

            object response = result.Value!;
            return Result.Success(new PlanKickoffResult(
                (Guid)PlanResponseType.GetProperty(nameof(PlanKickoffResult.ChildChatId))!.GetValue(response)!,
                (string)PlanResponseType.GetProperty(nameof(PlanKickoffResult.PlanFilePath))!.GetValue(response)!,
                (string)PlanResponseType.GetProperty(nameof(PlanKickoffResult.KickoffMessage))!.GetValue(response)!));
        }

        private static Result<ReviewKickoffResult> MapReviewResult(Result<object?> result)
        {
            if (result.IsFailure)
            {
                return Result.Failure<ReviewKickoffResult>(result.Error);
            }

            object response = result.Value!;
            return Result.Success(new ReviewKickoffResult(
                (Guid)ReviewResponseType.GetProperty(nameof(ReviewKickoffResult.ReviewChildChatId))!.GetValue(response)!,
                (string)ReviewResponseType.GetProperty(nameof(ReviewKickoffResult.ReviewFilePath))!.GetValue(response)!,
                (string)ReviewResponseType.GetProperty(nameof(ReviewKickoffResult.InitialPrompt))!.GetValue(response)!));
        }

        private static async Task<Result<object?>> InvokeHandlerAsync(
            IServiceScopeFactory scopeFactory,
            Type commandType,
            Type responseType,
            object command,
            CancellationToken cancellationToken)
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            Type handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, responseType);
            object handler = scope.ServiceProvider.GetRequiredService(handlerType);

            MethodInfo handleMethod = handlerType.GetMethod("Handle")!;
            Task task = (Task)handleMethod.Invoke(handler, [command, cancellationToken])!;
            await task.ConfigureAwait(false);

            object result = task.GetType().GetProperty("Result")!.GetValue(task)!;
            bool isFailure = (bool)result.GetType().GetProperty(nameof(Result.IsFailure))!.GetValue(result)!;

            if (isFailure)
            {
                Error error = (Error)result.GetType().GetProperty(nameof(Result.Error))!.GetValue(result)!;
                return Result.Failure<object?>(error);
            }

            object value = result.GetType().GetProperty("Value")!.GetValue(result)!;
            return Result.Success<object?>(value);
        }
    }
}
