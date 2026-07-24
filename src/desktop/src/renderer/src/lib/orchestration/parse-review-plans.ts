import {
  ORCHI_MARKERS,
  extractMarkdownTitle,
  getIdBlockParseConfig
} from '@/lib/orchestration/orchi-markers'
import { parseMarkedBlocks } from '@/lib/orchestration/parse-marked-blocks'

export type ParsedReviewPlan = {
  planId: string
  title: string
  contentMarkdown: string
}

export function parseReviewPlans(content: string): ParsedReviewPlan[] {
  return parseMarkedBlocks(content, getIdBlockParseConfig(ORCHI_MARKERS.reviewPlan)).map(
    ({ id, body }) => ({
      planId: id,
      title: extractMarkdownTitle(body, ORCHI_MARKERS.reviewPlan.defaultTitle),
      contentMarkdown: body
    })
  )
}

export function parseReviewPlansFromMessages(
  messages: Array<{ role: string; content: string; status?: string }>
): ParsedReviewPlan[] {
  const plans = new Map<string, ParsedReviewPlan>()

  for (const message of messages) {
    if (message.role !== 'assistant') {
      continue
    }

    for (const plan of parseReviewPlans(message.content)) {
      plans.set(plan.planId, plan)
    }
  }

  return [...plans.values()]
}

function isCompleteAssistantMessage(message: {
  role: string
  status?: string
}): boolean {
  return (
    message.role === 'assistant' &&
    message.status !== 'processing' &&
    message.status !== 'streaming'
  )
}

/**
 * Returns review content from marked blocks when present, otherwise the latest
 * complete assistant turn (direct review markdown).
 */
export function resolveReviewContentFromMessages(
  messages: Array<{ role: string; content: string; status?: string }>,
  fallbackPlanId = 'review'
): ParsedReviewPlan | undefined {
  const fromBlocks = parseReviewPlansFromMessages(messages)
  if (fromBlocks.length > 0) {
    return fromBlocks[fromBlocks.length - 1]
  }

  const assistant = [...messages].reverse().find(isCompleteAssistantMessage)
  const content = assistant?.content.trim()
  if (!content) {
    return undefined
  }

  return {
    planId: fallbackPlanId,
    title: extractMarkdownTitle(content, ORCHI_MARKERS.reviewPlan.defaultTitle),
    contentMarkdown: content
  }
}
