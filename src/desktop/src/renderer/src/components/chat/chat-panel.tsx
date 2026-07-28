import { useCallback, useMemo } from 'react'

import type { Dispatch } from 'react'

import { useQuery } from '@tanstack/react-query'

import { ChatLayout } from '@/components/chat/chat-layout'

import { OrchiChatComposer, type ComposerSendPayload } from '@/components/chat/chat-composer'

import { getNextAgentMode, resolveAgentModeOptions } from '@/lib/chat/agent-mode-utils'

import { ChatWorkspaceContext } from '@/components/chat/chat-workspace-context'

import { OrchiChatMessageList } from '@/components/chat/chat-message-list'

import { AttachmentsPanel } from '@/components/chat/attachments-panel'
import { collectChatAttachments } from '@/components/chat/message-attachments'
import { PlanCards } from '@/components/orchestration/plan-cards'

import { PlanReviewPanel } from '@/components/orchestration/plan-review-panel'

import {
  MessageScroller,
  MessageScrollerButton,
  MessageScrollerContent,
  MessageScrollerItem,
  MessageScrollerProvider,
  MessageScrollerViewport
} from '@/components/ui/message-scroller'

import { useElementWidth } from '@/hooks/use-element-width'

import { useKeyboardShortcutCombo } from '@/hooks/use-keyboard-shortcut'

import type { ReviewAction, ReviewState } from '@/hooks/use-plan-review'

import { listAgentModes } from '@/lib/chat/api'
import { agentKeys } from '@/lib/query-keys'

import type { AgentMode, ChatMarker, ChatMessage, ChatThread } from '@/lib/chat/types'

import type { ParsedPlan } from '@/lib/orchestration/parse-plans'

import type { ParsedReviewPlan } from '@/lib/orchestration/parse-review-plans'

import { findChildForPlan } from '@/lib/projects/chat-tree'

import type { Project } from '@/lib/projects/types'

type ChatPanelProps = {
  messages: ChatMessage[]

  markers: ChatMarker[]

  onSend: (payload: ComposerSendPayload) => void

  mode: AgentMode

  showModeSelector: boolean

  canChangeMode: boolean

  modeUpdateError?: string | null

  onModeChange: (mode: AgentMode) => void

  agentId: string

  modelId: string | null

  canChangeModel: boolean

  modelUpdateError?: string | null

  onModelChange: (modelId: string | null) => void

  contextSizeId: string | null

  canChangeContextSize: boolean

  contextSizeUpdateError?: string | null

  onContextSizeChange: (contextSizeId: string | null) => void

  reasoningEffortId: string | null

  canChangeReasoningEffort: boolean

  reasoningEffortUpdateError?: string | null

  onReasoningEffortChange: (reasoningEffortId: string | null) => void

  approvalPolicyId: string | null

  canChangeApprovalPolicy: boolean

  approvalPolicyUpdateError?: string | null

  onApprovalPolicyChange: (approvalPolicyId: string | null) => void

  projectId: string | null

  workspaceId: string | null

  workspaceName: string | null

  workspacePath?: string | null

  projectName: string | null

  projects: Project[]

  canChangeWorkspace?: boolean

  onWorkspaceChange?: (workspaceId: string) => void

  chatId: string

  plans?: ParsedPlan[]

  parentChatId?: string

  isSending?: boolean

  isPlanKickingOff?: (parentChatId: string, planId: string) => boolean

  isParentKickingOffAny?: (parentChatId: string) => boolean

  onKickOffPlan?: (plan: ParsedPlan) => void

  onKickOffAllPlans?: () => void

  sequencePlanIds?: string[]

  sequentialKickoffProgress?: { active: boolean; currentStep: number; totalSteps: number } | null

  orchestrationError?: string | null

  childChats?: ChatThread[]

  reviewPlansByPlanId?: Record<string, ParsedReviewPlan | undefined>

  showPlanReview?: boolean

  reviewState: ReviewState

  dispatchReview: Dispatch<ReviewAction>

  activeReviewTabId: string | null

  attachmentsPanelOpen?: boolean

  onCloseAttachmentsPanel?: () => void
}

export function ChatPanel({
  messages,

  markers,

  onSend,

  mode,

  showModeSelector,

  canChangeMode,

  modeUpdateError = null,

  onModeChange,

  agentId,

  modelId,

  canChangeModel,

  modelUpdateError = null,

  onModelChange,

  contextSizeId,

  canChangeContextSize,

  contextSizeUpdateError = null,

  onContextSizeChange,

  reasoningEffortId,

  canChangeReasoningEffort,

  reasoningEffortUpdateError = null,

  onReasoningEffortChange,

  approvalPolicyId,

  canChangeApprovalPolicy,

  approvalPolicyUpdateError = null,

  onApprovalPolicyChange,

  projectId,

  workspaceId,

  workspaceName,

  workspacePath,

  projectName,

  projects,

  canChangeWorkspace = false,

  onWorkspaceChange,

  chatId,

  plans = [],

  parentChatId,

  isSending = false,

  isPlanKickingOff,

  isParentKickingOffAny,

  onKickOffPlan,

  onKickOffAllPlans,

  sequencePlanIds = [],

  sequentialKickoffProgress = null,

  orchestrationError = null,

  childChats = [],

  reviewPlansByPlanId = {},

  showPlanReview = false,

  reviewState,

  dispatchReview,

  activeReviewTabId,

  attachmentsPanelOpen = false,

  onCloseAttachmentsPanel
}: ChatPanelProps): React.JSX.Element {
  const modesQuery = useQuery({
    queryKey: agentKeys.modes(),

    queryFn: listAgentModes,

    staleTime: Infinity
  })

  const cycleMode = useCallback((): boolean => {
    const modeOptions = resolveAgentModeOptions(modesQuery.data)

    const nextMode = getNextAgentMode(mode, modeOptions)

    if (nextMode.toLowerCase() === mode.toLowerCase()) {
      return false
    }

    onModeChange(nextMode)

    return true
  }, [mode, modesQuery.data, onModeChange])

  // Shift+Tab intentionally overrides default backward focus navigation while mode changes are allowed.

  useKeyboardShortcutCombo({ key: 'Tab', shift: true }, cycleMode, {
    enabled: showModeSelector && canChangeMode,

    allowInTextarea: true
  })

  const { width: splitContainerWidth, ref: splitContainerRef } = useElementWidth<HTMLDivElement>()

  const activeReviewPlan = activeReviewTabId
    ? plans.find((plan) => plan.planId === activeReviewTabId)
    : undefined
  const activeReviewPlanHasChild = activeReviewPlan
    ? Boolean(findChildForPlan(activeReviewPlan.planId, childChats))
    : false
  const activeReviewPlanKickingOff =
    activeReviewPlan && isPlanKickingOff && parentChatId
      ? isPlanKickingOff(parentChatId, activeReviewPlan.planId)
      : false
  const activeReviewPlanShowingReview = activeReviewPlan
    ? Boolean(reviewPlansByPlanId[activeReviewPlan.planId])
    : false

  const kickOffActivePlan = useCallback((): boolean => {
    if (!onKickOffPlan || !reviewState.panelOpen || !activeReviewPlan) {
      return false
    }

    if (activeReviewPlanShowingReview || activeReviewPlanHasChild || activeReviewPlanKickingOff) {
      return false
    }

    onKickOffPlan(activeReviewPlan)
    return true
  }, [
    activeReviewPlan,
    activeReviewPlanHasChild,
    activeReviewPlanKickingOff,
    activeReviewPlanShowingReview,
    onKickOffPlan,
    reviewState.panelOpen
  ])

  useKeyboardShortcutCombo({ key: 'Enter', shift: true }, kickOffActivePlan, {
    enabled:
      showPlanReview &&
      reviewState.panelOpen &&
      Boolean(onKickOffPlan) &&
      Boolean(activeReviewPlan) &&
      !activeReviewPlanShowingReview &&
      !activeReviewPlanHasChild &&
      !activeReviewPlanKickingOff,
    allowInTextarea: true
  })

  const kickOffAllCount = plans.filter((plan) => !findChildForPlan(plan.planId, childChats)).length
  const sequentialRunActive = sequentialKickoffProgress?.active ?? false
  const kickingOffAny =
    parentChatId && isParentKickingOffAny ? isParentKickingOffAny(parentChatId) : false

  const kickOffAllPlans = useCallback((): boolean => {
    if (!onKickOffAllPlans || kickingOffAny || kickOffAllCount === 0 || sequentialRunActive) {
      return false
    }

    onKickOffAllPlans()
    return true
  }, [kickOffAllCount, kickingOffAny, onKickOffAllPlans, sequentialRunActive])

  useKeyboardShortcutCombo({ key: 'Enter', ctrl: true }, kickOffAllPlans, {
    enabled:
      showPlanReview &&
      Boolean(onKickOffAllPlans) &&
      !kickingOffAny &&
      kickOffAllCount > 0 &&
      !sequentialRunActive,
    allowInTextarea: true
  })

  const isNewRootChat = messages.length === 0 && markers.length === 0 && showModeSelector
  const chatAttachments = useMemo(() => collectChatAttachments(messages), [messages])

  const composer = (
    <OrchiChatComposer
      key={chatId}
      chatId={chatId}
      autoFocus={isNewRootChat}
      disabled={isSending}
      onSend={onSend}
      expanded={isNewRootChat}
      mode={mode}
      showModeControls={showModeSelector}
      canChangeMode={canChangeMode}
      modeUpdateError={modeUpdateError}
      onModeChange={onModeChange}
      agentId={agentId}
      modelId={modelId}
      canChangeModel={canChangeModel}
      modelUpdateError={modelUpdateError}
      onModelChange={onModelChange}
      contextSizeId={contextSizeId}
      canChangeContextSize={canChangeContextSize}
      contextSizeUpdateError={contextSizeUpdateError}
      onContextSizeChange={onContextSizeChange}
      reasoningEffortId={reasoningEffortId}
      canChangeReasoningEffort={canChangeReasoningEffort}
      reasoningEffortUpdateError={reasoningEffortUpdateError}
      onReasoningEffortChange={onReasoningEffortChange}
      approvalPolicyId={approvalPolicyId}
      canChangeApprovalPolicy={canChangeApprovalPolicy}
      approvalPolicyUpdateError={approvalPolicyUpdateError}
      onApprovalPolicyChange={onApprovalPolicyChange}
      projectId={projectId}
      projects={projects}
      messageCount={messages.length}
    />
  )

  const workspaceContext = useMemo(
    () =>
      isNewRootChat ? (
        <ChatWorkspaceContext
          workspaceId={workspaceId}
          workspaceName={workspaceName}
          projectName={projectName}
          projects={projects}
          canChangeWorkspace={canChangeWorkspace}
          onWorkspaceChange={onWorkspaceChange}
        />
      ) : null,
    [
      canChangeWorkspace,
      isNewRootChat,
      onWorkspaceChange,
      projectName,
      projects,
      workspaceId,
      workspaceName
    ]
  )

  return (
    <div ref={splitContainerRef} className="flex min-h-0 flex-1 overflow-hidden">
      <div className="flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden">
        <MessageScrollerProvider autoScroll>
          <ChatLayout
            variant={isNewRootChat ? 'centered' : 'default'}
            projectContext={workspaceContext}
            composer={composer}
          >
            {!isNewRootChat ? (
              <MessageScroller className="min-h-0 flex-1">
                <MessageScrollerViewport>
                  <MessageScrollerContent
                    aria-busy={isSending}
                    className="mx-auto w-full max-w-3xl gap-7 px-6 py-8"
                  >
                    <OrchiChatMessageList
                      chatId={chatId}
                      messages={messages}
                      markers={markers}
                      mode={mode}
                      workspacePath={workspacePath}
                    />

                    {showPlanReview ? (
                      <MessageScrollerItem messageId={`${chatId}-plan-cards`}>
                        <PlanCards
                          plans={plans}
                          openTabIds={reviewState.openTabIds}
                          childChats={childChats}
                          reviewPlansByPlanId={reviewPlansByPlanId}
                          parentChatId={parentChatId!}
                          projectId={projectId}
                          isParentKickingOffAny={isParentKickingOffAny!}
                          sequencePlanIds={sequencePlanIds}
                          sequentialKickoffProgress={sequentialKickoffProgress}
                          orchestrationError={orchestrationError}
                          onToggleReview={(plan) =>
                            dispatchReview({ type: 'toggle-tab', planId: plan.planId })
                          }
                          onKickOffAll={onKickOffAllPlans!}
                        />
                      </MessageScrollerItem>
                    ) : null}
                  </MessageScrollerContent>
                </MessageScrollerViewport>

                <MessageScrollerButton />
              </MessageScroller>
            ) : null}
          </ChatLayout>
        </MessageScrollerProvider>
      </div>

      {attachmentsPanelOpen && chatAttachments.length > 0 ? (
        <AttachmentsPanel
          containerWidth={splitContainerWidth}
          chatId={chatId}
          attachments={chatAttachments}
          onClose={() => onCloseAttachmentsPanel?.()}
        />
      ) : null}

      {showPlanReview && reviewState.panelOpen && activeReviewTabId ? (
        <PlanReviewPanel
          containerWidth={splitContainerWidth}
          plans={plans}
          openTabIds={reviewState.openTabIds}
          activeTabId={activeReviewTabId}
          parentChatId={parentChatId!}
          childChats={childChats}
          reviewPlansByPlanId={reviewPlansByPlanId}
          workspacePath={workspacePath}
          isPlanKickingOff={isPlanKickingOff!}
          onSelectTab={(planId) => dispatchReview({ type: 'select-tab', planId })}
          onCloseTab={(planId) => dispatchReview({ type: 'close-tab', planId })}
          onClose={() => dispatchReview({ type: 'close-panel' })}
          onKickOff={onKickOffPlan!}
        />
      ) : null}
    </div>
  )
}
