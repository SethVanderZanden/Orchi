import { useCallback, useEffect, useReducer, useRef } from 'react'

import { useKeyboardShortcut } from '@/hooks/use-keyboard-shortcut'
import type { ParsedPlan } from '@/lib/orchestration/parse-plans'
import type { ParsedReviewPlan } from '@/lib/orchestration/parse-review-plans'

export type ReviewState = {
  panelOpen: boolean
  openTabIds: string[]
  activeTabId: string | null
}

export type ReviewAction =
  | { type: 'sync-plans'; planIds: string[] }
  | { type: 'toggle-tab'; planId: string }
  | { type: 'open-panel'; planId?: string }
  | { type: 'toggle-panel'; planId?: string }
  | { type: 'select-tab'; planId: string }
  | { type: 'close-tab'; planId: string }
  | { type: 'highlight-review'; planId: string }
  | { type: 'close-panel' }

export const initialReviewState: ReviewState = {
  panelOpen: false,
  openTabIds: [],
  activeTabId: null
}

export function reviewReducer(state: ReviewState, action: ReviewAction): ReviewState {
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

type UsePlanReviewOptions = {
  plans: ParsedPlan[]
  reviewPlansByPlanId: Record<string, ParsedReviewPlan | undefined>
  showPlanReview: boolean
}

export function usePlanReview({
  plans,
  reviewPlansByPlanId,
  showPlanReview
}: UsePlanReviewOptions) {
  const [reviewState, dispatchReview] = useReducer(reviewReducer, initialReviewState)
  const highlightedReviewPlanIdsRef = useRef<Set<string>>(new Set())

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

  const hasReviewReady = plans.some((plan) => Boolean(reviewPlansByPlanId[plan.planId]))

  return {
    reviewState,
    dispatchReview,
    toggleReviewPanel,
    activeReviewTabId,
    hasReviewReady
  }
}
