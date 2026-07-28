import { describe, expect, it } from 'vitest'

import {
  MAX_ATTACHMENT_FILE_SIZE_BYTES,
  MAX_ATTACHMENTS_PER_MESSAGE,
  validateAttachmentsForStaging
} from '@/lib/chat/attachment-limits'

function fakeFile(name: string, size: number): File {
  const buffer = new Uint8Array(Math.min(size, 16))
  const file = new File([buffer], name, { type: 'application/octet-stream' })
  Object.defineProperty(file, 'size', { value: size })
  return file
}

describe('validateAttachmentsForStaging', () => {
  it('accepts files within size and count limits', () => {
    const result = validateAttachmentsForStaging(
      [fakeFile('a.txt', 1024), fakeFile('b.txt', 2048)],
      0
    )

    expect(result.ok).toBe(true)
    if (result.ok) {
      expect(result.files).toHaveLength(2)
    }
  })

  it('rejects files over the size limit before staging', () => {
    const result = validateAttachmentsForStaging(
      [fakeFile('huge.bin', MAX_ATTACHMENT_FILE_SIZE_BYTES + 1)],
      0
    )

    expect(result.ok).toBe(false)
    if (!result.ok) {
      expect(result.error).toContain('huge.bin')
      expect(result.error).toContain('MB limit')
    }
  })

  it('rejects when staged count would exceed the per-message max', () => {
    const result = validateAttachmentsForStaging(
      [fakeFile('extra.txt', 10)],
      MAX_ATTACHMENTS_PER_MESSAGE
    )

    expect(result.ok).toBe(false)
    if (!result.ok) {
      expect(result.error).toContain(String(MAX_ATTACHMENTS_PER_MESSAGE))
    }
  })

  it('rejects empty files', () => {
    const result = validateAttachmentsForStaging([fakeFile('empty.txt', 0)], 0)

    expect(result.ok).toBe(false)
    if (!result.ok) {
      expect(result.error).toContain('empty')
    }
  })
})
