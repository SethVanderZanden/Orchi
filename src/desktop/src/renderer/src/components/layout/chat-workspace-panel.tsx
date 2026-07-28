import { useCallback, useEffect, useMemo } from 'react'

import { ChatPanel } from '@/components/chat/chat-panel'
import { ChatWorkspaceHeader } from '@/components/layout/chat-workspace-header'
import { usePlanReview } from '@/hooks/use-plan-review'
import { mergeOrchestrationPlans } from '@/lib/orchestration/resolve-plans'
import { parseOrchestrationPlansFromMessages } from '@/lib/orchestration/parse-plans'
import { resolveReviewContentFromMessages } from '@/lib/orchestration/parse-review-plans'
import type { ParsedReviewPlan } from '@/lib/orchestration/parse-review-plans'
import { needsOrchestrationHydration } from '@/lib/orchestration/needs-orchestration-hydration'
import { listReviewChildIdsNeedingReload } from '@/lib/orchestration/review-ready'
import { isLocalChat } from '@/lib/chat/chat-persistence'
import type { ChatThread } from '@/lib/chat/types'
import type { GitHostProvider } from '@/lib/git/types'
import type { Project, Workspace } from '@/lib/projects/types'
import { resolveWorkspaceContext } from '@/lib/projects/find-workspace'
import { findReviewChildForPlan } from '@/lib/projects/chat-tree'
import { useDeleteChat } from '@/hooks/use-delete-chat'
import { useOrchestration } from '@/hooks/use-orchestration'
import { useOrchestrationParentEvents } from '@/hooks/use-orchestration-parent-events'
import { useChat } from '@/providers/chat-context'
import { useChatTabs } from '@/providers/chat-tabs-provider'
import { useProjects } from '@/providers/project-provider'

type ChatWorkspacePanelProps = {
  chat: ChatThread
}

type ProjectGitSettings = {
  defaultBaseBranch?: string
  gitHostProvider?: GitHostProvider
}

type WorkspaceBranchInfo = {
  branch?: string | null
}

function readProjectGitSettings(project: Project | undefined): {
  defaultBaseBranch: string
  gitHostProvider: GitHostProvider
} {
  const settings = project as (Project & ProjectGitSettings) | undefined

  return {
    defaultBaseBranch: settings?.defaultBaseBranch ?? 'main',
    gitHostProvider: settings?.gitHostProvider ?? 'github'
  }
}

function readWorkspaceBranch(workspace: Workspace | undefined): string | null {
  return (workspace as (Workspace & WorkspaceBranchInfo) | undefined)?.branch ?? null
}

export function ChatWorkspacePanel({ chat }: ChatWorkspacePanelProps): React.JSX.Element {
  const {
    sendMessage,
    getMarkers,
    getChildChats,
    getChat,
    loadChat,
    kickOffPlan,
    kickOffAllPlans,
    getOrchestrationKickoffProgress,
    setOrchestrationKickoffProgress,
    getOrchestrationError,
    updateChatMode,
    getModeUpdateError,
    updateChatModel,
    getModelUpdateError,
    updateChatContextSize,
    getContextSizeUpdateError,
    updateChatReasoningEffort,
    getReasoningEffortUpdateError,
    updateChatApprovalPolicy,
    getApprovalPolicyUpdateError,
    updateChatWorkspace,
    isChatSending,
    isPlanKickingOff,
    isParentKickingOffAny
  } = useChat()
  const { requestDelete, isDeletingChat } = useDeleteChat()
  const { openChat, openChatInSplit, closeTab, splitTabId, createAndOpenSplitTab, isCreatingTab } =
    useChatTabs()
  const { projects } = useProjects()

  const {
    project,
    workspace,
    resolvedProjectName: projectName
  } = useMemo(
    () =>
      resolveWorkspaceContext(
        projects,
        chat.projectId,
        chat.workspaceId,
        null,
        null
      ),
    [chat.projectId, chat.workspaceId, projects]
  )

  const { defaultBaseBranch, gitHostProvider } = readProjectGitSettings(project)
  const workspaceBranch = readWorkspaceBranch(workspace)
  const orchestrationParse = useMemo(
    () =>
      chat.mode === 'orchestration'
        ? parseOrchestrationPlansFromMessages(chat.messages)
        : { plans: [], sequencePlanIds: [] as string[] },
    [chat.mode, chat.messages]
  )
  const parentChat =
    chat.parentChatId && chat.mode !== 'orchestration' ? getChat(chat.parentChatId) : undefined
  const childCount = getChildChats(chat.id).length
  const needsHydration = needsOrchestrationHydration(
    chat,
    childCount,
    isParentKickingOffAny(chat.id)
  )

  useEffect(() => {
    if (!chat.parentChatId || chat.mode === 'orchestration') {
      return
    }

    const parent = getChat(chat.parentChatId)
    if (!parent || parent.mode !== 'orchestration') {
      void loadChat(chat.parentChatId)
    }
  }, [chat.mode, chat.parentChatId, getChat, loadChat])

  const onWorkflowProgress = useCallback(
    (progress: Parameters<typeof setOrchestrationKickoffProgress>[1]) => {
      setOrchestrationKickoffProgress(chat.id, progress)
    },
    [chat.id, setOrchestrationKickoffProgress]
  )

  const onChildrenHydrated = useCallback(
    (childIds: string[]) => {
      for (const childId of childIds) {
        const child = getChat(childId)
        if (child && child.messages.length === 0) {
          void loadChat(childId)
        }
      }
    },
    [getChat, loadChat]
  )

  const { workflowProgress, sequencePlanIds: backendSequencePlanIds, backendPlans } =
    useOrchestration({
    parentChatId: needsHydration ? chat.id : undefined,
    parentChat: needsHydration ? chat : undefined,
    getChat,
    loadChat,
    enabled: needsHydration,
    onWorkflowProgress,
    onChildrenHydrated
  })

  const plans = useMemo(
    () =>
      chat.mode === 'orchestration'
        ? mergeOrchestrationPlans(backendPlans, orchestrationParse.plans)
        : [],
    [backendPlans, chat.mode, orchestrationParse.plans]
  )

  useOrchestrationParentEvents({
    childChat: chat.parentChatId ? chat : undefined,
    parentChat: parentChat?.mode === 'orchestration' ? parentChat : undefined,
    isParentKickoffActive: parentChat ? isParentKickingOffAny(parentChat.id) : false,
    getChat,
    loadChat
  })
  const sequencePlanIds =
    backendSequencePlanIds.length > 0 ? backendSequencePlanIds : orchestrationParse.sequencePlanIds
  const orchestrationKickoffProgress = workflowProgress ?? getOrchestrationKickoffProgress(chat.id)
  const orchestrationError = getOrchestrationError(chat.id)
  const childChats = useMemo(
    () => getChildChats(chat.id).map((child) => getChat(child.id) ?? child),
    [chat.id, getChat, getChildChats]
  )
  const reviewPlansByPlanId = useMemo(
    () =>
      Object.fromEntries(
        plans.map((plan) => {
          const reviewChildSummary = findReviewChildForPlan(plan.planId, childChats)
          const reviewChild = reviewChildSummary ? getChat(reviewChildSummary.id) : undefined
          const reviewPlan = reviewChild
            ? resolveReviewContentFromMessages(reviewChild.messages, plan.planId)
            : undefined
          return [plan.planId, reviewPlan] as const
        })
      ) as Record<string, ParsedReviewPlan | undefined>,
    [childChats, getChat, plans]
  )

  useEffect(() => {
    if (chat.mode !== 'orchestration') {
      return
    }

    for (const childId of listReviewChildIdsNeedingReload(chat, childChats, getChat)) {
      void loadChat(childId)
    }
  }, [chat, childChats, getChat, loadChat])

  const childChatIds = useMemo(
    () =>
      getChildChats(chat.id)
        .map((child) => child.id)
        .join(','),
    [chat.id, getChildChats]
  )

  useEffect(() => {
    if (!needsHydration) {
      return
    }

    for (const id of childChatIds.split(',').filter(Boolean)) {
      const child = getChat(id)
      if (child && child.messages.length === 0) {
        void loadChat(id)
      }
    }
  }, [childChatIds, getChat, loadChat, needsHydration])

  const isAgentRunning = chat.messages.some(
    (message) => message.status === 'processing' || message.status === 'streaming'
  )
  const showModeSelector = chat.messages.length === 0 && chat.parentChatId === null
  const canChangeMode = showModeSelector && !isAgentRunning
  const canChangeWorkspace = isLocalChat(chat.id) && showModeSelector
  const canChangeModel = !isAgentRunning
  const showPlanReview = chat.mode === 'orchestration' && plans.length > 0

  const { reviewState, dispatchReview, toggleReviewPanel, activeReviewTabId, hasReviewReady } =
    usePlanReview({
      plans,
      reviewPlansByPlanId,
      showPlanReview
    })

  const openParentBeside = useCallback(() => {
    if (!chat.parentChatId) {
      return
    }

    // Keep child + parent side by side: if this chat is already in the split,
    // open the parent as the primary tab instead of replacing the split.
    if (splitTabId === chat.id) {
      openChat(chat.parentChatId)
      return
    }

    openChatInSplit(chat.parentChatId)
  }, [chat.id, chat.parentChatId, openChat, openChatInSplit, splitTabId])

  const handleSend = useCallback(
    (content: string) => {
      void sendMessage(chat.id, content)
    },
    [chat.id, sendMessage]
  )

  const handleWorkspaceChange = useCallback(
    (workspaceId: string) => {
      updateChatWorkspace(chat.id, workspaceId)
    },
    [chat.id, updateChatWorkspace]
  )

  const handleClose = useCallback(() => {
    closeTab(chat.id)
  }, [chat.id, closeTab])

  const handleDelete = useCallback(() => {
    requestDelete(chat)
  }, [chat, requestDelete])

  const handleCreateChatFromThis = useCallback(() => {
    void createAndOpenSplitTab({ sourceChatId: chat.id })
  }, [chat.id, createAndOpenSplitTab])

  return (
    <div className="flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden">
      <ChatWorkspaceHeader
        chat={chat}
        projectName={projectName}
        childChatCount={childChats.length}
        workspacePath={chat.workspacePath}
        chatId={chat.id}
        projectId={chat.projectId}
        defaultBaseBranch={defaultBaseBranch}
        gitHostProvider={gitHostProvider}
        workspaceBranch={workspaceBranch}
        parentChatId={chat.parentChatId}
        parentTitle={parentChat?.title ?? null}
        showPlanReview={showPlanReview}
        reviewPanelOpen={reviewState.panelOpen}
        hasReviewReady={hasReviewReady}
        onToggleReviewPanel={toggleReviewPanel}
        onOpenParentBeside={openParentBeside}
        onCreateChatFromThis={handleCreateChatFromThis}
        createChatFromThisDisabled={isChatSending(chat.id) || isCreatingTab}
        onClose={handleClose}
        onDelete={handleDelete}
        deleteDisabled={isChatSending(chat.id) || isDeletingChat(chat.id)}
      />

      <ChatPanel
        messages={chat.messages}
        markers={getMarkers(chat.id)}
        onSend={handleSend}
        mode={chat.mode}
        showModeSelector={showModeSelector}
        canChangeMode={canChangeMode}
        modeUpdateError={getModeUpdateError(chat.id)}
        onModeChange={(mode) => void updateChatMode(chat.id, mode)}
        agentId={chat.agentId}
        modelId={chat.modelId}
        canChangeModel={canChangeModel}
        modelUpdateError={getModelUpdateError(chat.id)}
        onModelChange={(modelId) => void updateChatModel(chat.id, modelId)}
        contextSizeId={chat.contextSizeId}
        canChangeContextSize={canChangeModel}
        contextSizeUpdateError={getContextSizeUpdateError(chat.id)}
        onContextSizeChange={(contextSizeId) => void updateChatContextSize(chat.id, contextSizeId)}
        reasoningEffortId={chat.reasoningEffortId}
        canChangeReasoningEffort={canChangeModel}
        reasoningEffortUpdateError={getReasoningEffortUpdateError(chat.id)}
        onReasoningEffortChange={(reasoningEffortId) =>
          void updateChatReasoningEffort(chat.id, reasoningEffortId)
        }
        approvalPolicyId={chat.approvalPolicyId}
        canChangeApprovalPolicy={canChangeModel}
        approvalPolicyUpdateError={getApprovalPolicyUpdateError(chat.id)}
        onApprovalPolicyChange={(approvalPolicyId) =>
          void updateChatApprovalPolicy(chat.id, approvalPolicyId)
        }
        projectId={chat.projectId}
        workspaceId={chat.workspaceId}
        workspaceName={workspace?.name ?? null}
        workspacePath={chat.workspacePath}
        projectName={projectName}
        projects={projects}
        canChangeWorkspace={canChangeWorkspace}
        onWorkspaceChange={handleWorkspaceChange}
        chatId={chat.id}
        plans={plans}
        parentChatId={chat.id}
        isSending={isChatSending(chat.id)}
        isPlanKickingOff={isPlanKickingOff}
        isParentKickingOffAny={isParentKickingOffAny}
        onKickOffPlan={
          chat.mode === 'orchestration' ? (plan) => kickOffPlan(chat.id, plan) : undefined
        }
        onKickOffAllPlans={
          chat.mode === 'orchestration' ? () => kickOffAllPlans(chat.id) : undefined
        }
        sequencePlanIds={chat.mode === 'orchestration' ? sequencePlanIds : undefined}
        sequentialKickoffProgress={
          chat.mode === 'orchestration' ? orchestrationKickoffProgress : undefined
        }
        orchestrationError={chat.mode === 'orchestration' ? orchestrationError : undefined}
        childChats={chat.mode === 'orchestration' ? childChats : undefined}
        reviewPlansByPlanId={chat.mode === 'orchestration' ? reviewPlansByPlanId : undefined}
        showPlanReview={showPlanReview}
        reviewState={reviewState}
        dispatchReview={dispatchReview}
        activeReviewTabId={activeReviewTabId}
      />
    </div>
  )
}
