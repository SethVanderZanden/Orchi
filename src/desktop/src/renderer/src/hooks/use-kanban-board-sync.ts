import { useEffect } from 'react'

import { subscribeChatStatusListeners } from '@/lib/chat/chat-status-listeners'
import { useChat } from '@/providers/chat-context'

/** Refetch the chat list when the board opens and when status SSE events arrive. */
export function useKanbanBoardSync(): void {
  const { refetchChats } = useChat()

  useEffect(() => {
    void refetchChats()
  }, [refetchChats])

  useEffect(() => {
    let debounceTimer: ReturnType<typeof setTimeout> | undefined

    return subscribeChatStatusListeners(() => {
      if (debounceTimer !== undefined) {
        clearTimeout(debounceTimer)
      }

      debounceTimer = setTimeout(() => {
        void refetchChats()
      }, 150)
    })
  }, [refetchChats])
}
