import { Button } from '@astryxdesign/core/Button'
import { Card } from '@astryxdesign/core/Card'
import { HStack, VStack } from '@astryxdesign/core/Layout'
import { Text } from '@astryxdesign/core/Text'

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
    <VStack gap={2} className="px-4 pb-4">
      <Text type="label" weight="semibold">
        Plans
      </Text>
      {plans.map((plan) => (
        <Card key={plan.planId}>
          <VStack gap={2}>
            <HStack gap={2} vAlign="center" hAlign="between">
              <VStack gap={0}>
                <Text type="label" weight="semibold">
                  {plan.title}
                </Text>
                <Text type="supporting" color="secondary">
                  {plan.planId}
                </Text>
              </VStack>
              <Button
                label={kickingOffPlanId === plan.planId ? 'Kicking off…' : 'Kick off'}
                size="sm"
                isDisabled={isKickingOff}
                onClick={() => onKickOff(plan)}
              />
            </HStack>
          </VStack>
        </Card>
      ))}
    </VStack>
  )
}
