import { useEffect } from 'react'
import { useQueryClient } from '@tanstack/react-query'

import type { ChatThread } from '@/lib/chat/types'
import { createOrchestrationEventHandlers } from '@/lib/orchestration/orchestration-cache'
import { subscribeOrchestrationEvents } from '@/lib/orchestration/orchestration-events'

type UseOrchestrationParentEventsOptions = {
  childChat: ChatThread | undefined
  parentChat: ChatThread | undefined
}

export function useOrchestrationParentEvents({
  childChat,
  parentChat
}: UseOrchestrationParentEventsOptions): void {
  const queryClient = useQueryClient()

  useEffect(() => {
    if (!childChat?.parentChatId || !parentChat || parentChat.mode !== 'orchestration') {
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
  }, [childChat?.id, childChat?.parentChatId, parentChat, queryClient])
}
