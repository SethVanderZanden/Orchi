import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from '@/components/ui/dialog'

type ArchiveChatDialogProps = {
  open: boolean
  chatTitle: string
  onOpenChange: (open: boolean) => void
  onConfirm: () => void
  isArchiving?: boolean
}

export function ArchiveChatDialog({
  open,
  chatTitle,
  onOpenChange,
  onConfirm,
  isArchiving = false
}: ArchiveChatDialogProps): React.JSX.Element {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Archive chat?</DialogTitle>
          <DialogDescription>
            &ldquo;{chatTitle}&rdquo; will be removed from your chat list. If this is the last chat
            for its workspace, the workspace will also be removed.
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={isArchiving}>
            Cancel
          </Button>
          <Button onClick={onConfirm} disabled={isArchiving}>
            {isArchiving ? 'Archiving…' : 'Archive Chat'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
