import { parsePlanSequenceFromMessages } from './plan-sequence'
import { parseMarkedBlocks } from './parse-marked-blocks'

export type ParsedPlan = {
  planId: string
  title: string
  contentMarkdown: string
}

const PLAN_PARSE_CONFIG = {
  completeBlockPattern:
    /<!--\s*orchi-plan:([a-z0-9]+(?:-[a-z0-9]+)*)\s*-->\s*([\s\S]*?)<!--\s*\/orchi-plan\s*-->/gi,
  openMarkerPattern: /<!--\s*orchi-plan:([a-z0-9]+(?:-[a-z0-9]+)*)\s*-->/gi
} as const

function extractTitle(content: string): string {
  const headingMatch = content.match(/^#\s+(.+)$/m)
  return headingMatch?.[1]?.trim() ?? 'Untitled plan'
}

export function parsePlans(content: string): ParsedPlan[] {
  return parseMarkedBlocks(content, PLAN_PARSE_CONFIG).map(({ id, body }) => ({
    planId: id,
    title: extractTitle(body),
    contentMarkdown: body
  }))
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
