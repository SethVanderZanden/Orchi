export type ParsedReviewPlan = {
  planId: string
  title: string
  contentMarkdown: string
}

const REVIEW_PLAN_BLOCK_PATTERN =
  /<!--\s*orchi-review-plan:([a-z0-9]+(?:-[a-z0-9]+)*)\s*-->\s*([\s\S]*?)<!--\s*\/orchi-review-plan\s*-->/gi

function extractTitle(content: string): string {
  const headingMatch = content.match(/^#\s+(.+)$/m)
  return headingMatch?.[1]?.trim() ?? 'Untitled review plan'
}

export function parseReviewPlans(content: string): ParsedReviewPlan[] {
  const plans = new Map<string, ParsedReviewPlan>()

  for (const match of content.matchAll(REVIEW_PLAN_BLOCK_PATTERN)) {
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
