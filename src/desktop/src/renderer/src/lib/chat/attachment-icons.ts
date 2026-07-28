import { FileIcon, FileSpreadsheet, FileText, ImageIcon, Table2 } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'

import { resolveAttachmentKind } from '@/lib/chat/attachment-kind'
import type { AttachmentKind, ChatAttachment } from '@/lib/chat/types'

export function getAttachmentKind(
  fileName: string,
  contentType: string,
  kind?: AttachmentKind
): AttachmentKind {
  return kind ?? resolveAttachmentKind(fileName, contentType)
}

export function attachmentKindFromAttachment(attachment: ChatAttachment): AttachmentKind {
  return getAttachmentKind(attachment.fileName, attachment.contentType, attachment.kind)
}

export function attachmentKindIcon(kind: AttachmentKind): LucideIcon {
  switch (kind) {
    case 'image':
      return ImageIcon
    case 'pdf':
      return FileText
    case 'spreadsheet':
      return FileSpreadsheet
    case 'csv':
      return Table2
    case 'text':
      return FileText
    default:
      return FileIcon
  }
}
