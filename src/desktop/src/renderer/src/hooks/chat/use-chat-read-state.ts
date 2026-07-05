import { useCallback, useEffect } from 'react'

import { markChatRead as markChatReadInStorage } from '@/lib/chat/chat-read-state'
import {
  getChatSidebarStatus,
  type ChatSidebarStatusVariant
} from '@/lib/chat/chat-sidebar-status'
import type { ChatThread } from '@/lib/chat/types'

type UseChatReadStateOptions = {
  activeChatId?: string
  chats: ChatThread[]
  getChat: (chatId: string) => ChatThread | undefined
  getChildChats: (parentChatId: string) => ChatThread[]
  loadChat: (chatId: string) => Promise<ChatThread | undefined>
  isChatSending: (chatId: string) => boolean
  isParentKickingOffAny: (parentChatId: string) => boolean
}

export function useChatReadState({
  activeChatId,
  chats,
  getChat,
  getChildChats,
  loadChat,
  isChatSending,
  isParentKickingOffAny
}: UseChatReadStateOptions) {
  const activeChat = activeChatId ? getChat(activeChatId) : undefined

  useEffect(() => {
    if (!activeChatId || !activeChat) {
      return
    }

    markChatReadInStorage(activeChatId, activeChat.updatedAt)
  }, [activeChatId, activeChat?.updatedAt, activeChat?.messages.length])

  useEffect(() => {
    for (const chat of chats) {
      if (chat.mode !== 'orchestration') {
        continue
      }

      const resolved = getChat(chat.id)
      if (!resolved?.messages.length) {
        void loadChat(chat.id)
      }

      for (const child of getChildChats(chat.id)) {
        const resolvedChild = getChat(child.id)
        if (!resolvedChild?.messages.length) {
          void loadChat(child.id)
        }
      }
    }
  }, [chats, getChat, getChildChats, loadChat])

  const markChatRead = useCallback(
    (chatId: string) => {
      const chat = getChat(chatId)
      if (!chat) {
        return
      }

      markChatReadInStorage(chatId, chat.updatedAt)
    },
    [getChat]
  )

  const getChatSidebarStatusForChat = useCallback(
    (chat: ChatThread) =>
      getChatSidebarStatus({
        chat,
        activeChatId,
        isSending: isChatSending(chat.id),
        isParentKickingOff: isParentKickingOffAny(chat.id),
        getChat,
        getChildChats
      }),
    [activeChatId, getChat, getChildChats, isChatSending, isParentKickingOffAny]
  )

  return {
    markChatRead,
    getChatSidebarStatus: getChatSidebarStatusForChat
  }
}

export type { ChatSidebarStatusVariant }
