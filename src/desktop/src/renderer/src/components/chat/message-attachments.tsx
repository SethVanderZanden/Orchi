import { FileIcon } from 'lucide-react'

import { getChatAttachmentContentUrl } from '@/lib/chat/api'
import type { ChatAttachment } from '@/lib/chat/types'
import { cn } from '@/lib/utils'

type MessageAttachmentsProps = {
  chatId: string
  attachments: ChatAttachment[]
  className?: string
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

export function MessageAttachments({
  chatId,
  attachments,
  className
}: MessageAttachmentsProps): React.JSX.Element | null {
  if (attachments.length === 0) {
    return null
  }

  return (
    <div className={cn('mt-2 flex flex-col gap-2', className)}>
      {attachments.map((attachment) => {
        const url = getChatAttachmentContentUrl(chatId, attachment.id)
        const isImage = attachment.contentType.startsWith('image/')

        if (isImage) {
          return (
            <a
              key={attachment.id}
              href={url}
              target="_blank"
              rel="noreferrer"
              className="block max-w-xs overflow-hidden rounded-lg border border-border/70"
            >
              <img src={url} alt={attachment.fileName} className="max-h-48 w-full object-cover" />
            </a>
          )
        }

        const Icon = FileIcon
        return (
          <a
            key={attachment.id}
            href={url}
            target="_blank"
            rel="noreferrer"
            className="inline-flex max-w-full items-center gap-2 rounded-lg border border-border/70 bg-muted/30 px-2.5 py-1.5 text-xs hover:bg-muted/50"
          >
            <Icon className="size-3.5 shrink-0 text-muted-foreground" aria-hidden />
            <span className="truncate font-medium">{attachment.fileName}</span>
            <span className="shrink-0 text-muted-foreground">
              {formatFileSize(attachment.sizeBytes)}
            </span>
          </a>
        )
      })}
    </div>
  )
}
