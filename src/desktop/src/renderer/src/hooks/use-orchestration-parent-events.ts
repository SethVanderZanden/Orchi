import { useEffect } from 'react'
import { useQueryClient } from '@tanstack/react-query'

import type { ChatThread } from '@/lib/chat/types'
import { createOrchestrationEventHandlers } from '@/lib/orchestration/orchestration-cache'
import { needsOrchestrationHydration } from '@/lib/orchestration/needs-orchestration-hydration'
import { subscribeOrchestrationEvents } from '@/lib/orchestration/orchestration-events'

type UseOrchestrationParentEventsOptions = {
  childChat: ChatThread | undefined
  parentChat: ChatThread | undefined
  parentChildCount: number
  isParentKickoffActive: boolean
}

export function useOrchestrationParentEvents({
  childChat,
  parentChat,
  parentChildCount,
  isParentKickoffActive
}: UseOrchestrationParentEventsOptions): void {
  const queryClient = useQueryClient()

  useEffect(() => {
    if (
      !childChat?.parentChatId ||
      !parentChat ||
      !needsOrchestrationHydration(parentChat, parentChildCount, isParentKickoffActive)
    ) {
      return
    }

    const controller = new AbortController()

    void subscribeOrchestrationEvents(
      parentChat.id,
      createOrchestrationEventHandlers(parentChat, queryClient),
      controller.signal
    ).catch(() => {
      // Stream closed on unmount or network error.
    })

    return () => {
      controller.abort()
    }
  }, [
    childChat?.id,
    childChat?.parentChatId,
    isParentKickoffActive,
    parentChat,
    parentChildCount,
    queryClient
  ])
}
