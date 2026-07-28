import { useCallback, useEffect, useRef, useState } from 'react'
import { X } from 'lucide-react'

import { Button } from '@/components/ui/button'
import { ScrollArea } from '@/components/ui/scroll-area'
import { useLiveRef } from '@/hooks/use-live-ref'
import { getChatAttachmentContentUrl } from '@/lib/chat/api'
import {
  attachmentKindFromAttachment,
  attachmentKindIcon
} from '@/lib/chat/attachment-icons'
import type { ChatAttachment } from '@/lib/chat/types'
import {
  ATTACHMENTS_PANEL_DEFAULT_WIDTH,
  clampAttachmentsPanelWidth,
  getAttachmentsPanelWidthBounds
} from '@/lib/layout/attachments-panel-width'
import { cn } from '@/lib/utils'

type AttachmentsPanelProps = {
  containerWidth: number
  chatId: string
  attachments: ChatAttachment[]
  onClose: () => void
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

export function AttachmentsPanel({
  containerWidth,
  chatId,
  attachments,
  onClose
}: AttachmentsPanelProps): React.JSX.Element {
  const [preferredWidth, setPreferredWidth] = useState(ATTACHMENTS_PANEL_DEFAULT_WIDTH)
  const isDragging = useRef(false)
  const dragStartX = useRef(0)
  const dragStartWidth = useRef(ATTACHMENTS_PANEL_DEFAULT_WIDTH)
  const containerWidthRef = useLiveRef(containerWidth)

  const width = clampAttachmentsPanelWidth(preferredWidth, containerWidth)
  const { min: minWidth, max: maxWidth } = getAttachmentsPanelWidthBounds(containerWidth)

  const handleMouseMove = useCallback(
    (event: MouseEvent) => {
      if (!isDragging.current) {
        return
      }

      const delta = dragStartX.current - event.clientX
      setPreferredWidth(
        clampAttachmentsPanelWidth(dragStartWidth.current + delta, containerWidthRef.current)
      )
    },
    [containerWidthRef]
  )

  const handleMouseUp = useCallback(() => {
    isDragging.current = false
    document.body.style.cursor = ''
    document.body.style.userSelect = ''
  }, [])

  useEffect(() => {
    window.addEventListener('mousemove', handleMouseMove)
    window.addEventListener('mouseup', handleMouseUp)
    return () => {
      window.removeEventListener('mousemove', handleMouseMove)
      window.removeEventListener('mouseup', handleMouseUp)
    }
  }, [handleMouseMove, handleMouseUp])

  const handleResizeMouseDown = (event: React.MouseEvent<HTMLDivElement>): void => {
    event.preventDefault()
    isDragging.current = true
    dragStartX.current = event.clientX
    dragStartWidth.current = width
    document.body.style.cursor = 'col-resize'
    document.body.style.userSelect = 'none'
  }

  return (
    <aside
      className="flex h-full shrink-0 border-l border-border bg-background"
      style={{ width }}
      aria-label="Chat attachments"
    >
      <div
        role="separator"
        aria-orientation="vertical"
        aria-valuemin={minWidth}
        aria-valuemax={maxWidth}
        aria-valuenow={width}
        className="w-1 shrink-0 cursor-col-resize bg-transparent hover:bg-border/80"
        onMouseDown={handleResizeMouseDown}
      />
      <div className="flex min-w-0 flex-1 flex-col">
        <div className="flex items-center justify-between gap-2 border-b border-border px-4 py-3">
          <div>
            <h2 className="text-sm font-medium">Attachments</h2>
            <p className="text-xs text-muted-foreground">{attachments.length} file(s)</p>
          </div>
          <Button type="button" variant="ghost" size="icon" className="size-8" onClick={onClose}>
            <X className="size-4" />
            <span className="sr-only">Close attachments panel</span>
          </Button>
        </div>
        <ScrollArea className="min-h-0 flex-1">
          <ul className="space-y-2 p-4">
            {attachments.map((attachment) => {
              const url = getChatAttachmentContentUrl(chatId, attachment.id)
              const kind = attachmentKindFromAttachment(attachment)
              const Icon = attachmentKindIcon(kind)

              return (
                <li key={attachment.id}>
                  <a
                    href={url}
                    target="_blank"
                    rel="noreferrer"
                    className={cn(
                      'flex items-start gap-3 rounded-lg border border-border/70 p-3 hover:bg-muted/30'
                    )}
                  >
                    {kind === 'image' ? (
                      <img src={url} alt="" className="size-12 shrink-0 rounded object-cover" />
                    ) : (
                      <Icon className="mt-0.5 size-4 shrink-0 text-muted-foreground" />
                    )}
                    <div className="min-w-0">
                      <p className="truncate text-sm font-medium">{attachment.fileName}</p>
                      <p className="text-xs text-muted-foreground">
                        {formatFileSize(attachment.sizeBytes)}
                      </p>
                    </div>
                  </a>
                </li>
              )
            })}
          </ul>
        </ScrollArea>
      </div>
    </aside>
  )
}
