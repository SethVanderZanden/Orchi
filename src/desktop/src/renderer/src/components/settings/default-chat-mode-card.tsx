import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { useDefaultChatMode } from '@/hooks/use-default-chat-mode'
import {
  getDefaultChatModeLabel,
  getDefaultChatModeOptions
} from '@/lib/preferences/default-chat-mode'
import { cn } from '@/lib/utils'

export function DefaultChatModeCard(): React.JSX.Element {
  const { defaultChatMode, setDefaultChatMode } = useDefaultChatMode()
  const options = getDefaultChatModeOptions()

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">Default chat mode</CardTitle>
        <CardDescription>
          Choose the mode applied when you open a new chat. You can still change mode per chat.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <fieldset className="space-y-3">
          <legend className="sr-only">Default chat mode</legend>
          <div className="grid gap-2">
            {options.map((option) => {
              const selected = defaultChatMode.toLowerCase() === option.id.toLowerCase()
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
                    name="default-chat-mode"
                    value={option.id}
                    checked={selected}
                    onChange={() => setDefaultChatMode(option.id)}
                    className="mt-0.5 size-4 accent-primary"
                  />
                  <span className="min-w-0 space-y-0.5">
                    <span className="block font-medium">{option.label}</span>
                    {option.description ? (
                      <span className="block text-xs text-muted-foreground">
                        {option.description}
                      </span>
                    ) : null}
                  </span>
                </label>
              )
            })}
          </div>
          <p className="text-xs text-muted-foreground">
            New chats start in {getDefaultChatModeLabel(defaultChatMode)} mode.
          </p>
        </fieldset>
      </CardContent>
    </Card>
  )
}
