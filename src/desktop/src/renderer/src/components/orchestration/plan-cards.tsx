import { ExternalLink, FileText } from 'lucide-react'
import { useNavigate } from '@tanstack/react-router'

import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import type { ChatThread } from '@/lib/chat/types'
import type { ParsedPlan } from '@/lib/orchestration/parse-plans'
import type { ParsedReviewPlan } from '@/lib/orchestration/parse-review-plans'
import { findChildForPlan, findReviewChildForPlan } from '@/lib/projects/chat-tree'
import { getPlanReviewVisibility } from '@/lib/orchestration/plan-review-visibility'
import { getSequenceStepNumber, hasSequentialKickoff } from '@/lib/orchestration/plan-sequence'
import { cn } from '@/lib/utils'

type SequentialKickoffProgress = {
  active: boolean
  currentStep: number
  totalSteps: number
}

type PlanCardsProps = {
  plans: ParsedPlan[]
  openTabIds: string[]
  childChats?: ChatThread[]
  reviewPlansByPlanId?: Record<string, ParsedReviewPlan | undefined>
  isParentKickingOffAny: (parentChatId: string) => boolean
  parentChatId: string
  sequencePlanIds?: string[]
  sequentialKickoffProgress?: SequentialKickoffProgress | null
  orchestrationError?: string | null
  onToggleReview: (plan: ParsedPlan) => void
  onKickOffAll: () => void
}

export function PlanCards({
  plans,
  openTabIds,
  childChats = [],
  reviewPlansByPlanId = {},
  isParentKickingOffAny,
  parentChatId,
  sequencePlanIds = [],
  sequentialKickoffProgress = null,
  orchestrationError = null,
  onToggleReview,
  onKickOffAll
}: PlanCardsProps): React.JSX.Element | null {
  const navigate = useNavigate()
  const kickingOffAny = isParentKickingOffAny(parentChatId)
  const kickOffAllCount = plans.filter((plan) => !findChildForPlan(plan.planId, childChats)).length
  const isSequentialKickoff = hasSequentialKickoff(sequencePlanIds, plans)
  const sequentialRunActive = sequentialKickoffProgress?.active ?? false

  if (plans.length === 0) {
    return null
  }

  return (
    <div className="mx-auto w-full max-w-3xl space-y-2 px-4 pb-4">
      <p className="text-sm font-semibold">Plans</p>
      {orchestrationError ? (
        <p className="rounded-md border border-destructive/40 bg-destructive/10 px-3 py-2 text-sm text-destructive">
          {orchestrationError}
        </p>
      ) : null}
      {plans.map((plan) => {
        const isTabOpen = openTabIds.includes(plan.planId)
        const childChat = findChildForPlan(plan.planId, childChats)
        const reviewChild = findReviewChildForPlan(plan.planId, childChats)
        const reviewPlan = reviewPlansByPlanId[plan.planId]
        const reviewReady = Boolean(reviewPlan)
        const implementing = isChildRunning(childChat)
        const { reviewing, reviewStarted } = getPlanReviewVisibility(reviewChild, reviewReady)
        const sequenceStep = getSequenceStepNumber(plan.planId, sequencePlanIds)

        return (
          <Card
            key={plan.planId}
            className={cn(
              isTabOpen && 'border-primary/50 bg-primary/5',
              reviewReady && 'border-primary bg-primary/10',
              reviewStarted && 'border-primary/40 bg-primary/5'
            )}
          >
            <CardContent className="flex items-center justify-between gap-3 p-4">
              <div className="min-w-0 space-y-1">
                <div className="flex items-center gap-2">
                  {sequenceStep ? (
                    <Badge variant="outline" className="shrink-0 px-1.5 text-[10px] tabular-nums">
                      {sequenceStep}
                    </Badge>
                  ) : null}
                  <p className="truncate text-sm font-semibold">{plan.title}</p>
                  {implementing ? (
                    <Badge variant="secondary" className="shrink-0 text-[10px]">
                      Implementing…
                    </Badge>
                  ) : null}
                  {reviewing ? (
                    <Badge variant="secondary" className="shrink-0 text-[10px]">
                      Reviewing…
                    </Badge>
                  ) : null}
                  {reviewStarted ? (
                    <Badge variant="outline" className="shrink-0 text-[10px]">
                      Review started
                    </Badge>
                  ) : null}
                  {reviewReady ? (
                    <Badge variant="default" className="shrink-0 text-[10px]">
                      Review ready
                    </Badge>
                  ) : null}
                </div>
                <p className="truncate text-xs text-muted-foreground">{plan.planId}</p>
              </div>
              <div className="flex shrink-0 items-center gap-2">
                {childChat ? (
                  <Button
                    size="sm"
                    variant="secondary"
                    onClick={() =>
                      navigate({
                        to: '/chat/$chatId',
                        params: { chatId: childChat.id }
                      })
                    }
                  >
                    <ExternalLink className="size-3.5" />
                    Open agent
                  </Button>
                ) : null}
                {reviewChild ? (
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() =>
                      navigate({
                        to: '/chat/$chatId',
                        params: { chatId: reviewChild.id }
                      })
                    }
                  >
                    <ExternalLink className="size-3.5" />
                    Open review
                  </Button>
                ) : null}
                <Button
                  size="sm"
                  variant={isTabOpen ? 'default' : 'outline'}
                  onClick={() => onToggleReview(plan)}
                >
                  <FileText className="size-3.5" />
                  {reviewReady ? 'View review' : 'Review'}
                </Button>
              </div>
            </CardContent>
          </Card>
        )
      })}

      <Button
        className="w-full"
        disabled={kickingOffAny || kickOffAllCount === 0 || sequentialRunActive}
        onClick={onKickOffAll}
      >
        {sequentialRunActive && sequentialKickoffProgress
          ? `Running plan ${sequentialKickoffProgress.currentStep} of ${sequentialKickoffProgress.totalSteps}…`
          : kickingOffAny
            ? 'Kicking off plans…'
            : isSequentialKickoff
              ? `Kick off in order (${kickOffAllCount})`
              : `Kick off all (${kickOffAllCount})`}
      </Button>
    </div>
  )
}
