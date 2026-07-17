import { useEffect } from 'react'
import { useQueryClient } from '@tanstack/react-query'

import { useLiveRef } from '@/hooks/use-live-ref'
import type { ChatThread } from '@/lib/chat/types'
import { createOrchestrationEventHandlers } from '@/lib/orchestration/orchestration-cache'
import { needsOrchestrationHydration } from '@/lib/orchestration/needs-orchestration-hydration'
import { subscribeOrchestrationEvents } from '@/lib/orchestration/orchestration-events'

type UseOrchestrationParentEventsOptions = {
  childChat: ChatThread | undefined
  parentChat: ChatThread | undefined
  parentChildCount: number
  isParentKickoffActive: boolean
  getChat: (chatId: string) => ChatThread | undefined
}

export function useOrchestrationParentEvents({
  childChat,
  parentChat,
  parentChildCount,
  isParentKickoffActive,
  getChat
}: UseOrchestrationParentEventsOptions): void {
  const queryClient = useQueryClient()
  const parentChatRef = useLiveRef(parentChat)
  const getChatRef = useLiveRef(getChat)

  const parentChatId = parentChat?.id

  useEffect(() => {
    const parent = parentChatRef.current
    if (
      !childChat?.parentChatId ||
      !parentChatId ||
      !parent ||
      !needsOrchestrationHydration(parent, parentChildCount, isParentKickoffActive)
    ) {
      return
    }

    const controller = new AbortController()

    void subscribeOrchestrationEvents(
      parentChatId,
      createOrchestrationEventHandlers(parent, queryClient, (chatId) => getChatRef.current(chatId)),
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
    parentChatId,
    parentChatRef,
    parentChildCount,
    queryClient
  ])
}
