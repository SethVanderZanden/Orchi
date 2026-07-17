import { useCallback, useState } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'

import { listChats } from '@/lib/chat/api'
import { isLocalChat } from '@/lib/chat/chat-persistence'
import type { ChatThread } from '@/lib/chat/types'
import { mergeChatLists } from '@/lib/chat/merge-chat-lists'
import { chatKeys } from '@/lib/query-keys'

type UseChatListResult = {
  chats: ChatThread[]
  isLoadingChats: boolean
  isPendingChats: boolean
  isFetchingChats: boolean
  chatsError: Error | null
  refetchChats: () => Promise<unknown>
  searchQuery: string
  setSearchQuery: (query: string) => void
}

export function useChatList(): UseChatListResult {
  const queryClient = useQueryClient()
  const [searchQuery, setSearchQuery] = useState('')

  const { data, isLoading, isPending, isFetching, error, refetch } = useQuery({
    queryKey: chatKeys.lists(),
    queryFn: async () => {
      const fromApi = await listChats()
      const current = queryClient.getQueryData<ChatThread[]>(chatKeys.lists()) ?? []
      const localDrafts = current.filter((chat) => isLocalChat(chat.id))
      return mergeChatLists(localDrafts, fromApi)
    },
    refetchOnMount: 'always',
    retry: 3,
    retryDelay: (attempt) => Math.min(1000 * 2 ** attempt, 8000),
    placeholderData: (previous) => previous
  })

  const refetchChats = useCallback(async () => {
    const result = await refetch()
    if (result.data) {
      queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) =>
        mergeChatLists(current, result.data!)
      )
    }
    return result
  }, [refetch, queryClient])

  return {
    chats: data ?? [],
    isLoadingChats: isLoading,
    isPendingChats: isPending,
    isFetchingChats: isFetching,
    chatsError: error as Error | null,
    refetchChats,
    searchQuery,
    setSearchQuery
  }
}
