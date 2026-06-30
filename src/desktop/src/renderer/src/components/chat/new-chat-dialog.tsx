import { useEffect, useState } from 'react'

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

export type NewChatOptions = {
  workspacePath: string
  mode: ChatMode
  attachedPlanId?: string
}

type NewChatDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  workspacePath: string
  workspaceName: string
  onCreateChat: (options: NewChatOptions) => Promise<void>
  isSubmitting?: boolean
}

export function NewChatDialog({
  open,
  onOpenChange,
  workspacePath,
  workspaceName,
  onCreateChat,
  isSubmitting = false
}: NewChatDialogProps): React.JSX.Element {
  const [mode, setMode] = useState<ChatMode>('agent')
  const [attachedPlanId, setAttachedPlanId] = useState('')

  useEffect(() => {
    if (!open) {
      setMode('agent')
      setAttachedPlanId('')
    }
  }, [open])

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()

    await onCreateChat({
      workspacePath,
      mode,
      attachedPlanId:
        mode === 'implement' && attachedPlanId.trim() ? attachedPlanId.trim() : undefined
    })

    setMode('agent')
    setAttachedPlanId('')
    onOpenChange(false)
  }

  const selectedMode = CHAT_MODES.find((entry) => entry.value === mode)

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <form onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle>New chat in {workspaceName}</DialogTitle>
            <DialogDescription>
              Choose a mode for the Cursor agent in this project.
            </DialogDescription>
          </DialogHeader>

          <div className="grid gap-4 py-4">
            <div className="grid gap-1 rounded-lg border px-3 py-2">
              <p className="text-sm font-medium">{workspaceName}</p>
              <p className="text-muted-foreground truncate text-xs">{workspacePath}</p>
            </div>

            <div className="grid gap-2">
              <Label htmlFor="agent">Agent</Label>
              <Input id="agent" value="Cursor" readOnly disabled />
            </div>

            <div className="grid gap-2">
              <Label htmlFor="mode">Mode</Label>
              <Select value={mode} onValueChange={(value) => setMode(value as ChatMode)}>
                <SelectTrigger id="mode">
                  <SelectValue placeholder="Select mode" />
                </SelectTrigger>
                <SelectContent>
                  {CHAT_MODES.map((entry) => (
                    <SelectItem key={entry.value} value={entry.value}>
                      {entry.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {selectedMode ? (
                <p className="text-muted-foreground text-xs">{selectedMode.description}</p>
              ) : null}
            </div>

            {mode === 'implement' ? (
              <div className="grid gap-2">
                <Label htmlFor="attachedPlanId">Plan ID</Label>
                <Input
                  id="attachedPlanId"
                  value={attachedPlanId}
                  onChange={(event) => setAttachedPlanId(event.target.value)}
                  placeholder="Required for implement mode"
                  autoFocus
                />
              </div>
            ) : null}
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <Button
              type="submit"
              disabled={
                isSubmitting || (mode === 'implement' && attachedPlanId.trim().length === 0)
              }
            >
              Create chat
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
