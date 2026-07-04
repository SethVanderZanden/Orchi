import { useCallback } from 'react'
import type { Dispatch } from 'react'
import { useQuery } from '@tanstack/react-query'

import { ChatLayout } from '@/components/chat/chat-layout'
import { OrchiChatComposer } from '@/components/chat/chat-composer'
import { ChatModeSelector, CHAT_MODE_FALLBACK_OPTIONS, getNextAgentMode } from '@/components/chat/chat-mode-selector'
import { ChatModelSelector } from '@/components/chat/chat-model-selector'
import { OrchiChatMessageList } from '@/components/chat/chat-message-list'
import { PlanCards } from '@/components/orchestration/plan-cards'
import { PlanReviewPanel } from '@/components/orchestration/plan-review-panel'
import {
  MessageScroller,
  MessageScrollerButton,
  MessageScrollerContent,
  MessageScrollerProvider,
  MessageScrollerViewport
} from '@/components/ui/message-scroller'
import { useKeyboardShortcutCombo } from '@/hooks/use-keyboard-shortcut'
import type { ReviewAction, ReviewState } from '@/hooks/use-plan-review'
import { listAgentModes } from '@/lib/chat/api'
import type { AgentMode, ChatMarker, ChatMessage, ChatThread } from '@/lib/chat/types'
import type { ParsedPlan } from '@/lib/orchestration/parse-plans'
import type { ParsedReviewPlan } from '@/lib/orchestration/parse-review-plans'

type ChatPanelProps = {
  messages: ChatMessage[]
  markers: ChatMarker[]
  onSend: (content: string) => void
  mode: AgentMode
  canChangeMode: boolean
  modeUpdateError?: string | null
  onModeChange: (mode: AgentMode) => void
  agentId: string
  modelId: string | null
  canChangeModel: boolean
  modelUpdateError?: string | null
  onModelChange: (modelId: string | null) => void
  plans?: ParsedPlan[]
  parentChatId?: string
  isSending?: boolean
  isPlanKickingOff?: (parentChatId: string, planId: string) => boolean
  isParentKickingOffAny?: (parentChatId: string) => boolean
  onKickOffPlan?: (plan: ParsedPlan) => void
  onKickOffAllPlans?: () => void
  childChats?: ChatThread[]
  reviewPlansByPlanId?: Record<string, ParsedReviewPlan | undefined>
  showPlanReview?: boolean
  reviewState: ReviewState
  dispatchReview: Dispatch<ReviewAction>
  activeReviewTabId: string | null
}

export function ChatPanel({
  messages,
  markers,
  onSend,
  mode,
  canChangeMode,
  modeUpdateError = null,
  onModeChange,
  agentId,
  modelId,
  canChangeModel,
  modelUpdateError = null,
  onModelChange,
  plans = [],
  parentChatId,
  isSending = false,
  isPlanKickingOff,
  isParentKickingOffAny,
  onKickOffPlan,
  onKickOffAllPlans,
  childChats = [],
  reviewPlansByPlanId = {},
  showPlanReview = false,
  reviewState,
  dispatchReview,
  activeReviewTabId
}: ChatPanelProps): React.JSX.Element {
  const modesQuery = useQuery({
    queryKey: ['agent-modes'],
    queryFn: listAgentModes,
    staleTime: Infinity
  })

  const cycleMode = useCallback(() => {
    const modeOptions = modesQuery.data ?? CHAT_MODE_FALLBACK_OPTIONS
    onModeChange(getNextAgentMode(mode, modeOptions))
  }, [mode, modesQuery.data, onModeChange])

  // Shift+Tab intentionally overrides default backward focus navigation while mode changes are allowed.
  useKeyboardShortcutCombo({ key: 'Tab', shift: true }, cycleMode, {
    enabled: canChangeMode,
    allowInTextarea: true
  })

  return (
    <div className="flex min-h-0 flex-1 overflow-hidden">
      <div className="flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden">
        <MessageScrollerProvider autoScroll>
          <ChatLayout
            footer={
              <div className="flex flex-wrap items-start gap-6">
                <ChatModeSelector
                  mode={mode}
                  disabled={!canChangeMode}
                  error={modeUpdateError}
                  onModeChange={onModeChange}
                />
                <ChatModelSelector
                  agentId={agentId}
                  modelId={modelId}
                  disabled={!canChangeModel}
                  error={modelUpdateError}
                  onModelChange={onModelChange}
                />
              </div>
            }
            composer={<OrchiChatComposer disabled={isSending} onSend={onSend} />}
          >
            <MessageScroller className="min-h-0 flex-1">
              <MessageScrollerViewport>
                <MessageScrollerContent aria-busy={isSending}>
                  <OrchiChatMessageList messages={messages} markers={markers} />
                  {showPlanReview ? (
                    <PlanCards
                      plans={plans}
                      openTabIds={reviewState.openTabIds}
                      childChats={childChats}
                      reviewPlansByPlanId={reviewPlansByPlanId}
                      parentChatId={parentChatId!}
                      isParentKickingOffAny={isParentKickingOffAny!}
                      onToggleReview={(plan) =>
                        dispatchReview({ type: 'toggle-tab', planId: plan.planId })
                      }
                      onKickOffAll={onKickOffAllPlans!}
                    />
                  ) : null}
                </MessageScrollerContent>
              </MessageScrollerViewport>
              <MessageScrollerButton />
            </MessageScroller>
          </ChatLayout>
        </MessageScrollerProvider>
      </div>

      {showPlanReview && reviewState.panelOpen && activeReviewTabId ? (
        <PlanReviewPanel
          plans={plans}
          openTabIds={reviewState.openTabIds}
          activeTabId={activeReviewTabId}
          parentChatId={parentChatId!}
          childChats={childChats}
          reviewPlansByPlanId={reviewPlansByPlanId}
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
