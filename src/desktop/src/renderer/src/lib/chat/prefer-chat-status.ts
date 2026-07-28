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

/** Merge list summaries without dropping optimistic inProgress before the server catches up. */
export function mergeChatStatus(existing: ChatStatus, incoming: ChatStatus): ChatStatus {
  if (existing === 'inProgress' && incoming === 'read') {
    return 'inProgress'
  }

  return preferChatStatus(existing, incoming)
}
