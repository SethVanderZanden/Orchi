using System.Collections.Concurrent;
using Orchi.Api.Common.Abstractions;
using Orchi.Api.Common.Results;
using Orchi.Api.Features.Chats.KickOffPlan;
using Orchi.Api.Features.Chats.KickOffReview;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Orchestration.Persistence;
using Orchi.Api.Infrastructure.Agents.Plans;

namespace Orchi.Api.Infrastructure.Agents.Orchestration;

public interface IOrchestrationWorkflowService
{
    Task<Result<OrchestrationSnapshot>> GetSnapshotAsync(Guid parentChatId, CancellationToken cancellationToken);

    Task<Result<OrchestrationSnapshot>> StartKickoffAllAsync(Guid parentChatId, CancellationToken cancellationToken);

    Task OnAgentTurnCompletedAsync(Guid chatId, bool succeeded, CancellationToken cancellationToken);
}

public sealed record OrchestrationSnapshot(
    string Status,
    int? CurrentStep,
    int? TotalSteps,
    string? CurrentPlanId,
    IReadOnlyList<string> SequencePlanIds,
    IReadOnlyList<OrchestrationPlanSnapshot> Plans,
    IReadOnlyList<OrchestrationChildSnapshot> Children);

public sealed record OrchestrationPlanSnapshot(
    string PlanId,
    string Title,
    string ContentMarkdown);

public sealed record OrchestrationChildSnapshot(
    string PlanId,
    Guid ChatId,
    string Mode,
    string? PlanFilePath);

public sealed class OrchestrationWorkflowService(
    AgentSessionManager sessionManager,
    IOrchestrationWorkflowStore workflowStore,
    OrchestrationStepPipeline stepPipeline,
    OrchestrationEventHub eventHub,
    IServiceScopeFactory scopeFactory,
    ILogger<OrchestrationWorkflowService> logger) : IOrchestrationWorkflowService
{
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> ParentLocks = new();

    public async Task<Result<OrchestrationSnapshot>> GetSnapshotAsync(
        Guid parentChatId,
        CancellationToken cancellationToken)
    {
        ChatSession? parent = await sessionManager.GetOrLoadSessionAsync(parentChatId, cancellationToken);
        if (parent is null)
        {
            return Result.Failure<OrchestrationSnapshot>(
                Error.NotFound($"Chat '{parentChatId}' was not found."));
        }

        if (!string.Equals(parent.Mode, OrchestrationAgentModeStrategy.Mode, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<OrchestrationSnapshot>(
                Error.Validation("Mode.Invalid", "Orchestration state is only available for orchestration chats."));
        }

        OrchestrationWorkflowRecord? workflow = await workflowStore.GetAsync(parentChatId, cancellationToken);
        IReadOnlyList<ChatSession> childChats = await GetChildChatsAsync(parentChatId, cancellationToken);

        return Result.Success(BuildSnapshot(parent, workflow, childChats));
    }

    /// <summary>
    /// "Kick off all" = press start on every plan that hasn't been handed to a worker yet.
    /// Think of the parent chat as a manager with a to-do list; this method reads the list,
    /// figures out what's still waiting, and dispatches work without blocking the HTTP response.
    /// </summary>
    public async Task<Result<OrchestrationSnapshot>> StartKickoffAllAsync(
        Guid parentChatId,
        CancellationToken cancellationToken)
    {
        // Only one person can rearrange this manager's desk at a time.
        SemaphoreSlim gate = ParentLocks.GetOrAdd(parentChatId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);

        try
        {
            // Step 1: Find the parent chat — the orchestration "control room."
            ChatSession? parent = await sessionManager.GetOrLoadSessionAsync(parentChatId, cancellationToken);
            if (parent is null)
            {
                return Result.Failure<OrchestrationSnapshot>(
                    Error.NotFound($"Chat '{parentChatId}' was not found."));
            }

            // This button only works on orchestration chats, not regular one-off chats.
            if (!string.Equals(parent.Mode, OrchestrationAgentModeStrategy.Mode, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure<OrchestrationSnapshot>(
                    Error.Validation("Mode.Invalid", "Kick off all is only available for orchestration chats."));
            }

            // Step 2: Read the parent's conversation to learn what plans exist and in what order.
            //   - plans        = every plan card the orchestrator wrote out
            //   - sequencePlanIds = the "do these in order" list (if any)
            //   - childChats   = worker chats already spawned under this parent
            IReadOnlyList<PlanMarkdownParser.ParsedPlan> plans =
                PlanMarkdownParser.ExtractAllPlansFromMessages(parent.Messages);
            IReadOnlyList<string> sequencePlanIds =
                PlanSequenceMarkdownParser.ParseSequenceFromMessages(parent.Messages);
            IReadOnlyList<ChatSession> childChats = await GetChildChatsAsync(parentChatId, cancellationToken);

            // Step 3: Filter to plans that don't have an implementation worker yet.
            // A plan is "done being kicked off" once a child chat exists for it.
            IReadOnlyList<PlanMarkdownParser.ParsedPlan> pendingPlans = plans
                .Where(plan => !HasImplementationChild(plan.PlanId, childChats))
                .ToArray();

            // Nothing left to start — just show the current status board and go home.
            if (pendingPlans.Count == 0)
            {
                return await GetSnapshotAsync(parentChatId, cancellationToken);
            }

            // Step 4: Split plans into two lanes:
            //   - sequenced  = must run one-after-another (like assembly-line steps)
            //   - independent = free to run in parallel (like separate side quests)
            (IReadOnlyList<PlanMarkdownParser.ParsedPlan> sequenced, IReadOnlyList<PlanMarkdownParser.ParsedPlan> independent) =
                PlanKickoffGroups.Resolve(plans, sequencePlanIds);

            // Of the assembly-line plans, which ones still need a worker?
            IReadOnlyList<PlanMarkdownParser.ParsedPlan> pendingSequenced = sequenced
                .Where(plan => !HasImplementationChild(plan.PlanId, childChats))
                .ToArray();

            // Step 5: Update the workflow bookmark so we remember where we are in the sequence.
            // NextSequenceIndex is 1-based: "we're about to start step N."
            OrchestrationWorkflowRecord? existing = await workflowStore.GetAsync(parentChatId, cancellationToken);
            int nextSequenceIndex = ResolveNextSequenceIndex(existing, sequenced, childChats);

            var workflow = new OrchestrationWorkflowRecord(
                parentChatId,
                pendingSequenced.Count > 0 ? OrchestrationWorkflowStatus.Running : OrchestrationWorkflowStatus.Idle,
                sequencePlanIds,
                nextSequenceIndex,
                existing?.CreatedAt ?? DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow);

            // If we were paused (e.g. a step failed) and there's still sequence work, unpause.
            if (existing?.Status == OrchestrationWorkflowStatus.Paused && pendingSequenced.Count > 0)
            {
                workflow = workflow with { Status = OrchestrationWorkflowStatus.Running };
            }

            // Save progress if we're doing sequence work or resuming an existing workflow.
            if (pendingSequenced.Count > 0 || existing is not null)
            {
                await workflowStore.UpsertAsync(workflow, cancellationToken);
            }

            // Tell anyone listening (UI via SSE) that the workflow state changed.
            await PublishWorkflowEventAsync(parentChatId, workflow, cancellationToken);

            // Step 6: Decide what to actually start right now.
            //   - All pending independent plans go immediately (parallel side quests).
            //   - Only the FIRST pending sequenced plan goes now; later steps wait for
            //     OnAgentTurnCompletedAsync to advance the assembly line.
            var plansToKick = independent
                .Where(plan => pendingPlans.Any(pending => pending.PlanId == plan.PlanId))
                .ToArray();
            PlanMarkdownParser.ParsedPlan? firstSequenced = pendingSequenced.FirstOrDefault();

            if (plansToKick.Length > 0 || firstSequenced is not null)
            {
                Guid parentId = parent.Id;

                // Fire-and-forget: spawn workers in the background so the API returns fast.
                // The caller gets a snapshot immediately; agents keep running after that.
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Start every independent plan that is still waiting.
                        foreach (PlanMarkdownParser.ParsedPlan plan in plansToKick)
                        {
                            await KickOffPlanAndRunAsync(parentId, plan, CancellationToken.None);
                        }

                        // Start only the next assembly-line step (not the whole sequence).
                        if (firstSequenced is not null)
                        {
                            await KickOffPlanAndRunAsync(parentId, firstSequenced, CancellationToken.None);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Kick off all failed for orchestration chat {ParentChatId}", parentId);
                    }
                }, CancellationToken.None);
            }

            // Step 7: Return the status board — plans, children, current step, running/idle/paused.
            return Result.Success(BuildSnapshot(parent, workflow, childChats));
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task OnAgentTurnCompletedAsync(Guid chatId, bool succeeded, CancellationToken cancellationToken)
    {
        ChatSession? completedChat = await sessionManager.GetOrLoadSessionAsync(chatId, cancellationToken);
        if (completedChat?.ParentChatId is null)
        {
            return;
        }

        ChatSession? parent = await sessionManager.GetOrLoadSessionAsync(
            completedChat.ParentChatId.Value,
            cancellationToken);

        if (parent is null ||
            !string.Equals(parent.Mode, OrchestrationAgentModeStrategy.Mode, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        SemaphoreSlim gate = ParentLocks.GetOrAdd(parent.Id, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);

        try
        {
            OrchestrationWorkflowRecord? workflow = await workflowStore.GetAsync(parent.Id, cancellationToken);
            IReadOnlyList<ChatSession> childChats = await GetChildChatsAsync(parent.Id, cancellationToken);
            IReadOnlyList<PlanMarkdownParser.ParsedPlan> plans =
                PlanMarkdownParser.ExtractAllPlansFromMessages(parent.Messages);
            string? completedPlanId = PlanMarkdownParser.TryExtractPlanIdFromPath(completedChat.PlanFilePath);

            await AppendParentStatusMessageAsync(
                parent.Id,
                BuildStatusMessage(completedChat, completedPlanId, succeeded),
                cancellationToken);

            var context = new OrchestrationStepContext(
                parent.Id,
                parent,
                completedChat,
                completedPlanId,
                succeeded,
                workflow,
                plans,
                childChats);

            IReadOnlyList<OrchestrationStepAction> actions =
                await stepPipeline.ExecuteAsync(context, cancellationToken);

            foreach (OrchestrationStepAction action in actions)
            {
                await ExecuteActionAsync(parent, workflow, action, cancellationToken);
            }

            workflow = await workflowStore.GetAsync(parent.Id, cancellationToken);
            if (workflow is not null)
            {
                await PublishWorkflowEventAsync(parent.Id, workflow, cancellationToken);
                await TryMarkWorkflowCompletedAsync(parent, workflow, childChats, cancellationToken);
            }
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task ExecuteActionAsync(
        ChatSession parent,
        OrchestrationWorkflowRecord? workflow,
        OrchestrationStepAction action,
        CancellationToken cancellationToken)
    {
        switch (action.Kind)
        {
            case OrchestrationStepActionKind.PauseWorkflow:
                if (workflow is null)
                {
                    return;
                }

                await workflowStore.UpsertAsync(
                    workflow with { Status = OrchestrationWorkflowStatus.Paused },
                    cancellationToken);
                break;

            case OrchestrationStepActionKind.KickOffReview:
                if (action.ImplementationChildChatId is null)
                {
                    return;
                }

                await KickOffReviewAndRunAsync(parent.Id, action.ImplementationChildChatId.Value, cancellationToken);
                break;

            case OrchestrationStepActionKind.KickOffNextPlan:
                if (action.Plan is null || workflow is null)
                {
                    return;
                }

                int nextIndex = IndexOfPlanId(workflow.SequencePlanIds, action.Plan.PlanId);

                if (nextIndex < 0)
                {
                    return;
                }

                await workflowStore.UpsertAsync(
                    workflow with
                    {
                        Status = OrchestrationWorkflowStatus.Running,
                        NextSequenceIndex = nextIndex + 1
                    },
                    cancellationToken);

                await KickOffPlanAndRunAsync(parent.Id, action.Plan, cancellationToken);
                break;
        }
    }

    private async Task KickOffPlanAndRunAsync(
        Guid parentChatId,
        PlanMarkdownParser.ParsedPlan plan,
        CancellationToken cancellationToken)
    {
        ChatSession? parent = await sessionManager.GetOrLoadSessionAsync(parentChatId, cancellationToken);
        if (parent is null)
        {
            return;
        }

        using IServiceScope scope = scopeFactory.CreateScope();
        ICommandHandler<KickOffPlan.Command, KickOffPlanResponse> kickOffPlanHandler =
            scope.ServiceProvider.GetRequiredService<ICommandHandler<KickOffPlan.Command, KickOffPlanResponse>>();

        Result<KickOffPlanResponse> result = await kickOffPlanHandler.Handle(
            new KickOffPlan.Command(parent.Id, plan.PlanId, plan.Title, plan.ContentMarkdown),
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

        KickOffPlanResponse response = result.Value;

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

    private async Task KickOffReviewAndRunAsync(
        Guid parentChatId,
        Guid implementationChildChatId,
        CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        ICommandHandler<KickOffReview.Command, KickOffReviewResponse> kickOffReviewHandler =
            scope.ServiceProvider.GetRequiredService<ICommandHandler<KickOffReview.Command, KickOffReviewResponse>>();

        Result<KickOffReviewResponse> result = await kickOffReviewHandler.Handle(
            new KickOffReview.Command(implementationChildChatId),
            cancellationToken);

        if (result.IsFailure)
        {
            logger.LogWarning(
                "Review kickoff failed for implementation chat {ImplementationChildChatId}: {Error}",
                implementationChildChatId,
                result.Error.Message);
            return;
        }

        KickOffReviewResponse response = result.Value;
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

    private void RunAgentTurnInBackground(Guid parentChatId, Guid childChatId, string content)
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

    private async Task TryMarkWorkflowCompletedAsync(
        ChatSession parent,
        OrchestrationWorkflowRecord workflow,
        IReadOnlyList<ChatSession> childChats,
        CancellationToken cancellationToken)
    {
        if (workflow.SequencePlanIds.Count == 0)
        {
            return;
        }

        bool allSequencedStarted = workflow.SequencePlanIds
            .All(planId => HasImplementationChild(planId, childChats));

        if (!allSequencedStarted || workflow.NextSequenceIndex < workflow.SequencePlanIds.Count)
        {
            return;
        }

        bool anyRunning = childChats.Any(chat =>
            chat.Messages.Any(message =>
                message.Status is "processing" or "streaming"));

        if (anyRunning)
        {
            return;
        }

        await workflowStore.UpsertAsync(
            workflow with { Status = OrchestrationWorkflowStatus.Completed },
            cancellationToken);
    }

    private async Task PublishWorkflowEventAsync(
        Guid parentChatId,
        OrchestrationWorkflowRecord workflow,
        CancellationToken cancellationToken)
    {
        int totalSteps = workflow.SequencePlanIds.Count;
        int? currentStep = totalSteps > 0 ? workflow.NextSequenceIndex : null;
        string? currentPlanId = currentStep is > 0 and var step && step <= totalSteps
            ? workflow.SequencePlanIds[step - 1]
            : null;

        await eventHub.PublishAsync(
            parentChatId,
            new OrchestrationWorkflowEvent(
                workflow.Status,
                currentStep,
                totalSteps > 0 ? totalSteps : null,
                currentPlanId),
            cancellationToken);
    }

    private async Task<IReadOnlyList<ChatSession>> GetChildChatsAsync(
        Guid parentChatId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ChatSession> sessions = await sessionManager.ListSessionsAsync(cancellationToken);
        return sessions.Where(chat => chat.ParentChatId == parentChatId).ToArray();
    }

    private static int ResolveNextSequenceIndex(
        OrchestrationWorkflowRecord? existing,
        IReadOnlyList<PlanMarkdownParser.ParsedPlan> sequenced,
        IReadOnlyList<ChatSession> childChats)
    {
        if (existing is not null && existing.NextSequenceIndex > 0)
        {
            return existing.NextSequenceIndex;
        }

        IReadOnlyList<string> sequencePlanIds = sequenced.Select(plan => plan.PlanId).ToArray();
        int nextIndex = 0;

        while (nextIndex < sequencePlanIds.Count &&
               HasImplementationChild(sequencePlanIds[nextIndex], childChats))
        {
            nextIndex++;
        }

        return nextIndex + 1;
    }

    private static bool HasImplementationChild(string planId, IReadOnlyList<ChatSession> childChats)
    {
        return childChats.Any(chat =>
            string.Equals(chat.Mode, ImplementationAgentModeStrategy.Mode, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(PlanMarkdownParser.TryExtractPlanIdFromPath(chat.PlanFilePath), planId, StringComparison.OrdinalIgnoreCase));
    }

    private static int IndexOfPlanId(IReadOnlyList<string> sequencePlanIds, string planId)
    {
        for (int index = 0; index < sequencePlanIds.Count; index++)
        {
            if (string.Equals(sequencePlanIds[index], planId, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private static OrchestrationSnapshot BuildSnapshot(
        ChatSession parent,
        OrchestrationWorkflowRecord? workflow,
        IReadOnlyList<ChatSession> childChats)
    {
        IReadOnlyList<PlanMarkdownParser.ParsedPlan> plans =
            PlanMarkdownParser.ExtractAllPlansFromMessages(parent.Messages);
        IReadOnlyList<string> sequencePlanIds = workflow?.SequencePlanIds ??
            PlanSequenceMarkdownParser.ParseSequenceFromMessages(parent.Messages);

        string status = workflow?.Status ?? OrchestrationWorkflowStatus.Idle;
        int totalSteps = sequencePlanIds.Count;
        int? currentStep = workflow?.NextSequenceIndex;
        string? currentPlanId = currentStep is > 0 and var step && step <= totalSteps
            ? sequencePlanIds[step - 1]
            : null;

        var children = new List<OrchestrationChildSnapshot>();
        foreach (ChatSession child in childChats)
        {
            string? planId = PlanMarkdownParser.TryExtractAnyPlanIdFromPath(child.PlanFilePath);
            if (planId is null)
            {
                continue;
            }

            children.Add(new OrchestrationChildSnapshot(planId, child.Id, child.Mode, child.PlanFilePath));
        }

        return new OrchestrationSnapshot(
            status,
            totalSteps > 0 ? currentStep : null,
            totalSteps > 0 ? totalSteps : null,
            currentPlanId,
            sequencePlanIds,
            plans.Select(plan => new OrchestrationPlanSnapshot(plan.PlanId, plan.Title, plan.ContentMarkdown)).ToArray(),
            children);
    }

    private static string BuildStatusMessage(ChatSession completedChat, string? planId, bool succeeded)
    {
        string subject = planId is null ? "Agent" : $"Plan `{planId}`";
        string mode = completedChat.Mode;

        if (string.Equals(mode, ImplementationAgentModeStrategy.Mode, StringComparison.OrdinalIgnoreCase))
        {
            return succeeded
                ? $"{subject} implementation completed."
                : $"{subject} implementation failed.";
        }

        if (string.Equals(mode, ReviewAgentModeStrategy.Mode, StringComparison.OrdinalIgnoreCase))
        {
            return succeeded
                ? $"{subject} review completed."
                : $"{subject} review failed.";
        }

        return succeeded
            ? $"{subject} completed."
            : $"{subject} failed.";
    }
}
