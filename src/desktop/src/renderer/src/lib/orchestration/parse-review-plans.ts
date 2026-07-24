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
  messages: Array<{ role: string; content: string }>
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
