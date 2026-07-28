import { X } from 'lucide-react'

import { Button } from '@/components/ui/button'
import { attachmentKindIcon, getAttachmentKind } from '@/lib/chat/attachment-icons'
import type { ChatAttachment } from '@/lib/chat/types'
import { cn } from '@/lib/utils'

export type ComposerStagedItem =
  | { kind: 'uploaded'; attachment: ChatAttachment }
  | { kind: 'pending'; localId: string; file: File }

type ComposerStagedAttachmentsProps = {
  items: ComposerStagedItem[]
  disabled?: boolean
  onRemove: (item: ComposerStagedItem) => void
}

function formatFileSize(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} B`
  }

  if (bytes < 1024 * 1024) {
    return `${(bytes / 1024).toFixed(1)} KB`
  }

  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

function itemLabel(item: ComposerStagedItem): string {
  return item.kind === 'uploaded' ? item.attachment.fileName : item.file.name
}

function itemSize(item: ComposerStagedItem): number {
  return item.kind === 'uploaded' ? item.attachment.sizeBytes : item.file.size
}

function itemKind(item: ComposerStagedItem) {
  if (item.kind === 'uploaded') {
    return getAttachmentKind(
      item.attachment.fileName,
      item.attachment.contentType,
      item.attachment.kind
    )
  }

  return getAttachmentKind(item.file.name, item.file.type || 'application/octet-stream')
}

export function ComposerStagedAttachments({
  items,
  disabled = false,
  onRemove
}: ComposerStagedAttachmentsProps): React.JSX.Element | null {
  if (items.length === 0) {
    return null
  }

  return (
    <div className="flex flex-wrap gap-2 border-b border-border/60 px-3.5 py-2.5">
      {items.map((item) => {
        const key = item.kind === 'uploaded' ? item.attachment.id : item.localId
        const Icon = attachmentKindIcon(itemKind(item))

        return (
          <div
            key={key}
            className={cn(
              'flex max-w-full items-center gap-2 rounded-lg border border-border/70 bg-background/80 px-2.5 py-1.5 text-xs'
            )}
          >
            <Icon className="size-3.5 shrink-0 text-muted-foreground" aria-hidden />
            <span className="truncate font-medium">{itemLabel(item)}</span>
            <span className="shrink-0 text-muted-foreground">{formatFileSize(itemSize(item))}</span>
            <Button
              type="button"
              variant="ghost"
              size="icon"
              className="size-6 shrink-0"
              disabled={disabled}
              aria-label={`Remove ${itemLabel(item)}`}
              onClick={() => onRemove(item)}
            >
              <X className="size-3.5" />
            </Button>
          </div>
        )
      })}
    </div>
  )
}
