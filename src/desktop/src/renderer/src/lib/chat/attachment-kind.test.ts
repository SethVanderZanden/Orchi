import { describe, expect, it } from 'vitest'

import { resolveAttachmentKind } from '@/lib/chat/attachment-kind'

describe('resolveAttachmentKind', () => {
  it('classifies pdf and excel by extension even when MIME is generic', () => {
    expect(resolveAttachmentKind('spec.pdf', 'application/octet-stream')).toBe('pdf')
    expect(resolveAttachmentKind('budget.xlsx', '')).toBe('spreadsheet')
    expect(resolveAttachmentKind('legacy.xls', 'application/octet-stream')).toBe('spreadsheet')
    expect(resolveAttachmentKind('macro.xlsm', 'application/octet-stream')).toBe('spreadsheet')
  })

  it('classifies csv, images, and text', () => {
    expect(resolveAttachmentKind('data.csv', 'text/csv')).toBe('csv')
    expect(resolveAttachmentKind('photo.png', 'image/png')).toBe('image')
    expect(resolveAttachmentKind('notes.md', 'text/markdown')).toBe('text')
  })
})
