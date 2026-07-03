import { useCallback, useEffect, useRef, useState } from 'react'
import { X } from 'lucide-react'

import { MarkdownContent } from '@/components/markdown-content'
import { Button } from '@/components/ui/button'
import { ScrollArea } from '@/components/ui/scroll-area'
import { cn } from '@/lib/utils'
import type { ParsedPlan } from '@/lib/orchestration/parse-plans'
import type { ParsedReviewPlan } from '@/lib/orchestration/parse-review-plans'

const DEFAULT_WIDTH = 420
const MIN_WIDTH = 300
const MAX_WIDTH = 720

type PlanReviewPanelProps = {
  plans: ParsedPlan[]
  openTabIds: string[]
  activeTabId: string
  reviewPlansByPlanId?: Record<string, ParsedReviewPlan | undefined>
  isKickingOff: boolean
  kickingOffPlanId: string | null
  onSelectTab: (planId: string) => void
  onCloseTab: (planId: string) => void
  onClose: () => void
  onKickOff: (plan: ParsedPlan) => void
}

export function PlanReviewPanel({
  plans,
  openTabIds,
  activeTabId,
  reviewPlansByPlanId = {},
  isKickingOff,
  kickingOffPlanId,
  onSelectTab,
  onCloseTab,
  onClose,
  onKickOff
}: PlanReviewPanelProps): React.JSX.Element {
  const [width, setWidth] = useState(DEFAULT_WIDTH)
  const isDragging = useRef(false)
  const dragStartX = useRef(0)
  const dragStartWidth = useRef(DEFAULT_WIDTH)

  const openPlans = openTabIds
    .map((planId) => plans.find((plan) => plan.planId === planId))
    .filter((plan): plan is ParsedPlan => plan !== undefined)

  const activePlan = openPlans.find((plan) => plan.planId === activeTabId) ?? openPlans[0]
  const activeReviewPlan = activePlan ? reviewPlansByPlanId[activePlan.planId] : undefined
  const showingReview = Boolean(activeReviewPlan)

  const handleMouseMove = useCallback((event: MouseEvent) => {
    if (!isDragging.current) {
      return
    }

    const delta = dragStartX.current - event.clientX
    setWidth(Math.min(MAX_WIDTH, Math.max(MIN_WIDTH, dragStartWidth.current + delta)))
  }, [])

  const handleMouseUp = useCallback(() => {
    isDragging.current = false
  }, [])

  useEffect(() => {
    window.addEventListener('mousemove', handleMouseMove)
    window.addEventListener('mouseup', handleMouseUp)
    return () => {
      window.removeEventListener('mousemove', handleMouseMove)
      window.removeEventListener('mouseup', handleMouseUp)
    }
  }, [handleMouseMove, handleMouseUp])

  return (
    <div className="flex h-full shrink-0" style={{ width }}>
      <div
        role="separator"
        aria-orientation="vertical"
        aria-label="Resize plan panel"
        className="group flex w-1.5 shrink-0 cursor-col-resize items-stretch hover:bg-border/80"
        onMouseDown={(event) => {
          isDragging.current = true
          dragStartX.current = event.clientX
          dragStartWidth.current = width
        }}
      >
        <div className="mx-auto w-px bg-border group-hover:bg-muted-foreground/40" />
      </div>

      <div className="flex min-w-0 flex-1 flex-col border-l bg-background">
        <div className="flex shrink-0 items-center justify-between gap-2 border-b px-3 py-2">
          <p className="text-xs font-medium text-muted-foreground">
            {showingReview ? 'Code review' : 'Plan review'}
          </p>
          <Button
            variant="ghost"
            size="icon"
            className="size-7 shrink-0"
            aria-label="Close plan review"
            onClick={onClose}
          >
            <X className="size-4" />
          </Button>
        </div>

        <div className="flex shrink-0 gap-1 overflow-x-auto border-b px-2 py-1.5">
          {openPlans.map((plan) => (
            <div
              key={plan.planId}
              className={cn(
                'flex max-w-[180px] shrink-0 items-center gap-1 rounded-md border px-2 py-1 text-xs',
                plan.planId === activeTabId
                  ? 'border-primary/40 bg-primary/10 text-foreground'
                  : 'border-transparent bg-muted/50 text-muted-foreground hover:bg-muted'
              )}
            >
              <button
                type="button"
                className="min-w-0 truncate"
                onClick={() => onSelectTab(plan.planId)}
                title={plan.title}
              >
                {plan.title}
              </button>
              <button
                type="button"
                className="shrink-0 rounded p-0.5 hover:bg-background/80"
                aria-label={`Close ${plan.title}`}
                onClick={() => onCloseTab(plan.planId)}
              >
                <X className="size-3" />
              </button>
            </div>
          ))}
        </div>

        {activePlan ? (
          <>
            <ScrollArea className="min-h-0 flex-1">
              <div className="space-y-2 px-4 py-4">
                <p className="truncate text-xs text-muted-foreground">{activePlan.planId}</p>
                <MarkdownContent>
                  {showingReview
                    ? activeReviewPlan!.contentMarkdown
                    : activePlan.contentMarkdown}
                </MarkdownContent>
              </div>
            </ScrollArea>

            {showingReview ? null : (
              <div className="shrink-0 border-t px-4 py-3">
                <Button
                  className="w-full"
                  disabled={isKickingOff}
                  onClick={() => onKickOff(activePlan)}
                >
                  {isKickingOff && kickingOffPlanId === activePlan.planId
                    ? 'Kicking off…'
                    : 'Kick off'}
                </Button>
              </div>
            )}
          </>
        ) : null}
      </div>
    </div>
  )
}
