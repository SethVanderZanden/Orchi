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

/** Detail queries keep full message history — evict closed tabs and GC after a short idle window. */
const CHAT_DETAIL_GC_TIME_MS = 60_000

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
    gcTime: CHAT_DETAIL_GC_TIME_MS,
    refetchOnMount: true,
    placeholderData: (previous: ChatThread | undefined) => previous
  }
}

export function useChatDetail(chatId: string): UseQueryResult<ChatThread, Error> {
  return useQuery(getChatDetailQueryOptions(chatId))
}
