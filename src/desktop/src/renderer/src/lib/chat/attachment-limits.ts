/** Matches API `AttachmentOptions.MaxFileSizeBytes` (25 MB). */
export const MAX_ATTACHMENT_FILE_SIZE_BYTES = 26_214_400

/** Matches API `AttachmentOptions.MaxFilesPerMessage`. */
export const MAX_ATTACHMENTS_PER_MESSAGE = 10

const MAX_ATTACHMENT_FILE_SIZE_MB = MAX_ATTACHMENT_FILE_SIZE_BYTES / (1024 * 1024)

export type AttachmentValidationResult =
  | { ok: true; files: File[] }
  | { ok: false; message: string }

export function validateAttachmentFiles(
  incoming: File[],
  existingCount: number
): AttachmentValidationResult {
  if (incoming.length === 0) {
    return { ok: true, files: [] }
  }

  const remainingSlots = MAX_ATTACHMENTS_PER_MESSAGE - existingCount
  if (remainingSlots <= 0) {
    return {
      ok: false,
      message: `You can attach at most ${MAX_ATTACHMENTS_PER_MESSAGE} files per message.`
    }
  }

  if (incoming.length > remainingSlots) {
    return {
      ok: false,
      message: `You can attach at most ${MAX_ATTACHMENTS_PER_MESSAGE} files per message.`
    }
  }

  for (const file of incoming) {
    if (file.size > MAX_ATTACHMENT_FILE_SIZE_BYTES) {
      return {
        ok: false,
        message: `"${file.name}" exceeds the ${MAX_ATTACHMENT_FILE_SIZE_MB} MB limit.`
      }
    }
  }

  return { ok: true, files: incoming }
}
