import { useState } from 'react'

import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from '@/components/ui/dialog'
import { useUserPreferences } from '@/hooks/use-user-preferences'
import { getAvailableAgentOptions } from '@/lib/user-preferences/enabled-agents'
import { cn } from '@/lib/utils'

type EnabledAgentsPickerProps = {
  /** When true, the dialog cannot be dismissed until at least one agent is saved. */
  mandatory?: boolean
  open: boolean
  onOpenChange?: (open: boolean) => void
  initialSelectedIds?: string[]
  title?: string
  description?: string
}

export function EnabledAgentsPickerDialog({
  mandatory = false,
  open,
  onOpenChange,
  initialSelectedIds = [],
  title = 'Choose your agents',
  description = 'Orchi needs to know which agents you have installed. Select Cursor, Codex, or both — we will use these to set default models for each chat mode.'
}: EnabledAgentsPickerProps): React.JSX.Element {
  const { setEnabledAgentIds, isUpdating } = useUserPreferences()
  const options = getAvailableAgentOptions()
  const initialKey = initialSelectedIds.slice().sort().join(',')
  const syncKey = open ? `open:${initialKey}` : 'closed'
  const [selectedIds, setSelectedIds] = useState<string[]>(initialSelectedIds)
  const [error, setError] = useState<string | null>(null)
  const [prevSyncKey, setPrevSyncKey] = useState(syncKey)

  if (syncKey !== prevSyncKey) {
    setPrevSyncKey(syncKey)
    if (open) {
      setSelectedIds(initialSelectedIds)
      setError(null)
    }
  }

  function toggleAgent(agentId: string): void {
    setError(null)
    setSelectedIds((current) => {
      if (current.includes(agentId)) {
        return current.filter((id) => id !== agentId)
      }

      return [...current, agentId]
    })
  }

  async function handleContinue(): Promise<void> {
    if (selectedIds.length === 0) {
      setError('Select at least one agent from the list.')
      return
    }

    try {
      await setEnabledAgentIds(selectedIds)
      onOpenChange?.(false)
    } catch (saveError) {
      setError(saveError instanceof Error ? saveError.message : 'Unable to save agent preferences.')
    }
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        if (mandatory && !next) {
          return
        }

        onOpenChange?.(next)
      }}
    >
      <DialogContent
        showCloseButton={!mandatory}
        className="sm:max-w-md"
        onPointerDownOutside={(event) => {
          if (mandatory) {
            event.preventDefault()
          }
        }}
        onEscapeKeyDown={(event) => {
          if (mandatory) {
            event.preventDefault()
          }
        }}
      >
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          <DialogDescription>{description}</DialogDescription>
        </DialogHeader>

        <fieldset className="space-y-2" disabled={isUpdating}>
          <legend className="sr-only">Available agents</legend>
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

        <DialogFooter>
          <Button
            type="button"
            disabled={isUpdating || selectedIds.length === 0}
            onClick={() => void handleContinue()}
          >
            {isUpdating ? 'Saving…' : 'Continue'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
