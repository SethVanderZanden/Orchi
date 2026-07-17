import { useState } from 'react'

import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { useUserPreferences } from '@/hooks/use-user-preferences'
import {
  getAvailableAgentLabel,
  getAvailableAgentOptions
} from '@/lib/user-preferences/enabled-agents'
import { cn } from '@/lib/utils'

export function EnabledAgentsCard(): React.JSX.Element {
  const { enabledAgentIds, isUpdating, setEnabledAgentIds } = useUserPreferences()
  const options = getAvailableAgentOptions()
  const [draftIds, setDraftIds] = useState<string[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  const selectedIds = draftIds ?? enabledAgentIds

  function toggleAgent(agentId: string): void {
    setError(null)
    setDraftIds((current) => {
      const base = current ?? enabledAgentIds
      if (base.includes(agentId)) {
        return base.filter((id) => id !== agentId)
      }

      return [...base, agentId]
    })
  }

  async function handleSave(): Promise<void> {
    if (selectedIds.length === 0) {
      setError('Keep at least one agent enabled.')
      return
    }

    try {
      await setEnabledAgentIds(selectedIds)
      setDraftIds(null)
    } catch (saveError) {
      setError(saveError instanceof Error ? saveError.message : 'Unable to save agents.')
    }
  }

  const dirty =
    draftIds !== null &&
    (draftIds.length !== enabledAgentIds.length ||
      draftIds.some((id) => !enabledAgentIds.includes(id)))

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">Enabled agents</CardTitle>
        <CardDescription>
          Choose which agents Orchi can use. Mode defaults and chat creation only offer enabled
          agents.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-3">
        <fieldset className="space-y-2" disabled={isUpdating}>
          <legend className="sr-only">Enabled agents</legend>
          {options.map((option) => {
            const selected = selectedIds.includes(option.id)
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
                  type="checkbox"
                  checked={selected}
                  onChange={() => toggleAgent(option.id)}
                  className="mt-0.5 size-4 shrink-0 rounded border border-input"
                />
                <span className="min-w-0 space-y-0.5">
                  <span className="block font-medium">{option.label}</span>
                  <span className="block text-xs text-muted-foreground">{option.description}</span>
                </span>
              </label>
            )
          })}
        </fieldset>

        {error ? <p className="text-sm text-destructive">{error}</p> : null}

        <div className="flex flex-wrap items-center gap-2">
          <Button
            type="button"
            size="sm"
            disabled={isUpdating || !dirty || selectedIds.length === 0}
            onClick={() => void handleSave()}
          >
            Save agents
          </Button>
          {dirty ? (
            <Button
              type="button"
              size="sm"
              variant="ghost"
              disabled={isUpdating}
              onClick={() => {
                setDraftIds(null)
                setError(null)
              }}
            >
              Cancel
            </Button>
          ) : null}
        </div>

        <p className="text-xs text-muted-foreground">
          Currently enabled:{' '}
          {enabledAgentIds.length === 0
            ? 'none'
            : enabledAgentIds.map(getAvailableAgentLabel).join(', ')}
        </p>
      </CardContent>
    </Card>
  )
}
