import type { QueryClient } from '@tanstack/react-query'

import type { ChatThread } from '@/lib/chat/types'
import { chatKeys } from '@/lib/query-keys'

export function resolveDetailCache(
  queryClient: QueryClient,
  chatId: string,
  getChat: (chatId: string) => ChatThread | undefined
): ChatThread | undefined {
  return queryClient.getQueryData<ChatThread>(chatKeys.detail(chatId)) ?? getChat(chatId)
}
