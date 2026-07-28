/** Keep in sync with API `Attachments:MaxFileSizeBytes` (25 MiB). */
export const MAX_ATTACHMENT_FILE_SIZE_BYTES = 25 * 1024 * 1024

/** Keep in sync with API `Attachments:MaxFilesPerMessage`. */
export const MAX_ATTACHMENTS_PER_MESSAGE = 10

export type AttachmentValidationResult =
  | { ok: true; files: File[] }
  | { ok: false; error: string }

function formatMb(bytes: number): string {
  return `${Math.round(bytes / (1024 * 1024))} MB`
}

/**
 * Reject oversized or excess files before they are staged in React state or uploaded.
 * Holding multi-GB `File` / clipboard blobs in the renderer is a primary memory-spike risk.
 */
export function validateAttachmentsForStaging(
  incoming: File[],
  alreadyStagedCount: number
): AttachmentValidationResult {
  if (incoming.length === 0) {
    return { ok: true, files: [] }
  }

  const remainingSlots = MAX_ATTACHMENTS_PER_MESSAGE - alreadyStagedCount
  if (remainingSlots <= 0) {
    return {
      ok: false,
      error: `A message can include at most ${MAX_ATTACHMENTS_PER_MESSAGE} attachments.`
    }
  }

  if (incoming.length > remainingSlots) {
    return {
      ok: false,
      error: `A message can include at most ${MAX_ATTACHMENTS_PER_MESSAGE} attachments.`
    }
  }

  for (const file of incoming) {
    if (file.size <= 0) {
      return { ok: false, error: `"${file.name}" is empty.` }
    }

    if (file.size > MAX_ATTACHMENT_FILE_SIZE_BYTES) {
      return {
        ok: false,
        error: `"${file.name}" exceeds the ${formatMb(MAX_ATTACHMENT_FILE_SIZE_BYTES)} limit.`
      }
    }
  }

  return { ok: true, files: incoming }
}
