import type { ChatAttachment } from '@/lib/chat/types'

export function collectChatAttachments(
  messages: { attachments?: ChatAttachment[] }[]
): ChatAttachment[] {
  const seen = new Set<string>()
  const collected: ChatAttachment[] = []

  for (const message of messages) {
    for (const attachment of message.attachments ?? []) {
      if (seen.has(attachment.id)) {
        continue
      }

      seen.add(attachment.id)
      collected.push(attachment)
    }
  }

  return collected
}
