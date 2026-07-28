import { useEffect, useState } from 'react'

import { useQueryClient } from '@tanstack/react-query'

import { useLiveRef } from '@/hooks/use-live-ref'
import type { ChatThread } from '@/lib/chat/types'

import {
  createOrchestrationEventHandlers,
  mergeOrchestrationChildren
} from '@/lib/orchestration/orchestration-cache'

import {
  getOrchestration,
  subscribeOrchestrationEvents
} from '@/lib/orchestration/orchestration-events'

import type { OrchestrationPlanResponse } from '@/lib/orchestration/orchestration-state'
import type { OrchestrationWorkflowProgress } from '@/lib/orchestration/orchestration-state'
import {
  workflowProgressFromState,
  workflowProgressFromWorkflowEvent
} from '@/lib/orchestration/orchestration-state'

type UseOrchestrationOptions = {
  parentChatId: string | undefined
  parentChat: ChatThread | undefined
  getChat: (chatId: string) => ChatThread | undefined
  loadChat?: (chatId: string) => Promise<ChatThread | undefined>
  enabled: boolean
  onWorkflowProgress?: (progress: OrchestrationWorkflowProgress | null) => void
  onChildrenHydrated?: (childIds: string[]) => void
}

export function useOrchestration({
  parentChatId,
  parentChat,
  getChat,
  loadChat,
  enabled,
  onWorkflowProgress,
  onChildrenHydrated
}: UseOrchestrationOptions): {
  workflowProgress: OrchestrationWorkflowProgress | null
  sequencePlanIds: string[]
  backendPlans: OrchestrationPlanResponse[]
} {
  const queryClient = useQueryClient()

  const [workflowProgress, setWorkflowProgress] = useState<OrchestrationWorkflowProgress | null>(
    null
  )
  const [sequencePlanIds, setSequencePlanIds] = useState<string[]>([])
  const [backendPlans, setBackendPlans] = useState<OrchestrationPlanResponse[]>([])

  const parentChatRef = useLiveRef(parentChat)
  const getChatRef = useLiveRef(getChat)
  const loadChatRef = useLiveRef(loadChat)
  const onWorkflowProgressRef = useLiveRef(onWorkflowProgress)
  const onChildrenHydratedRef = useLiveRef(onChildrenHydrated)

  const isTracking = enabled && Boolean(parentChatId) && Boolean(parentChat)
  const hasParentChat = Boolean(parentChat)
  const messageFingerprint =
    parentChat?.messages
      .filter((message) => message.role === 'assistant')
      .map((message) => `${message.id}:${message.status}`)
      .join('|') ?? ''

  useEffect(() => {
    if (!isTracking || !parentChatId) {
      return
    }

    const controller = new AbortController()

    const emitWorkflowProgress = (progress: OrchestrationWorkflowProgress | null): void => {
      setWorkflowProgress(progress)
      onWorkflowProgressRef.current?.(progress)
    }

    const applyOrchestrationState = (state: Awaited<ReturnType<typeof getOrchestration>>): void => {
      setBackendPlans(state.plans)
      setSequencePlanIds(state.sequencePlanIds)
      emitWorkflowProgress(workflowProgressFromState(state))
    }

    const resolveParentChat = (): ChatThread | undefined =>
      parentChatRef.current ?? getChatRef.current(parentChatId)

    void (async () => {
      try {
        const state = await getOrchestration(parentChatId)
        if (controller.signal.aborted) {
          return
        }

        const seededParent = resolveParentChat()
        if (seededParent) {
          const newChildIds = mergeOrchestrationChildren(seededParent, state.children, queryClient)
          if (newChildIds.length > 0) {
            onChildrenHydratedRef.current?.(newChildIds)
          }
          applyOrchestrationState(state)
        }
      } catch {
        // Seed failure is non-fatal; SSE may still deliver state.
      }

      const parent = resolveParentChat()
      if (!parent || controller.signal.aborted) {
        return
      }

      await subscribeOrchestrationEvents(
        parentChatId,
        createOrchestrationEventHandlers(
          parent,
          queryClient,
          (chatId) => getChatRef.current(chatId),
          {
            onWorkflow: (payload) =>
              emitWorkflowProgress(workflowProgressFromWorkflowEvent(payload)),
            onChatCreated: (payload) => {
              onChildrenHydratedRef.current?.([payload.chatId])
            },
            onParentMessage: () => {
              void getOrchestration(parentChatId)
                .then((state) => {
                  if (!controller.signal.aborted) {
                    applyOrchestrationState(state)
                  }
                })
                .catch(() => {
                  // Non-fatal refresh failure.
                })
            },
            loadChat: (chatId) => loadChatRef.current?.(chatId) ?? Promise.resolve(undefined)
          }
        ),
        controller.signal
      )
    })().catch(() => {
      // Stream closed on unmount or network error.
    })

    return () => {
      controller.abort()
    }
  }, [
    enabled,
    getChatRef,
    hasParentChat,
    isTracking,
    loadChatRef,
    messageFingerprint,
    onChildrenHydratedRef,
    onWorkflowProgressRef,
    parentChatId,
    parentChatRef,
    queryClient
  ])

  return {
    workflowProgress: isTracking ? workflowProgress : null,
    sequencePlanIds,
    backendPlans: isTracking ? backendPlans : []
  }
}
