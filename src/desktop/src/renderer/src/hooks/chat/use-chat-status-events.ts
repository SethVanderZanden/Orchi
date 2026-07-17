import { useEffect } from 'react'
import { useQueryClient } from '@tanstack/react-query'

import { subscribeChatStatusEvents } from '@/lib/chat/api'
import type { ChatStatus, ChatThread } from '@/lib/chat/types'
import { chatKeys } from '@/lib/query-keys'

function patchChatStatus(
  queryClient: ReturnType<typeof useQueryClient>,
  chatId: string,
  status: ChatStatus
): void {
  if (!chatId) {
    return
  }

  queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) =>
    current.map((chat) => (chat.id === chatId ? { ...chat, status } : chat))
  )

  queryClient.setQueryData<ChatThread>(chatKeys.detail(chatId), (current) => {
    if (!current) {
      return current
    }

    return { ...current, status }
  })
}

export function useChatStatusEvents(): void {
  const queryClient = useQueryClient()

  useEffect(() => {
    const controller = new AbortController()

    void subscribeChatStatusEvents(
      {
        onSnapshot: (items) => {
          for (const item of items) {
            patchChatStatus(queryClient, item.chatId, item.status)
          }
        },
        onStatus: (payload) => {
          patchChatStatus(queryClient, payload.chatId, payload.status)
        }
      },
      controller.signal
    ).catch(() => {
      // connection dropped; provider remount / navigation can retry
    })

    return () => {
      controller.abort()
    }
  }, [queryClient])
}
