import type { OrchestrationPlanResponse } from '@/lib/orchestration/orchestration-state'
import type { ParsedPlan } from '@/lib/orchestration/parse-plans'

export function mergeOrchestrationPlans(
  backendPlans: OrchestrationPlanResponse[],
  messagePlans: ParsedPlan[]
): ParsedPlan[] {
  const merged = new Map<string, ParsedPlan>()

  for (const plan of messagePlans) {
    merged.set(plan.planId, plan)
  }

  for (const plan of backendPlans) {
    merged.set(plan.planId, {
      planId: plan.planId,
      title: plan.title,
      contentMarkdown: plan.contentMarkdown
    })
  }

  return [...merged.values()]
}
