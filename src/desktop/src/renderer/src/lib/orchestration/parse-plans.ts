import {
  ORCHI_MARKERS,
  extractMarkdownTitle,
  getIdBlockParseConfig
} from '@/lib/orchestration/orchi-markers'
import { parseMarkedBlocks } from '@/lib/orchestration/parse-marked-blocks'
import { parsePlanSequenceFromMessages } from './plan-sequence'

export type ParsedPlan = {
  planId: string
  title: string
  contentMarkdown: string
}

export function parsePlans(content: string): ParsedPlan[] {
  return parseMarkedBlocks(content, getIdBlockParseConfig(ORCHI_MARKERS.plan)).map(
    ({ id, body }) => ({
      planId: id,
      title: extractMarkdownTitle(body, ORCHI_MARKERS.plan.defaultTitle),
      contentMarkdown: body
    })
  )
}

export function parsePlansFromMessages(
  messages: Array<{ role: string; content: string }>
): ParsedPlan[] {
  const plans = new Map<string, ParsedPlan>()

  for (const message of messages) {
    if (message.role !== 'assistant') {
      continue
    }

    for (const plan of parsePlans(message.content)) {
      plans.set(plan.planId, plan)
    }
  }

  return [...plans.values()]
}

export function parseOrchestrationPlansFromMessages(
  messages: Array<{ role: string; content: string }>
): { plans: ParsedPlan[]; sequencePlanIds: string[] } {
  return {
    plans: parsePlansFromMessages(messages),
    sequencePlanIds: parsePlanSequenceFromMessages(messages)
  }
}
