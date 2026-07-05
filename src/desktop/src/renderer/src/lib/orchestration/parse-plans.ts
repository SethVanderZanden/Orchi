import { parsePlanSequenceFromMessages } from './plan-sequence'

export type ParsedPlan = {
  planId: string
  title: string
  contentMarkdown: string
}

const PLAN_BLOCK_PATTERN =
  /<!--\s*orchi-plan:([a-z0-9]+(?:-[a-z0-9]+)*)\s*-->\s*([\s\S]*?)<!--\s*\/orchi-plan\s*-->/gi

function extractTitle(content: string): string {
  const headingMatch = content.match(/^#\s+(.+)$/m)
  return headingMatch?.[1]?.trim() ?? 'Untitled plan'
}

export function parsePlans(content: string): ParsedPlan[] {
  const plans = new Map<string, ParsedPlan>()

  for (const match of content.matchAll(PLAN_BLOCK_PATTERN)) {
    const planId = match[1]
    const body = match[2].trim()
    if (!planId || !body) {
      continue
    }

    plans.set(planId, {
      planId,
      title: extractTitle(body),
      contentMarkdown: body
    })
  }

  return [...plans.values()]
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
