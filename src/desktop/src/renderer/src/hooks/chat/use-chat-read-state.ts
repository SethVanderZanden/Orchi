import { useCallback, useEffect } from 'react'

import { isLocalChat } from '@/lib/chat/chat-persistence'
import { markChatRead as markChatReadInStorage } from '@/lib/chat/chat-read-state'
import { getChatSidebarStatus, type ChatSidebarStatusVariant } from '@/lib/chat/chat-sidebar-status'
import { needsOrchestrationHydration } from '@/lib/orchestration/needs-orchestration-hydration'
import type { ChatThread } from '@/lib/chat/types'

type UseChatReadStateOptions = {
  activeChatId?: string
  getChat: (chatId: string) => ChatThread | undefined
  getChildChats: (parentChatId: string) => ChatThread[]
  loadChat: (chatId: string) => Promise<ChatThread | undefined>
  isChatSending: (chatId: string) => boolean
  isParentKickingOffAny: (parentChatId: string) => boolean
}

type UseChatReadStateResult = {
  markChatRead: (chatId: string) => void
  getChatSidebarStatus: (chat: ChatThread) => ChatSidebarStatusVariant
}

export function useChatReadState({
  activeChatId,
  getChat,
  getChildChats,
  loadChat,
  isChatSending,
  isParentKickingOffAny
}: UseChatReadStateOptions): UseChatReadStateResult {
  const activeChat = activeChatId ? getChat(activeChatId) : undefined

  useEffect(() => {
    if (!activeChatId || !activeChat || isLocalChat(activeChatId)) {
      return
    }

    markChatReadInStorage(activeChatId, activeChat.updatedAt)
  }, [activeChatId, activeChat?.updatedAt, activeChat?.messages.length, activeChat])

  useEffect(() => {
    if (!activeChatId) {
      return
    }

    const chat = getChat(activeChatId)
    const childCount = getChildChats(activeChatId).length

    if (!needsOrchestrationHydration(chat, childCount, isParentKickingOffAny(activeChatId))) {
      return
    }

    const resolved = getChat(activeChatId)
    if (!resolved?.messages.length) {
      void loadChat(activeChatId)
    }

    for (const child of getChildChats(activeChatId)) {
      const resolvedChild = getChat(child.id)
      if (resolvedChild && resolvedChild.messages.length === 0) {
        void loadChat(child.id)
      }
    }
  }, [activeChatId, getChat, getChildChats, isParentKickingOffAny, loadChat])

  const markChatRead = useCallback(
    (chatId: string) => {
      if (isLocalChat(chatId)) {
        return
      }

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
