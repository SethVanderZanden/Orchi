import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from '@/components/ui/dialog'

type DeleteChatDialogProps = {
  open: boolean
  chatCount: number
  chatTitle?: string
  skippedSendingCount?: number
  onOpenChange: (open: boolean) => void
  onConfirm: () => void
  isDeleting?: boolean
}

export function DeleteChatDialog({
  open,
  chatCount,
  chatTitle = '',
  skippedSendingCount = 0,
  onOpenChange,
  onConfirm,
  isDeleting = false
}: DeleteChatDialogProps): React.JSX.Element {
  const isBulk = chatCount > 1
  const title = isBulk ? `Delete ${chatCount} chats?` : 'Delete chat?'

  const description = isBulk ? (
    <>
      {chatCount} chat{chatCount === 1 ? '' : 's'} will be permanently deleted. Projects and
      workspaces will not be affected. This action cannot be undone.
      {skippedSendingCount > 0 ? (
        <>
          {' '}
          {skippedSendingCount} running chat{skippedSendingCount === 1 ? '' : 's'} will be
          skipped.
        </>
      ) : null}
    </>
  ) : (
    <>
      &ldquo;{chatTitle}&rdquo; will be permanently deleted. This action cannot be undone.
    </>
  )

  const confirmLabel = isBulk
    ? isDeleting
      ? 'Deleting…'
      : `Delete ${chatCount} chats`
    : isDeleting
      ? 'Deleting…'
      : 'Delete Chat'

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          <DialogDescription>{description}</DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={isDeleting}>
            Cancel
          </Button>
          <Button variant="destructive" onClick={onConfirm} disabled={isDeleting}>
            {confirmLabel}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
