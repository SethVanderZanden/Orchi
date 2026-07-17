import type { ChatStatus } from '@/lib/chat/types'

/** Prefer the more advanced status so late InProgress snapshots cannot clobber Ready/Read. */
export function preferChatStatus(
  current: ChatStatus | undefined,
  incoming: ChatStatus
): ChatStatus {
  if (!current || current === incoming) {
    return incoming
  }

  if (incoming === 'inProgress' && (current === 'readyForReview' || current === 'read')) {
    return current
  }

  return incoming
}
