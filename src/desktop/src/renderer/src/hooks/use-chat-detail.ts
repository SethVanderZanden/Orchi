import {
  useQuery,
  type QueryFunctionContext,
  type UseQueryOptions,
  type UseQueryResult
} from '@tanstack/react-query'

import { getChat } from '@/lib/chat/api'
import { isLocalChat } from '@/lib/chat/chat-persistence'
import { mergeChatDetail } from '@/lib/chat/merge-chat-detail'
import type { ChatThread } from '@/lib/chat/types'
import { chatKeys } from '@/lib/query-keys'

export function getChatDetailQueryOptions(
  chatId: string
): UseQueryOptions<ChatThread, Error, ChatThread, ReturnType<typeof chatKeys.detail>> {
  return {
    queryKey: chatKeys.detail(chatId),
    queryFn: async ({ client }: QueryFunctionContext<ReturnType<typeof chatKeys.detail>>) => {
      const existing = client.getQueryData<ChatThread>(chatKeys.detail(chatId))
      const incoming = await getChat(chatId)
      return mergeChatDetail(existing, incoming)
    },
    enabled: Boolean(chatId) && !isLocalChat(chatId),
    staleTime: 0,
    refetchOnMount: 'always' as const,
    placeholderData: (previous: ChatThread | undefined) => previous
  }
}

export function useChatDetail(chatId: string): UseQueryResult<ChatThread, Error> {
  return useQuery(getChatDetailQueryOptions(chatId))
}
