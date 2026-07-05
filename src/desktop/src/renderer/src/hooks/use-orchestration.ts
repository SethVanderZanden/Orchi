import { useCallback, useEffect, useState } from 'react'

import { useQueryClient } from '@tanstack/react-query'

import type { ChatThread } from '@/lib/chat/types'

import { createOrchestrationEventHandlers } from '@/lib/orchestration/orchestration-cache'

import { subscribeOrchestrationEvents } from '@/lib/orchestration/orchestration-events'

import type { OrchestrationWorkflowProgress } from '@/lib/orchestration/orchestration-state'

type UseOrchestrationOptions = {
  parentChatId: string | undefined
  parentChat: ChatThread | undefined
  enabled: boolean
  onWorkflowProgress?: (progress: OrchestrationWorkflowProgress | null) => void
  onChildrenHydrated?: (childIds: string[]) => void
}

export function useOrchestration({
  parentChatId,
  parentChat,
  enabled,
  onWorkflowProgress,
  onChildrenHydrated
}: UseOrchestrationOptions): {
  workflowProgress: OrchestrationWorkflowProgress | null
  sequencePlanIds: string[]
} {
  const queryClient = useQueryClient()

  const [workflowProgress, setWorkflowProgress] = useState<OrchestrationWorkflowProgress | null>(
    null
  )
  const [sequencePlanIds] = useState<string[]>([])

  const emitWorkflowProgress = useCallback(
    (progress: OrchestrationWorkflowProgress | null) => {
      setWorkflowProgress(progress)
      onWorkflowProgress?.(progress)
    },
    [onWorkflowProgress]
  )

  const applyWorkflow = useCallback(
    (payload: {
      status: string
      currentStep: number | null
      totalSteps: number | null
      planId: string | null
    }) => {
      if (payload.totalSteps === null || payload.totalSteps === 0 || payload.currentStep === null) {
        emitWorkflowProgress(
          payload.status === 'running'
            ? {
                active: true,
                currentStep: payload.currentStep ?? 1,
                totalSteps: payload.totalSteps ?? 1,
                status: payload.status
              }
            : null
        )
        return
      }

      emitWorkflowProgress({
        active: payload.status === 'running',
        currentStep: payload.currentStep,
        totalSteps: payload.totalSteps,
        status: payload.status
      })
    },
    [emitWorkflowProgress]
  )

  const isTracking = enabled && Boolean(parentChatId) && Boolean(parentChat)

  useEffect(() => {
    if (!isTracking) {
      return
    }

    const controller = new AbortController()

    void subscribeOrchestrationEvents(
      parentChatId!,
      createOrchestrationEventHandlers(parentChat!, queryClient, {
        onWorkflow: applyWorkflow,
        onChatCreated: (payload) => {
          onChildrenHydrated?.([payload.chatId])
        }
      }),
      controller.signal
    ).catch(() => {
      setWorkflowProgress(null)
    })

    return () => {
      controller.abort()
      setWorkflowProgress(null)
    }
  }, [applyWorkflow, isTracking, onChildrenHydrated, parentChat, parentChatId, queryClient])

  return { workflowProgress: isTracking ? workflowProgress : null, sequencePlanIds }
}
