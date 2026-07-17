import { hasComposerDraft } from '@/lib/chat/composer-drafts'
import type { ChatThread } from '@/lib/chat/types'

/** True when closing the tab should discard the chat entirely (no messages, no composer draft). */
export function isDisposableEmptyChat(chat: ChatThread | undefined, chatId: string): boolean {
  if (hasComposerDraft(chatId)) {
    return false
  }

  if (!chat) {
    return false
  }

  return chat.messages.length === 0
}
