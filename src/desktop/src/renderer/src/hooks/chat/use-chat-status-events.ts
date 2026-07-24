import { useEffect } from 'react'
import { useQueryClient } from '@tanstack/react-query'

import { subscribeChatStatusEvents } from '@/lib/chat/api'
import { preferChatStatus } from '@/lib/chat/prefer-chat-status'
import type { ChatStatus, ChatThread } from '@/lib/chat/types'
import { chatKeys } from '@/lib/query-keys'

type StatusItem = {
  chatId: string
  status: ChatStatus
}

function applyStatusToChat(chat: ChatThread, status: ChatStatus, updatedAt: string): ChatThread {
  const nextStatus = preferChatStatus(chat.status, status)
  if (nextStatus === chat.status) {
    return chat
  }

  return { ...chat, status: nextStatus, updatedAt }
}

/** Patch one chat status. Skips writes when status is unchanged to avoid render thrash. */
function patchChatStatus(
  queryClient: ReturnType<typeof useQueryClient>,
  chatId: string,
  status: ChatStatus
): void {
  if (!chatId) {
    return
  }

  const updatedAt = new Date().toISOString()
  let listChanged = false

  queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) => {
    const next = current.map((chat) => {
      if (chat.id !== chatId) {
        return chat
      }

      const patched = applyStatusToChat(chat, status, updatedAt)
      if (patched !== chat) {
        listChanged = true
      }
      return patched
    })

    return listChanged ? next : current
  })

  queryClient.setQueryData<ChatThread>(chatKeys.detail(chatId), (current) => {
    if (!current) {
      return current
    }

    return applyStatusToChat(current, status, updatedAt)
  })
}

/** Apply many status updates in one list write (SSE snapshots / reconnects). */
function patchChatStatuses(
  queryClient: ReturnType<typeof useQueryClient>,
  items: readonly StatusItem[]
): void {
  if (items.length === 0) {
    return
  }

  if (items.length === 1) {
    const only = items[0]
    if (only) {
      patchChatStatus(queryClient, only.chatId, only.status)
    }
    return
  }

  const statusByChatId = new Map<string, ChatStatus>()
  for (const item of items) {
    if (item.chatId) {
      statusByChatId.set(item.chatId, item.status)
    }
  }

  if (statusByChatId.size === 0) {
    return
  }

  const updatedAt = new Date().toISOString()

  queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) => {
    let changed = false
    const next = current.map((chat) => {
      const status = statusByChatId.get(chat.id)
      if (status === undefined) {
        return chat
      }

      const patched = applyStatusToChat(chat, status, updatedAt)
      if (patched !== chat) {
        changed = true
      }
      return patched
    })

    return changed ? next : current
  })

  for (const [chatId, status] of statusByChatId) {
    queryClient.setQueryData<ChatThread>(chatKeys.detail(chatId), (current) => {
      if (!current) {
        return current
      }

      return applyStatusToChat(current, status, updatedAt)
    })
  }
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
            patchChatStatuses(queryClient, items)
          },
          onStatus: (payload) => {
            patchChatStatus(queryClient, payload.chatId, payload.status)
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
