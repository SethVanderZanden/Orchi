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

type NewChatDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  onCreateChat: (workspacePath: string) => Promise<void>
  isSubmitting?: boolean
}

export function NewChatDialog({
  open,
  onOpenChange,
  onCreateChat,
  isSubmitting = false
}: NewChatDialogProps): React.JSX.Element {
  const [workspacePath, setWorkspacePath] = useState('')

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()

    const trimmed = workspacePath.trim()
    if (!trimmed) {
      return
    }

    await onCreateChat(trimmed)
    setWorkspacePath('')
    onOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <form onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle>New chat</DialogTitle>
            <DialogDescription>
              Choose a workspace folder for the Cursor agent. The agent runs against this directory.
            </DialogDescription>
          </DialogHeader>

          <div className="grid gap-4 py-4">
            <div className="grid gap-2">
              <Label htmlFor="agent">Agent</Label>
              <Input id="agent" value="Cursor" readOnly disabled />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="workspacePath">Workspace path</Label>
              <Input
                id="workspacePath"
                value={workspacePath}
                onChange={(event) => setWorkspacePath(event.target.value)}
                placeholder="e.g. E:\Projects\Orchi"
                autoFocus
              />
            </div>
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={isSubmitting || workspacePath.trim().length === 0}>
              Create chat
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
