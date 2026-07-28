import { hasComposerDraft } from '@/lib/chat/composer-drafts'
import type { ChatThread } from '@/lib/chat/types'

type DisposableEmptyChatOptions = {
  /** While preparing/sending (e.g. worktree provision), never treat as disposable. */
  isSending?: boolean
}

/** True when closing the tab should discard the chat entirely (no messages, no composer draft). */
export function isDisposableEmptyChat(
  chat: ChatThread | undefined,
  chatId: string,
  options?: DisposableEmptyChatOptions
): boolean {
  if (options?.isSending) {
    return false
  }

  if (hasComposerDraft(chatId)) {
    return false
  }

  if (!chat) {
    return false
  }

  return chat.messages.length === 0
}
