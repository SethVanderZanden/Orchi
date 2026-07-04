import { useQuery, useQueryClient } from '@tanstack/react-query'

import { getChat } from '@/lib/chat/api'
import type { ChatThread } from '@/lib/chat/types'
import { chatKeys } from '@/lib/query-keys'

export function useChatDetail(chatId: string) {
  const queryClient = useQueryClient()

  return useQuery({
    queryKey: chatKeys.detail(chatId),
    queryFn: () => getChat(chatId),
    enabled: Boolean(chatId),
    placeholderData: () => {
      const cachedDetail = queryClient.getQueryData<ChatThread>(chatKeys.detail(chatId))
      if (cachedDetail) {
        return cachedDetail
      }

      const list = queryClient.getQueryData<ChatThread[]>(chatKeys.lists())
      return list?.find((chat) => chat.id === chatId)
    }
  })
}
