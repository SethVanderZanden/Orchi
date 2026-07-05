import { useCallback, useState } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'

import { listChats } from '@/lib/chat/api'
import type { ChatThread } from '@/lib/chat/types'
import { mergeChatLists } from '@/lib/chat/merge-chat-lists'
import { chatKeys } from '@/lib/query-keys'

export function useChatList() {
  const queryClient = useQueryClient()
  const [searchQuery, setSearchQuery] = useState('')

  const chatsQuery = useQuery({
    queryKey: chatKeys.lists(),
    queryFn: listChats,
    refetchOnMount: 'always',
    retry: 3,
    retryDelay: (attempt) => Math.min(1000 * 2 ** attempt, 8000),
    placeholderData: (previous) => previous
  })

  const refetchChats = useCallback(async () => {
    const result = await chatsQuery.refetch()
    if (result.data) {
      queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) =>
        mergeChatLists(current, result.data!)
      )
    }
    return result
  }, [chatsQuery, queryClient])

  return {
    chats: chatsQuery.data ?? [],
    isLoadingChats: chatsQuery.isLoading,
    isPendingChats: chatsQuery.isPending,
    isFetchingChats: chatsQuery.isFetching,
    chatsError: chatsQuery.error as Error | null,
    refetchChats,
    searchQuery,
    setSearchQuery
  }
}
