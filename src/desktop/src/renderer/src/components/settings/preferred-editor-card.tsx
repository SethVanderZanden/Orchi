import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { usePreferredEditor } from '@/hooks/use-preferred-editor'
import { getEditorLabel, type EditorId } from '@/lib/preferences/preferred-editor'
import { cn } from '@/lib/utils'

const EDITOR_OPTIONS: EditorId[] = ['vscode', 'cursor']

export function PreferredEditorCard(): React.JSX.Element {
  const { preferredEditor, setPreferredEditor } = usePreferredEditor()

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">Default editor</CardTitle>
        <CardDescription>
          Choose which editor the Open in editor button and Ctrl+E shortcut use.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <fieldset className="space-y-3">
          <legend className="sr-only">Default editor</legend>
          <div className="grid gap-2 sm:grid-cols-2">
            {EDITOR_OPTIONS.map((editor) => {
              const selected = preferredEditor === editor
              return (
                <label
                  key={editor}
                  className={cn(
                    'flex cursor-pointer items-center gap-3 rounded-lg border px-3 py-2.5 text-sm transition-colors',
                    selected ? 'border-primary bg-primary/5' : 'border-border hover:bg-accent/40'
                  )}
                >
                  <input
                    type="radio"
                    name="preferred-editor"
                    value={editor}
                    checked={selected}
                    onChange={() => setPreferredEditor(editor)}
                    className="size-4 accent-primary"
                  />
                  <span className="font-medium">{getEditorLabel(editor)}</span>
                </label>
              )
            })}
          </div>
          <p className="text-xs text-muted-foreground">
            Current button label: Open in {getEditorLabel(preferredEditor)}
          </p>
        </fieldset>
      </CardContent>
    </Card>
  )
}
