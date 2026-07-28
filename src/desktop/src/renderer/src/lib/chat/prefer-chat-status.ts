import type { ChatStatus } from '@/lib/chat/types'

/**
 * Merge chat status updates from SSE / mark-read / optimistic writes.
 * ReadyForReview is sticky against late InProgress (completion already won).
 * Read → InProgress is allowed so kickoff / a new turn can leave Done.
 */
export function preferChatStatus(
  current: ChatStatus | undefined,
  incoming: ChatStatus
): ChatStatus {
  if (!current || current === incoming) {
    return incoming
  }

  if (incoming === 'inProgress' && current === 'readyForReview') {
    return current
  }

  return incoming
}
