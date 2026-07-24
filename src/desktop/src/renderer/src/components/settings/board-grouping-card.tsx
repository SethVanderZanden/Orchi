import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { useBoardGrouping } from '@/hooks/use-board-grouping'
import { getBoardGroupingLabel, type BoardGroupingMode } from '@/lib/preferences/board-grouping'
import { cn } from '@/lib/utils'

const GROUPING_OPTIONS: ReadonlyArray<{
  id: BoardGroupingMode
  label: string
  description: string
}> = [
  {
    id: 'state',
    label: 'Group by state',
    description: 'List chats under Processing, Ready to Review, then Done.'
  },
  {
    id: 'project',
    label: 'Group by project',
    description: 'Group by project first, then by state under each project.'
  }
]

export function BoardGroupingCard(): React.JSX.Element {
  const { grouping, setGrouping } = useBoardGrouping()

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">Agents sidebar grouping</CardTitle>
        <CardDescription>Choose how chats are organized in the agents sidebar.</CardDescription>
      </CardHeader>
      <CardContent>
        <fieldset className="space-y-3">
          <legend className="sr-only">Agents sidebar grouping</legend>
          <div className="grid gap-2">
            {GROUPING_OPTIONS.map((option) => {
              const selected = grouping === option.id
              return (
                <label
                  key={option.id}
                  className={cn(
                    'flex cursor-pointer items-start gap-3 rounded-lg border px-3 py-2.5 text-sm transition-colors',
                    selected ? 'border-primary bg-primary/5' : 'border-border hover:bg-accent/40'
                  )}
                >
                  <input
                    type="radio"
                    name="board-grouping"
                    value={option.id}
                    checked={selected}
                    onChange={() => setGrouping(option.id)}
                    className="mt-0.5 size-4 accent-primary"
                  />
                  <span className="min-w-0 space-y-0.5">
                    <span className="block font-medium">{option.label}</span>
                    <span className="block text-xs text-muted-foreground">
                      {option.description}
                    </span>
                  </span>
                </label>
              )
            })}
          </div>
          <p className="text-xs text-muted-foreground">
            Current: {getBoardGroupingLabel(grouping)}. Project and date filters still apply.
          </p>
        </fieldset>
      </CardContent>
    </Card>
  )
}
