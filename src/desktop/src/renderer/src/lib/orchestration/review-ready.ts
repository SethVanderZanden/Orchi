import type { ChatThread } from '@/lib/chat/types'
import { parsePlansFromMessages } from '@/lib/orchestration/parse-plans'
import { parseReviewPlansFromMessages } from '@/lib/orchestration/parse-review-plans'
import type { ParsedReviewPlan } from '@/lib/orchestration/parse-review-plans'
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
      const reviewPlans = reviewChild ? parseReviewPlansFromMessages(reviewChild.messages) : []
      const reviewPlan = reviewPlans.find((item) => item.planId === plan.planId) ?? reviewPlans[0]

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
