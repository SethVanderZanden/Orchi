import { useEffect } from 'react'
import { useQueryClient } from '@tanstack/react-query'

import { useLiveRef } from '@/hooks/use-live-ref'
import type { ChatThread } from '@/lib/chat/types'
import { createOrchestrationEventHandlers } from '@/lib/orchestration/orchestration-cache'
import { needsOrchestrationHydration } from '@/lib/orchestration/needs-orchestration-hydration'
import { subscribeOrchestrationEvents } from '@/lib/orchestration/orchestration-events'
import { chatKeys } from '@/lib/query-keys'

type UseOrchestrationParentEventsOptions = {
  childChat: ChatThread | undefined
  parentChat: ChatThread | undefined
  isParentKickoffActive: boolean
  getChat: (chatId: string) => ChatThread | undefined
  loadChat?: (chatId: string) => Promise<ChatThread | undefined>
}

export function useOrchestrationParentEvents({
  childChat,
  parentChat,
  isParentKickoffActive,
  getChat,
  loadChat
}: UseOrchestrationParentEventsOptions): void {
  const queryClient = useQueryClient()
  const parentChatRef = useLiveRef(parentChat)
  const getChatRef = useLiveRef(getChat)
  const loadChatRef = useLiveRef(loadChat)

  const parentChatId = parentChat?.id

  useEffect(() => {
    const parent = parentChatRef.current
    if (!childChat?.parentChatId || !parentChatId || !parent) {
      return
    }

    const parentChildCount =
      queryClient
        .getQueryData<ChatThread[]>(chatKeys.lists())
        ?.filter((chat) => chat.parentChatId === parentChatId).length ?? 0

    if (!needsOrchestrationHydration(parent, parentChildCount, isParentKickoffActive)) {
      return
    }

    const controller = new AbortController()

    void subscribeOrchestrationEvents(
      parentChatId,
      createOrchestrationEventHandlers(
        parent,
        queryClient,
        (chatId) => getChatRef.current(chatId),
        {
          loadChat: (chatId) => loadChatRef.current?.(chatId) ?? Promise.resolve(undefined)
        }
      ),
      controller.signal
    ).catch(() => {
      // Stream closed on unmount or network error.
    })

    return () => {
      controller.abort()
    }
  }, [
    childChat?.parentChatId,
    getChatRef,
    isParentKickoffActive,
    loadChatRef,
    parentChatId,
    parentChatRef,
    queryClient
  ])
}
