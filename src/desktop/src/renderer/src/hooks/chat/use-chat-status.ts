import { useCallback, useEffect } from 'react'
import { useQueryClient } from '@tanstack/react-query'

import { markChatRead as markChatReadApi } from '@/lib/chat/api'
import { isLocalChat } from '@/lib/chat/chat-persistence'
import { getChatStatusVariant, type ChatStatusVariant } from '@/lib/chat/chat-status-variant'
import { mergeChatThread } from '@/lib/chat/merge-chat-lists'
import { preferChatStatus } from '@/lib/chat/prefer-chat-status'
import { needsOrchestrationHydration } from '@/lib/orchestration/needs-orchestration-hydration'
import type { ChatThread } from '@/lib/chat/types'
import { chatKeys } from '@/lib/query-keys'

type UseChatStatusOptions = {
  activeChatId?: string
  getChat: (chatId: string) => ChatThread | undefined
  getChildChats: (parentChatId: string) => ChatThread[]
  loadChat: (chatId: string) => Promise<ChatThread | undefined>
  isChatSending: (chatId: string) => boolean
  isParentKickingOffAny: (parentChatId: string) => boolean
}

type UseChatStatusResult = {
  markChatRead: (chatId: string) => void
  getChatStatusVariant: (chat: ChatThread, options?: { isViewing?: boolean }) => ChatStatusVariant
}

function applyStatusToCaches(
  queryClient: ReturnType<typeof useQueryClient>,
  summary: ChatThread
): void {
  queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) =>
    current.map((chat) => {
      if (chat.id !== summary.id) {
        return chat
      }

      return mergeChatThread(chat, {
        ...chat,
        status: preferChatStatus(chat.status, summary.status),
        lastReadAt: summary.lastReadAt,
        updatedAt: summary.updatedAt
      })
    })
  )

  queryClient.setQueryData<ChatThread>(chatKeys.detail(summary.id), (current) => {
    if (!current) {
      return current
    }

    return {
      ...current,
      status: preferChatStatus(current.status, summary.status),
      lastReadAt: summary.lastReadAt,
      updatedAt: summary.updatedAt || current.updatedAt
    }
  })
}

export function useChatStatus({
  activeChatId,
  getChat,
  getChildChats,
  loadChat,
  isChatSending,
  isParentKickingOffAny
}: UseChatStatusOptions): UseChatStatusResult {
  const queryClient = useQueryClient()
  const activeChat = activeChatId ? getChat(activeChatId) : undefined
  const activeStatus = activeChat?.status

  useEffect(() => {
    if (!activeChatId || isLocalChat(activeChatId)) {
      return
    }

    let cancelled = false

    void markChatReadApi(activeChatId)
      .then((summary) => {
        if (cancelled) {
          return
        }

        applyStatusToCaches(queryClient, summary)
      })
      .catch(() => {
        // ignore mark-read failures; SSE / list refetch will reconcile
      })

    return () => {
      cancelled = true
    }
  }, [activeChatId, activeStatus, queryClient])

  useEffect(() => {
    if (!activeChatId || isLocalChat(activeChatId)) {
      return
    }

    const chat = getChat(activeChatId)
    if (chat && chat.messages.length === 0 && !isChatSending(activeChatId)) {
      void loadChat(activeChatId)
    }
  }, [activeChatId, getChat, isChatSending, loadChat])

  useEffect(() => {
    if (!activeChatId) {
      return
    }

    const chat = getChat(activeChatId)
    const childCount = getChildChats(activeChatId).length

    if (!needsOrchestrationHydration(chat, childCount, isParentKickingOffAny(activeChatId))) {
      return
    }

    for (const child of getChildChats(activeChatId)) {
      const resolvedChild = getChat(child.id)
      if (resolvedChild && resolvedChild.messages.length === 0) {
        void loadChat(child.id)
      }
    }
  }, [activeChatId, getChat, getChildChats, isParentKickingOffAny, loadChat])

  const markChatRead = useCallback(
    (chatId: string) => {
      if (isLocalChat(chatId)) {
        return
      }

      void markChatReadApi(chatId)
        .then((summary) => {
          applyStatusToCaches(queryClient, summary)
        })
        .catch(() => {
          // ignore
        })
    },
    [queryClient]
  )

  const getChatStatusVariantForChat = useCallback(
    (chat: ChatThread, options?: { isViewing?: boolean }) =>
      getChatStatusVariant({
        chat: getChat(chat.id) ?? chat,
        isSending: isChatSending(chat.id),
        isParentKickingOff: isParentKickingOffAny(chat.id),
        isViewing: options?.isViewing ?? chat.id === activeChatId
      }),
    [activeChatId, getChat, isChatSending, isParentKickingOffAny]
  )

  return {
    markChatRead,
    getChatStatusVariant: getChatStatusVariantForChat
  }
}

export type { ChatStatusVariant }
