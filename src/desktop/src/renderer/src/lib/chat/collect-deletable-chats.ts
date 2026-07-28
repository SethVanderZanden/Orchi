import type { ChatThread } from '@/lib/chat/types'

type CollectDeletableChatsOptions = {
  chats: ChatThread[]
  isChatSending: (chatId: string) => boolean
  isParentKickingOffAny: (chatId: string) => boolean
}

export type DeletableChatsResult = {
  deletable: ChatThread[]
  skippedSendingCount: number
}

/** Chats safe to bulk-delete: not actively sending or kicking off. */
export function collectDeletableChats({
  chats,
  isChatSending,
  isParentKickingOffAny
}: CollectDeletableChatsOptions): DeletableChatsResult {
  const deletable: ChatThread[] = []
  let skippedSendingCount = 0

  for (const chat of chats) {
    if (isChatSending(chat.id) || isParentKickingOffAny(chat.id)) {
      skippedSendingCount += 1
      continue
    }

    deletable.push(chat)
  }

  return { deletable, skippedSendingCount }
}
