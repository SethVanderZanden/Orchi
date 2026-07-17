import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { useUserPreferences } from '@/hooks/use-user-preferences'
import {
  getPostMessageBehaviorLabel,
  getPostMessageBehaviorOptions
} from '@/lib/user-preferences/post-message-behavior'
import { cn } from '@/lib/utils'

export function PostMessageBehaviorCard(): React.JSX.Element {
  const { postMessageBehavior, setPostMessageBehavior, isUpdating } = useUserPreferences()
  const options = getPostMessageBehaviorOptions()

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">After sending a message</CardTitle>
        <CardDescription>
          Choose where Orchi takes you once the agent finishes responding.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <fieldset className="space-y-3" disabled={isUpdating}>
          <legend className="sr-only">After sending a message</legend>
          <div className="grid gap-2">
            {options.map((option) => {
              const selected = postMessageBehavior === option.id
              return (
                <label
                  key={option.id}
                  className={cn(
                    'flex cursor-pointer items-start gap-3 rounded-lg border px-3 py-2.5 text-sm transition-colors',
                    selected ? 'border-primary bg-primary/5' : 'border-border hover:bg-accent/40',
                    isUpdating && 'pointer-events-none opacity-70'
                  )}
                >
                  <input
                    type="radio"
                    name="post-message-behavior"
                    value={option.id}
                    checked={selected}
                    onChange={() => setPostMessageBehavior(option.id)}
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
            After a response completes, Orchi will{' '}
            {getPostMessageBehaviorLabel(postMessageBehavior).toLowerCase()}.
          </p>
        </fieldset>
      </CardContent>
    </Card>
  )
}
