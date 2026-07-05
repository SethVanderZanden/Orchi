import { useEffect, useMemo } from 'react'

import { ChatPanel } from '@/components/chat/chat-panel'
import { ChatWorkspaceHeader } from '@/components/layout/chat-workspace-header'
import { usePlanReview } from '@/hooks/use-plan-review'
import { parseOrchestrationPlansFromMessages } from '@/lib/orchestration/parse-plans'
import { parseReviewPlansFromMessages } from '@/lib/orchestration/parse-review-plans'
import type { ParsedReviewPlan } from '@/lib/orchestration/parse-review-plans'
import type { ChatThread } from '@/lib/chat/types'
import { findReviewChildForPlan } from '@/lib/projects/chat-tree'
import { useDeleteChat } from '@/hooks/use-delete-chat'
import { useOrchestration } from '@/hooks/use-orchestration'
import { useOrchestrationParentEvents } from '@/hooks/use-orchestration-parent-events'
import { useChat } from '@/providers/chat-provider'
import { useProjects } from '@/providers/project-provider'

type ChatWorkspacePanelProps = {
  chat: ChatThread
}

export function ChatWorkspacePanel({ chat }: ChatWorkspacePanelProps): React.JSX.Element {
  const {
    chats,
    sendMessage,
    getMarkers,
    getChildChats,
    getChat,
    loadChat,
    kickOffPlan,
    kickOffAllPlans,
    getOrchestrationKickoffProgress,
    setOrchestrationKickoffProgress,
    updateChatMode,
    getModeUpdateError,
    updateChatModel,
    getModelUpdateError,
    isChatSending,
    isPlanKickingOff,
    isParentKickingOffAny
  } = useChat()
  const { requestDelete, isDeletingChat } = useDeleteChat()
  const { projects } = useProjects()
  const projectName =
    projects.find((project) => project.id === chat.projectId)?.name ??
    (chat.projectId ? 'Unknown project' : null)
  const orchestrationParse =
    chat.mode === 'orchestration'
      ? parseOrchestrationPlansFromMessages(chat.messages)
      : { plans: [], sequencePlanIds: [] as string[] }
  const plans = orchestrationParse.plans
  const parentChat =
    chat.parentChatId && chat.mode !== 'orchestration' ? getChat(chat.parentChatId) : undefined

  useEffect(() => {
    if (!chat.parentChatId || chat.mode === 'orchestration') {
      return
    }

    const parent = getChat(chat.parentChatId)
    if (!parent || parent.mode !== 'orchestration') {
      void loadChat(chat.parentChatId)
    }
  }, [chat.mode, chat.parentChatId, getChat, loadChat])

  const { workflowProgress, sequencePlanIds: backendSequencePlanIds } = useOrchestration({
    parentChat: chat.mode === 'orchestration' ? chat : undefined,
    onWorkflowProgress: (progress) => setOrchestrationKickoffProgress(chat.id, progress),
    onChildrenHydrated: (childIds) => {
      for (const childId of childIds) {
        const child = getChat(childId)
        if (child && child.messages.length === 0) {
          void loadChat(childId)
        }
      }
    }
  })

  useOrchestrationParentEvents({
    childChat: chat.parentChatId ? chat : undefined,
    parentChat: parentChat?.mode === 'orchestration' ? parentChat : undefined
  })
  const sequencePlanIds =
    backendSequencePlanIds.length > 0 ? backendSequencePlanIds : orchestrationParse.sequencePlanIds
  const orchestrationKickoffProgress =
    workflowProgress ?? getOrchestrationKickoffProgress(chat.id)
  const childChats = getChildChats(chat.id).map((child) => getChat(child.id) ?? child)
  const reviewPlansByPlanId = Object.fromEntries(
    plans.map((plan) => {
      const reviewChildSummary = findReviewChildForPlan(plan.planId, childChats)
      const reviewChild = reviewChildSummary ? getChat(reviewChildSummary.id) : undefined
      const reviewPlans = reviewChild
        ? parseReviewPlansFromMessages(reviewChild.messages)
        : []
      const reviewPlan = reviewPlans.find((item) => item.planId === plan.planId) ?? reviewPlans[0]
      return [plan.planId, reviewPlan] as const
    })
  ) as Record<string, ParsedReviewPlan | undefined>

  const childChatIds = useMemo(
    () => getChildChats(chat.id).map((child) => child.id).join(','),
    [chat.id, chats, getChildChats]
  )

  useEffect(() => {
    if (chat.mode !== 'orchestration') {
      return
    }

    for (const id of childChatIds.split(',').filter(Boolean)) {
      const child = getChat(id)
      if (child && child.messages.length === 0) {
        void loadChat(id)
      }
    }
  }, [chat.id, chat.mode, childChatIds, getChat, loadChat])

  const isAgentRunning = chat.messages.some(
    (message) => message.status === 'processing' || message.status === 'streaming'
  )
  const showModeSelector = chat.messages.length === 0 && chat.parentChatId === null
  const canChangeMode = showModeSelector && !isAgentRunning
  const canChangeModel = !isAgentRunning
  const showPlanReview = chat.mode === 'orchestration' && plans.length > 0

  const {
    reviewState,
    dispatchReview,
    toggleReviewPanel,
    activeReviewTabId,
    hasReviewReady
  } = usePlanReview({
    plans,
    reviewPlansByPlanId,
    showPlanReview
  })

  return (
    <div className="flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden">
      <ChatWorkspaceHeader
        chat={chat}
        projectName={projectName}
        childChatCount={childChats.length}
        workspacePath={chat.workspacePath}
        showPlanReview={showPlanReview}
        reviewPanelOpen={reviewState.panelOpen}
        hasReviewReady={hasReviewReady}
        onToggleReviewPanel={toggleReviewPanel}
        onDelete={() => requestDelete(chat)}
        deleteDisabled={isChatSending(chat.id) || isDeletingChat(chat.id)}
      />

      <ChatPanel
        messages={chat.messages}
        markers={getMarkers(chat.id)}
        onSend={(content) => sendMessage(chat.id, content)}
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
        projectId={chat.projectId}
        projectName={projectName}
        projects={projects}
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
