import { useCallback } from 'react'
import { useQueryClient } from '@tanstack/react-query'

import { getChat } from '@/lib/chat/api'
import { isLocalChat } from '@/lib/chat/chat-persistence'
import type { ChatThread } from '@/lib/chat/types'
import { mergeChatDetail } from '@/lib/chat/merge-chat-detail'
import { mergeChatLists } from '@/lib/chat/merge-chat-lists'
import { chatKeys } from '@/lib/query-keys'

type UseChatCacheOptions = {
  chats: ChatThread[]
}

type UseChatCacheResult = {
  getChat: (chatId: string) => ChatThread | undefined
  getChildChats: (parentChatId: string) => ChatThread[]
  loadChat: (chatId: string) => Promise<ChatThread | undefined>
  purgeFromQueryClient: (chatId: string) => void
}

export function useChatCache({ chats }: UseChatCacheOptions): UseChatCacheResult {
  const queryClient = useQueryClient()

  const getChatLocal = useCallback(
    (chatId: string) => {
      const detail = queryClient.getQueryData<ChatThread>(chatKeys.detail(chatId))
      const summary = chats.find((chat) => chat.id === chatId)

      if (detail && summary) {
        return { ...summary, ...detail, messages: detail.messages }
      }

      return detail ?? summary
    },
    [chats, queryClient]
  )

  const getChildChats = useCallback(
    (parentChatId: string) => chats.filter((chat) => chat.parentChatId === parentChatId),
    [chats]
  )

  const loadChat = useCallback(
    async (chatId: string) => {
      if (isLocalChat(chatId)) {
        return getChatLocal(chatId)
      }

      const existing = queryClient.getQueryData<ChatThread>(chatKeys.detail(chatId))
      const incoming = await getChat(chatId)
      const merged = mergeChatDetail(existing, incoming)

      queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) =>
        mergeChatLists(current, [merged])
      )

      queryClient.setQueryData(chatKeys.detail(chatId), merged)

      return merged
    },
    [getChatLocal, queryClient]
  )

  const purgeFromQueryClient = useCallback(
    (chatId: string) => {
      queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) =>
        current.filter((chat) => chat.id !== chatId)
      )
      queryClient.removeQueries({ queryKey: chatKeys.detail(chatId) })
    },
    [queryClient]
  )

  return {
    getChat: getChatLocal,
    getChildChats,
    loadChat,
    purgeFromQueryClient
  }
}
