import type { ParsedPlan } from './parse-plans'

const PLAN_SEQUENCE_PATTERN =
  /<!--\s*orchi-plan-sequence\s*-->\s*([\s\S]*?)<!--\s*\/orchi-plan-sequence\s*-->/gi

const PLAN_ID_PATTERN = /^[a-z0-9]+(?:-[a-z0-9]+)*$/

function parseSequenceBody(body: string): string[] {
  const ids: string[] = []
  const seen = new Set<string>()

  for (const line of body.split('\n')) {
    let trimmed = line.trim()
    if (!trimmed) {
      continue
    }

    if (trimmed.startsWith('- ')) {
      trimmed = trimmed.slice(2).trim()
    }

    const id = trimmed.toLowerCase()
    if (!PLAN_ID_PATTERN.test(id) || seen.has(id)) {
      continue
    }

    seen.add(id)
    ids.push(id)
  }

  return ids
}

export function parsePlanSequence(content: string): string[] | null {
  let latest: string[] | null = null
  let found = false

  for (const match of content.matchAll(PLAN_SEQUENCE_PATTERN)) {
    found = true
    latest = parseSequenceBody(match[1])
  }

  return found ? (latest ?? []) : null
}

export function parsePlanSequenceFromMessages(
  messages: Array<{ role: string; content: string }>
): string[] {
  let latest: string[] = []

  for (const message of messages) {
    if (message.role !== 'assistant') {
      continue
    }

    const parsed = parsePlanSequence(message.content)
    if (parsed !== null) {
      latest = parsed
    }
  }

  return latest
}

export function resolveKickoffGroups(
  plans: ParsedPlan[],
  sequencePlanIds: string[]
): { sequenced: ParsedPlan[]; independent: ParsedPlan[] } {
  const planById = new Map(plans.map((plan) => [plan.planId, plan]))
  const sequenceSet = new Set(sequencePlanIds)

  const sequenced: ParsedPlan[] = []
  for (const planId of sequencePlanIds) {
    const plan = planById.get(planId)
    if (plan) {
      sequenced.push(plan)
    }
  }

  const independent = plans.filter((plan) => !sequenceSet.has(plan.planId))

  return { sequenced, independent }
}

export function hasSequentialKickoff(sequencePlanIds: string[], plans: ParsedPlan[]): boolean {
  if (sequencePlanIds.length === 0) {
    return false
  }

  const planIds = new Set(plans.map((plan) => plan.planId))
  return sequencePlanIds.some((planId) => planIds.has(planId))
}

export function getSequenceStepNumber(planId: string, sequencePlanIds: string[]): number | null {
  const index = sequencePlanIds.indexOf(planId)
  return index >= 0 ? index + 1 : null
}
