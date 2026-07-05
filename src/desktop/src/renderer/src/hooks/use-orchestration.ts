import { useCallback, useEffect, useState } from 'react'

import { useQueryClient } from '@tanstack/react-query'

import type { ChatThread } from '@/lib/chat/types'

import {

  createOrchestrationEventHandlers,

  mergeOrchestrationChildren

} from '@/lib/orchestration/orchestration-cache'

import { getOrchestration, subscribeOrchestrationEvents } from '@/lib/orchestration/orchestration-events'

import {

  type OrchestrationWorkflowProgress,

  workflowProgressFromState

} from '@/lib/orchestration/orchestration-state'



type UseOrchestrationOptions = {

  parentChat: ChatThread | undefined

  onWorkflowProgress?: (progress: OrchestrationWorkflowProgress | null) => void

  onChildrenHydrated?: (childIds: string[]) => void

}



export function useOrchestration({

  parentChat,

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

  const [sequencePlanIds, setSequencePlanIds] = useState<string[]>([])



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



  useEffect(() => {

    if (!parentChat || parentChat.mode !== 'orchestration') {

      setWorkflowProgress(null)

      setSequencePlanIds([])

      return

    }



    const controller = new AbortController()



    void getOrchestration(parentChat.id)

      .then((state) => {

        setSequencePlanIds(state.sequencePlanIds)

        emitWorkflowProgress(workflowProgressFromState(state))

        const newChildIds = mergeOrchestrationChildren(parentChat, state.children, queryClient)

        if (newChildIds.length > 0) {

          onChildrenHydrated?.(newChildIds)

        }

      })

      .catch(() => {

        setWorkflowProgress(null)

      })



    void subscribeOrchestrationEvents(

      parentChat.id,

      createOrchestrationEventHandlers(parentChat, queryClient, { onWorkflow: applyWorkflow }),

      controller.signal

    ).catch(() => {

      // Stream closed on unmount or network error.

    })



    return () => {

      controller.abort()

    }

  }, [applyWorkflow, emitWorkflowProgress, onChildrenHydrated, parentChat, queryClient])



  return { workflowProgress, sequencePlanIds }

}


