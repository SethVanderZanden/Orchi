import { parseMarkedBlocks } from '@/lib/orchestration/parse-marked-blocks'

export type ParsedReviewPlan = {
  planId: string
  title: string
  contentMarkdown: string
}

const REVIEW_PLAN_PARSE_CONFIG = {
  completeBlockPattern:
    /<!--\s*orchi-review-plan:([a-z0-9]+(?:-[a-z0-9]+)*)\s*-->\s*([\s\S]*?)<!--\s*\/orchi-review-plan\s*-->/gi,
  openMarkerPattern: /<!--\s*orchi-review-plan:([a-z0-9]+(?:-[a-z0-9]+)*)\s*-->/gi
} as const

function extractTitle(content: string): string {
  const headingMatch = content.match(/^#\s+(.+)$/m)
  return headingMatch?.[1]?.trim() ?? 'Untitled review plan'
}

export function parseReviewPlans(content: string): ParsedReviewPlan[] {
  return parseMarkedBlocks(content, REVIEW_PLAN_PARSE_CONFIG).map(({ id, body }) => ({
    planId: id,
    title: extractTitle(body),
    contentMarkdown: body
  }))
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
