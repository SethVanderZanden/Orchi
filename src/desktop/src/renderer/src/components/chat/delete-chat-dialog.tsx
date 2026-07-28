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
  scopeLabel?: string
  onOpenChange: (open: boolean) => void
  onConfirm: () => void
  isDeleting?: boolean
}

export function DeleteChatDialog({
  open,
  chatCount,
  chatTitle = '',
  scopeLabel,
  onOpenChange,
  onConfirm,
  isDeleting = false
}: DeleteChatDialogProps): React.JSX.Element {
  const isBulk = chatCount > 1
  const title = isBulk ? `Delete ${chatCount} chats?` : 'Delete chat?'
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
          <DialogDescription>
            {isBulk ? (
              <>
                {scopeLabel ? `${scopeLabel}: ` : null}
                {chatCount} chats will be permanently deleted. Projects and workspaces are kept. This
                cannot be undone.
              </>
            ) : (
              <>
                &ldquo;{chatTitle}&rdquo; will be permanently deleted. This action cannot be undone.
              </>
            )}
          </DialogDescription>
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
