import type { QueryClient } from '@tanstack/react-query'

import type { ChatThread } from '@/lib/chat/types'
import { chatKeys } from '@/lib/query-keys'

export function applyWorkspaceToChatCache(
  queryClient: QueryClient,
  chatId: string,
  projectId: string | null,
  workspace: { id: string; path: string }
): void {
  const apply = (chat: ChatThread): ChatThread => ({
    ...chat,
    projectId,
    workspaceId: workspace.id,
    workspacePath: workspace.path
  })

  queryClient.setQueryData<ChatThread>(chatKeys.detail(chatId), (current) =>
    current ? apply(current) : current
  )

  queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) =>
    current.map((chat) => (chat.id === chatId ? apply(chat) : chat))
  )
}
