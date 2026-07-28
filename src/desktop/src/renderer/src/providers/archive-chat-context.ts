import { createContext, useContext } from 'react'

import type { ChatThread } from '@/lib/chat/types'

export type ArchiveChatContextValue = {
  requestArchive: (chat: ChatThread) => void
  isArchivingChat: (chatId: string) => boolean
}

export const ArchiveChatContext = createContext<ArchiveChatContextValue | null>(null)

export function useArchiveChatContext(): ArchiveChatContextValue {
  const context = useContext(ArchiveChatContext)

  if (!context) {
    throw new Error('useArchiveChat must be used within ArchiveChatProvider')
  }

  return context
}
