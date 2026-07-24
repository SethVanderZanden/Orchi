import type { ChatThread } from '@/lib/chat/types'
import { parsePlansFromMessages } from '@/lib/orchestration/parse-plans'
import { resolveReviewContentFromMessages } from '@/lib/orchestration/parse-review-plans'
import type { ParsedReviewPlan } from '@/lib/orchestration/parse-review-plans'
import { isChildRunning } from '@/lib/orchestration/plan-review-visibility'
import { findReviewChildForPlan } from '@/lib/projects/chat-tree'

export function buildReviewPlansByPlanId(
  chat: ChatThread,
  childChats: ChatThread[],
  getChat: (chatId: string) => ChatThread | undefined
): Record<string, ParsedReviewPlan | undefined> {
  const plans = parsePlansFromMessages(chat.messages)

  return Object.fromEntries(
    plans.map((plan) => {
      const reviewChildSummary = findReviewChildForPlan(plan.planId, childChats)
      const reviewChild = reviewChildSummary ? getChat(reviewChildSummary.id) : undefined
      const reviewPlan = reviewChild
        ? resolveReviewContentFromMessages(reviewChild.messages, plan.planId)
        : undefined

      return [plan.planId, reviewPlan] as const
    })
  )
}

export function hasReviewReadyPlan(
  chat: ChatThread,
  childChats: ChatThread[],
  getChat: (chatId: string) => ChatThread | undefined
): boolean {
  if (chat.mode !== 'orchestration') {
    return false
  }

  const reviewPlansByPlanId = buildReviewPlansByPlanId(chat, childChats, getChat)
  return Object.values(reviewPlansByPlanId).some(Boolean)
}

export function listReviewChildIdsNeedingReload(
  chat: ChatThread,
  childChats: ChatThread[],
  getChat: (chatId: string) => ChatThread | undefined
): string[] {
  if (chat.mode !== 'orchestration') {
    return []
  }

  const plans = parsePlansFromMessages(chat.messages)
  const childIds: string[] = []

  for (const plan of plans) {
    const reviewChildSummary = findReviewChildForPlan(plan.planId, childChats)
    if (!reviewChildSummary) {
      continue
    }

    const reviewChild = getChat(reviewChildSummary.id) ?? reviewChildSummary
    if (isChildRunning(reviewChild)) {
      continue
    }

    const reviewPlan = resolveReviewContentFromMessages(reviewChild.messages, plan.planId)
    if (reviewPlan) {
      continue
    }

    childIds.push(reviewChild.id)
  }

  return childIds
}
