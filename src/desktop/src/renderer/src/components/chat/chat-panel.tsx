import { useCallback, useEffect, useReducer, useRef } from 'react'
import { useQuery } from '@tanstack/react-query'

import { ChatLayout } from '@/components/chat/chat-layout'
import { OrchiChatComposer } from '@/components/chat/chat-composer'
import { ChatModeSelector, CHAT_MODE_FALLBACK_OPTIONS, getNextAgentMode } from '@/components/chat/chat-mode-selector'
import { OrchiChatMessageList } from '@/components/chat/chat-message-list'
import { PlanCards } from '@/components/orchestration/plan-cards'
import { PlanReviewPanel } from '@/components/orchestration/plan-review-panel'
import { useKeyboardShortcut, useKeyboardShortcutCombo } from '@/hooks/use-keyboard-shortcut'
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
  plans?: ParsedPlan[]
  parentChatId?: string
  isSending?: boolean
  isPlanKickingOff?: (parentChatId: string, planId: string) => boolean
  isParentKickingOffAny?: (parentChatId: string) => boolean
  onKickOffPlan?: (plan: ParsedPlan) => void
  onKickOffAllPlans?: () => void
  childChats?: ChatThread[]
  reviewPlansByPlanId?: Record<string, ParsedReviewPlan | undefined>
}

type ReviewState = {
  panelOpen: boolean
  openTabIds: string[]
  activeTabId: string | null
}

type ReviewAction =
  | { type: 'sync-plans'; planIds: string[] }
  | { type: 'toggle-tab'; planId: string }
  | { type: 'open-panel'; planId?: string }
  | { type: 'toggle-panel'; planId?: string }
  | { type: 'select-tab'; planId: string }
  | { type: 'close-tab'; planId: string }
  | { type: 'highlight-review'; planId: string }
  | { type: 'close-panel' }

const initialReviewState: ReviewState = {
  panelOpen: false,
  openTabIds: [],
  activeTabId: null
}

function reviewReducer(state: ReviewState, action: ReviewAction): ReviewState {
  switch (action.type) {
    case 'sync-plans': {
      const openTabIds = state.openTabIds.filter((planId) => action.planIds.includes(planId))
      const activeTabId =
        state.activeTabId && openTabIds.includes(state.activeTabId) ? state.activeTabId : null

      return {
        panelOpen: state.panelOpen && openTabIds.length > 0,
        openTabIds,
        activeTabId
      }
    }
    case 'toggle-tab': {
      if (!state.openTabIds.includes(action.planId)) {
        return {
          panelOpen: true,
          openTabIds: [...state.openTabIds, action.planId],
          activeTabId: action.planId
        }
      }

      if (!state.panelOpen) {
        return {
          ...state,
          panelOpen: true,
          activeTabId: action.planId
        }
      }

      if (state.activeTabId !== action.planId) {
        return {
          ...state,
          activeTabId: action.planId
        }
      }

      const openTabIds = state.openTabIds.filter((planId) => planId !== action.planId)
      const activeTabId = openTabIds[openTabIds.length - 1] ?? null

      return {
        panelOpen: openTabIds.length > 0,
        openTabIds,
        activeTabId
      }
    }
    case 'open-panel': {
      if (state.openTabIds.length > 0) {
        return {
          ...state,
          panelOpen: true,
          activeTabId: state.activeTabId ?? state.openTabIds[state.openTabIds.length - 1]
        }
      }

      if (!action.planId) {
        return state
      }

      return {
        panelOpen: true,
        openTabIds: [action.planId],
        activeTabId: action.planId
      }
    }
    case 'toggle-panel': {
      if (state.panelOpen) {
        return {
          ...state,
          panelOpen: false
        }
      }

      if (state.openTabIds.length > 0) {
        return {
          ...state,
          panelOpen: true,
          activeTabId: state.activeTabId ?? state.openTabIds[state.openTabIds.length - 1]
        }
      }

      if (!action.planId) {
        return state
      }

      return {
        panelOpen: true,
        openTabIds: [action.planId],
        activeTabId: action.planId
      }
    }
    case 'select-tab':
      return {
        ...state,
        activeTabId: action.planId
      }
    case 'close-tab': {
      const openTabIds = state.openTabIds.filter((planId) => planId !== action.planId)
      const activeTabId =
        state.activeTabId === action.planId
          ? (openTabIds[openTabIds.length - 1] ?? null)
          : state.activeTabId

      return {
        panelOpen: state.panelOpen && openTabIds.length > 0,
        openTabIds,
        activeTabId
      }
    }
    case 'close-panel':
      return {
        ...state,
        panelOpen: false
      }
    case 'highlight-review': {
      const openTabIds = state.openTabIds.includes(action.planId)
        ? state.openTabIds
        : [...state.openTabIds, action.planId]

      return {
        panelOpen: true,
        openTabIds,
        activeTabId: action.planId
      }
    }
    default:
      return state
  }
}

export function ChatPanel({
  messages,
  markers,
  onSend,
  mode,
  canChangeMode,
  modeUpdateError = null,
  onModeChange,
  plans = [],
  parentChatId,
  isSending = false,
  isPlanKickingOff,
  isParentKickingOffAny,
  onKickOffPlan,
  onKickOffAllPlans,
  childChats = [],
  reviewPlansByPlanId = {}
}: ChatPanelProps): React.JSX.Element {
  const showPlanReview = Boolean(onKickOffPlan && plans.length > 0)
  const [reviewState, dispatchReview] = useReducer(reviewReducer, initialReviewState)
  const highlightedReviewPlanIdsRef = useRef<Set<string>>(new Set())

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

  useEffect(() => {
    dispatchReview({ type: 'sync-plans', planIds: plans.map((plan) => plan.planId) })
  }, [plans])

  useEffect(() => {
    for (const plan of plans) {
      if (!reviewPlansByPlanId[plan.planId]) {
        continue
      }

      if (highlightedReviewPlanIdsRef.current.has(plan.planId)) {
        continue
      }

      highlightedReviewPlanIdsRef.current.add(plan.planId)
      dispatchReview({ type: 'highlight-review', planId: plan.planId })
      break
    }
  }, [plans, reviewPlansByPlanId])

  const toggleReviewPanel = useCallback(() => {
    if (!showPlanReview || plans.length === 0) {
      return
    }

    dispatchReview({
      type: 'toggle-panel',
      planId: plans[0]?.planId
    })
  }, [plans, showPlanReview])

  useKeyboardShortcut('r', toggleReviewPanel, { enabled: showPlanReview })

  const activeReviewTabId =
    reviewState.activeTabId ??
    reviewState.openTabIds[reviewState.openTabIds.length - 1] ??
    null

  return (
    <div className="flex min-h-0 flex-1 overflow-hidden">
      <div className="flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden">
        <ChatLayout
          footer={
            <ChatModeSelector
              mode={mode}
              disabled={!canChangeMode}
              error={modeUpdateError}
              onModeChange={onModeChange}
            />
          }
          composer={<OrchiChatComposer disabled={isSending} onSend={onSend} />}
        >
          <OrchiChatMessageList messages={messages} markers={markers} />
          {showPlanReview ? (
            <PlanCards
              plans={plans}
              openTabIds={reviewState.openTabIds}
              childChats={childChats}
              reviewPlansByPlanId={reviewPlansByPlanId}
              parentChatId={parentChatId!}
              isParentKickingOffAny={isParentKickingOffAny!}
              onToggleReview={(plan) => dispatchReview({ type: 'toggle-tab', planId: plan.planId })}
              onKickOffAll={onKickOffAllPlans!}
            />
          ) : null}
        </ChatLayout>
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
