import { describe, expect, it } from 'vitest'

import {
  MAX_ATTACHMENTS_PER_MESSAGE,
  MAX_ATTACHMENT_FILE_SIZE_BYTES,
  validateAttachmentFiles
} from './attachment-limits'

function makeFile(name: string, sizeBytes: number): File {
  const file = new File(['x'], name, { type: 'application/octet-stream' })
  Object.defineProperty(file, 'size', { value: sizeBytes })
  return file
}

describe('validateAttachmentFiles', () => {
  it('accepts files within size and count limits', () => {
    const files = [makeFile('a.txt', 1024), makeFile('b.txt', 2048)]

    expect(validateAttachmentFiles(files, 0)).toEqual({ ok: true, files })
  })

  it('rejects files larger than the limit', () => {
    const oversized = makeFile('huge.bin', MAX_ATTACHMENT_FILE_SIZE_BYTES + 1)

    const result = validateAttachmentFiles([oversized], 0)

    expect(result.ok).toBe(false)
    if (!result.ok) {
      expect(result.message).toContain('huge.bin')
      expect(result.message).toContain('25 MB')
    }
  })

  it('rejects when total staged files would exceed the per-message cap', () => {
    const files = [makeFile('one.txt', 10)]

    const result = validateAttachmentFiles(files, MAX_ATTACHMENTS_PER_MESSAGE)

    expect(result.ok).toBe(false)
    if (!result.ok) {
      expect(result.message).toContain(String(MAX_ATTACHMENTS_PER_MESSAGE))
    }
  })
})
