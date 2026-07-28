import type { AttachmentKind } from '@/lib/chat/types'

/** Primary file types highlighted in the attach picker (any file still works via "All files"). */
export const CHAT_ATTACHMENT_ACCEPT =
  '.pdf,.xlsx,.xls,.xlsm,.csv,image/*,.txt,.md,.json,.xml,.yaml,.yml,.log,application/pdf,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet,application/vnd.ms-excel'

export function resolveAttachmentKind(
  fileName: string,
  contentType: string
): AttachmentKind {
  const extension = fileName.includes('.')
    ? `.${fileName.split('.').pop()!.toLowerCase()}`
    : ''
  const type = contentType.toLowerCase()

  if (type.startsWith('image/') || ['.png', '.jpg', '.jpeg', '.gif', '.webp'].includes(extension)) {
    return 'image'
  }

  if (type === 'application/pdf' || extension === '.pdf') {
    return 'pdf'
  }

  if (
    ['.xlsx', '.xls', '.xlsm'].includes(extension) ||
    type.includes('spreadsheet') ||
    type === 'application/vnd.ms-excel' ||
    type === 'application/vnd.ms-excel.sheet.macroenabled.12'
  ) {
    return 'spreadsheet'
  }

  if (type === 'text/csv' || extension === '.csv') {
    return 'csv'
  }

  if (
    type.startsWith('text/') ||
    ['.txt', '.md', '.json', '.xml', '.yaml', '.yml', '.log'].includes(extension)
  ) {
    return 'text'
  }

  return 'other'
}
