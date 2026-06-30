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
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue
} from '@/components/ui/select'
import type { ChatMode } from '@/lib/chat/types'
import { CHAT_MODES } from '@/lib/chat/types'

type ChatModeSelectProps = {
  value: ChatMode
  disabled?: boolean
  onChange: (mode: ChatMode, attachedPlanId?: string) => void | Promise<void>
  className?: string
}

export function ChatModeSelect({
  value,
  disabled = false,
  onChange,
  className
}: ChatModeSelectProps): React.JSX.Element {
  const [planDialogOpen, setPlanDialogOpen] = useState(false)
  const [pendingMode, setPendingMode] = useState<ChatMode | null>(null)
  const [attachedPlanId, setAttachedPlanId] = useState('')

  async function handleModeChange(nextMode: ChatMode): Promise<void> {
    if (nextMode === value) {
      return
    }

    if (nextMode === 'implement') {
      setPendingMode(nextMode)
      setAttachedPlanId('')
      setPlanDialogOpen(true)
      return
    }

    await onChange(nextMode)
  }

  async function handlePlanSubmit(event: React.FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()

    const trimmedPlanId = attachedPlanId.trim()
    if (!trimmedPlanId || !pendingMode) {
      return
    }

    await onChange(pendingMode, trimmedPlanId)
    setPlanDialogOpen(false)
    setPendingMode(null)
    setAttachedPlanId('')
  }

  return (
    <>
      <Select value={value} onValueChange={(next) => void handleModeChange(next as ChatMode)} disabled={disabled}>
        <SelectTrigger className={className} aria-label="Chat mode">
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          {CHAT_MODES.map((entry) => (
            <SelectItem key={entry.value} value={entry.value}>
              {entry.label}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      <Dialog open={planDialogOpen} onOpenChange={setPlanDialogOpen}>
        <DialogContent>
          <form onSubmit={handlePlanSubmit}>
            <DialogHeader>
              <DialogTitle>Attach a plan</DialogTitle>
              <DialogDescription>
                Implement mode requires a plan ID from this or another chat.
              </DialogDescription>
            </DialogHeader>

            <div className="py-4">
              <Label htmlFor="switchPlanId">Plan ID</Label>
              <Input
                id="switchPlanId"
                value={attachedPlanId}
                onChange={(event) => setAttachedPlanId(event.target.value)}
                placeholder="Paste plan ID"
                autoFocus
              />
            </div>

            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setPlanDialogOpen(false)}>
                Cancel
              </Button>
              <Button type="submit" disabled={attachedPlanId.trim().length === 0}>
                Switch to Implement
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </>
  )
}
