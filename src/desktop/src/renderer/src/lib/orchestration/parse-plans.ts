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
  planFilePath?: string | null
}

export function buildConventionalPlanFilePath(planId: string): string {
  return `.orchi/plan-${planId}.md`
}

function isPlanFilePath(value: string): boolean {
  return value.startsWith('.orchi/') && value.endsWith('.md')
}

function normalizePlanFilePath(value: string): string {
  return value.replace(/\\/g, '/').replace(/^\//, '')
}

export function tryResolvePlanFileReference(
  body: string,
  planId: string
): { isReference: true; planFilePath: string } | { isReference: false } {
  const trimmed = body.trim()

  if (trimmed.length === 0) {
    return { isReference: true, planFilePath: buildConventionalPlanFilePath(planId) }
  }

  if (trimmed.startsWith('# ')) {
    return { isReference: false }
  }

  const firstLine = trimmed.split('\n', 2)[0]?.trim().replace(/^`|`$/g, '') ?? ''
  if (isPlanFilePath(firstLine)) {
    return { isReference: true, planFilePath: normalizePlanFilePath(firstLine) }
  }

  return { isReference: false }
}

export function parsePlans(content: string): ParsedPlan[] {
  return parseMarkedBlocks(content, getIdBlockParseConfig(ORCHI_MARKERS.plan)).map(
    ({ id, body }) => {
      const fileReference = tryResolvePlanFileReference(body, id)
      if (fileReference.isReference) {
        return {
          planId: id,
          title: ORCHI_MARKERS.plan.defaultTitle,
          contentMarkdown: '',
          planFilePath: fileReference.planFilePath
        }
      }

      return {
        planId: id,
        title: extractMarkdownTitle(body, ORCHI_MARKERS.plan.defaultTitle),
        contentMarkdown: body
      }
    }
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
