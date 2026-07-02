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

export type NewChatOptions = {
  workspacePath: string
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
  async function handleSubmit(event: React.FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()

    await onCreateChat({ workspacePath })
    onOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <form onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle>New chat in {workspaceName}</DialogTitle>
            <DialogDescription>
              Start a conversation with the Cursor agent in this project.
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
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              Create chat
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
