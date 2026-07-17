import { useEffect } from 'react'
import { useQueryClient } from '@tanstack/react-query'

import { subscribeChatStatusEvents } from '@/lib/chat/api'
import { notifyChatStatusListeners } from '@/lib/chat/chat-status-listeners'
import { preferChatStatus } from '@/lib/chat/prefer-chat-status'
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

  const updatedAt = new Date().toISOString()

  queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) =>
    current.map((chat) =>
      chat.id === chatId
        ? { ...chat, status: preferChatStatus(chat.status, status), updatedAt }
        : chat
    )
  )

  queryClient.setQueryData<ChatThread>(chatKeys.detail(chatId), (current) => {
    if (!current) {
      return current
    }

    return {
      ...current,
      status: preferChatStatus(current.status, status),
      updatedAt
    }
  })
}

export function useChatStatusEvents(): void {
  const queryClient = useQueryClient()

  useEffect(() => {
    const controller = new AbortController()
    let retryTimer: ReturnType<typeof setTimeout> | undefined
    let disposed = false

    const connect = (): void => {
      if (disposed) {
        return
      }

      void subscribeChatStatusEvents(
        {
          onSnapshot: (items) => {
            for (const item of items) {
              patchChatStatus(queryClient, item.chatId, item.status)
            }
          },
          onStatus: (payload) => {
            patchChatStatus(queryClient, payload.chatId, payload.status)
            notifyChatStatusListeners()
          }
        },
        controller.signal
      )
        .catch(() => {
          // connection dropped; retry so completion events are not missed forever
        })
        .finally(() => {
          if (disposed || controller.signal.aborted) {
            return
          }

          retryTimer = setTimeout(connect, 1500)
        })
    }

    connect()

    return () => {
      disposed = true
      controller.abort()
      if (retryTimer !== undefined) {
        clearTimeout(retryTimer)
      }
    }
  }, [queryClient])
}
