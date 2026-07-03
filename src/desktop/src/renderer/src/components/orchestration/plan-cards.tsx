import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import type { ParsedPlan } from '@/lib/orchestration/parse-plans'

type PlanCardsProps = {
  plans: ParsedPlan[]
  isKickingOff: boolean
  kickingOffPlanId: string | null
  onKickOff: (plan: ParsedPlan) => void
}

export function PlanCards({
  plans,
  isKickingOff,
  kickingOffPlanId,
  onKickOff
}: PlanCardsProps): React.JSX.Element | null {
  if (plans.length === 0) {
    return null
  }

  return (
    <div className="mx-auto w-full max-w-3xl space-y-2 px-4 pb-4">
      <p className="text-sm font-semibold">Plans</p>
      {plans.map((plan) => (
        <Card key={plan.planId}>
          <CardContent className="flex items-center justify-between gap-3 p-4">
            <div className="min-w-0">
              <p className="truncate text-sm font-semibold">{plan.title}</p>
              <p className="truncate text-xs text-muted-foreground">{plan.planId}</p>
            </div>
            <Button
              size="sm"
              disabled={isKickingOff}
              onClick={() => onKickOff(plan)}
              className="shrink-0"
            >
              {kickingOffPlanId === plan.planId ? 'Kicking off…' : 'Kick off'}
            </Button>
          </CardContent>
        </Card>
      ))}
    </div>
  )
}
