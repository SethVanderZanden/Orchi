import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { useUserPreferences } from '@/hooks/use-user-preferences'
import { cn } from '@/lib/utils'

export function AutoKickOffReviewCard(): React.JSX.Element {
  const { autoKickOffReview, setAutoKickOffReview, isUpdating } = useUserPreferences()

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">Review after implementation</CardTitle>
        <CardDescription>
          When an implementation agent finishes, Orchi can automatically start a review agent. Turn
          this off to skip review when you do not want it.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <label
          className={cn(
            'flex cursor-pointer items-start gap-3 rounded-lg border px-3 py-2.5 text-sm transition-colors',
            autoKickOffReview ? 'border-primary bg-primary/5' : 'border-border hover:bg-accent/40',
            isUpdating && 'pointer-events-none opacity-70'
          )}
        >
          <input
            type="checkbox"
            checked={autoKickOffReview}
            disabled={isUpdating}
            onChange={(event) => setAutoKickOffReview(event.target.checked)}
            className="mt-0.5 size-4 accent-primary"
          />
          <span className="min-w-0 space-y-0.5">
            <span className="block font-medium">Automatically kick off review</span>
            <span className="block text-xs text-muted-foreground">
              {autoKickOffReview
                ? 'Review agents start when implementation completes.'
                : 'Review agents are not started automatically after implementation.'}
            </span>
          </span>
        </label>
      </CardContent>
    </Card>
  )
}
