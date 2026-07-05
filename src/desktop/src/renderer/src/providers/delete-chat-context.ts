import { createContext, useContext } from 'react'

import type { ChatThread } from '@/lib/chat/types'

export type DeleteChatContextValue = {
  requestDelete: (chat: ChatThread) => void
  isDeletingChat: (chatId: string) => boolean
}

export const DeleteChatContext = createContext<DeleteChatContextValue | null>(null)

export function useDeleteChatContext(): DeleteChatContextValue {
  const context = useContext(DeleteChatContext)

  if (!context) {
    throw new Error('useDeleteChat must be used within DeleteChatProvider')
  }

  return context
}
