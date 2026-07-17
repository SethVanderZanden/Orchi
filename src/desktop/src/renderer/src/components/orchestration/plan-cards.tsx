import { ChevronDown, ExternalLink, FileText } from 'lucide-react'
import { useNavigate } from '@tanstack/react-router'

import { ShortcutHint } from '@/components/app-header/shortcut-hint'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { ButtonGroup } from '@/components/ui/button-group'
import { Card, CardContent } from '@/components/ui/card'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger
} from '@/components/ui/dropdown-menu'
import type { ChatThread } from '@/lib/chat/types'
import type { ParsedPlan } from '@/lib/orchestration/parse-plans'
import type { ParsedReviewPlan } from '@/lib/orchestration/parse-review-plans'
import { findChildForPlan, findReviewChildForPlan } from '@/lib/projects/chat-tree'
import { getPlanReviewVisibility, isChildRunning } from '@/lib/orchestration/plan-review-visibility'
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
        const reviewVariant = isTabOpen ? 'default' : 'outline'
        const hasOpenActions = Boolean(childChat || reviewChild)

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
              <ButtonGroup className="shrink-0" aria-label="Plan actions">
                <Button size="sm" variant={reviewVariant} onClick={() => onToggleReview(plan)}>
                  <FileText className="size-3.5" />
                  {reviewReady ? 'View review' : 'Review'}
                </Button>
                {hasOpenActions ? (
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button
                        size="sm"
                        variant={reviewVariant}
                        className="px-2"
                        aria-label="More plan actions"
                      >
                        <ChevronDown className="size-3.5" />
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end">
                      {childChat ? (
                        <DropdownMenuItem
                          onClick={() =>
                            navigate({
                              to: '/chat/$chatId',
                              params: { chatId: childChat.id }
                            })
                          }
                        >
                          <ExternalLink className="size-3.5" />
                          Open agent
                        </DropdownMenuItem>
                      ) : null}
                      {reviewChild ? (
                        <DropdownMenuItem
                          onClick={() =>
                            navigate({
                              to: '/chat/$chatId',
                              params: { chatId: reviewChild.id }
                            })
                          }
                        >
                          <ExternalLink className="size-3.5" />
                          Open review
                        </DropdownMenuItem>
                      ) : null}
                    </DropdownMenuContent>
                  </DropdownMenu>
                ) : null}
              </ButtonGroup>
            </CardContent>
          </Card>
        )
      })}

      <Button
        className="w-full min-w-0 gap-2 overflow-hidden"
        disabled={kickingOffAny || kickOffAllCount === 0 || sequentialRunActive}
        onClick={onKickOffAll}
      >
        <span className="min-w-0 truncate">
          {sequentialRunActive && sequentialKickoffProgress
            ? `Running plan ${sequentialKickoffProgress.currentStep} of ${sequentialKickoffProgress.totalSteps}…`
            : kickingOffAny
              ? 'Kicking off plans…'
              : isSequentialKickoff
                ? `Kick off in order (${kickOffAllCount})`
                : `Kick Off All (${kickOffAllCount})`}
        </span>
        {!kickingOffAny && !sequentialRunActive && kickOffAllCount > 0 ? (
          <ShortcutHint className="shrink-0">Ctrl+Enter</ShortcutHint>
        ) : null}
      </Button>
    </div>
  )
}
