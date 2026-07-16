import { useCallback, useEffect, useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import type { NavigateOptions } from '@tanstack/react-router'

import { kickOffPlan } from '@/lib/chat/api'
import type { ChatThread } from '@/lib/chat/types'
import {
  isParentKickingOffAnyKeys,
  kickOffKey,
  removeKickoffKeysForParent
} from '@/lib/chat/kickoff-keys'
import { registerChatIdMigrator } from '@/lib/chat/migrate-chat-client-state'
import type { ParsedPlan } from '@/lib/orchestration/parse-plans'
import { mergeOrchestrationChildren } from '@/lib/orchestration/orchestration-cache'
import { kickOffAllOrchestration } from '@/lib/orchestration/orchestration-events'
import { workflowProgressFromState } from '@/lib/orchestration/orchestration-state'
import type { OrchestrationWorkflowProgress } from '@/lib/orchestration/orchestration-state'
import { chatKeys } from '@/lib/query-keys'
import { findChildForPlan } from '@/lib/projects/chat-tree'

type UseChatOrchestrationOptions = {
  getChat: (chatId: string) => ChatThread | undefined
  getChildChats: (parentChatId: string) => ChatThread[]
  loadChat: (chatId: string) => Promise<ChatThread | undefined>
  sendMessage: (chatId: string, content: string) => Promise<void>
  navigate: (options: NavigateOptions) => void
}

type UseChatOrchestrationResult = {
  kickOffPlan: (chatId: string, plan: ParsedPlan) => Promise<void>
  kickOffAllPlans: (chatId: string) => Promise<void>
  getOrchestrationKickoffProgress: (parentChatId: string) => OrchestrationWorkflowProgress | null
  setOrchestrationKickoffProgress: (
    parentChatId: string,
    progress: OrchestrationWorkflowProgress | null
  ) => void
  getOrchestrationError: (parentChatId: string) => string | null
  isPlanKickingOff: (parentChatId: string, planId: string) => boolean
  isParentKickingOffAny: (parentChatId: string) => boolean
  purgeKickoffState: (chatId: string) => void
}

export function useChatOrchestration({
  getChat,
  getChildChats,
  loadChat,
  sendMessage,
  navigate
}: UseChatOrchestrationOptions): UseChatOrchestrationResult {
  const queryClient = useQueryClient()
  const [kickingOffKeys, setKickingOffKeys] = useState<Set<string>>(() => new Set())
  const [orchestrationKickoffProgress, setOrchestrationKickoffProgressState] = useState<
    Record<string, OrchestrationWorkflowProgress | null>
  >({})
  const [orchestrationErrorByChatId, setOrchestrationErrorByChatId] = useState<
    Record<string, string | null>
  >({})

  useEffect(() => {
    return registerChatIdMigrator((fromId, toId) => {
      setKickingOffKeys((current) => {
        const prefix = `${fromId}:`
        let changed = false
        const next = new Set<string>()

        for (const key of current) {
          if (key.startsWith(prefix)) {
            changed = true
            next.add(`${toId}:${key.slice(prefix.length)}`)
          } else {
            next.add(key)
          }
        }

        return changed ? next : current
      })

      setOrchestrationKickoffProgressState((current) => {
        if (!(fromId in current)) {
          return current
        }

        const next = { ...current, [toId]: current[fromId] }
        delete next[fromId]
        return next
      })

      setOrchestrationErrorByChatId((current) => {
        if (!(fromId in current)) {
          return current
        }

        const next = { ...current, [toId]: current[fromId] }
        delete next[fromId]
        return next
      })
    })
  }, [])

  const setOrchestrationKickoffProgress = useCallback(
    (parentChatId: string, progress: OrchestrationWorkflowProgress | null) => {
      setOrchestrationKickoffProgressState((current) => ({
        ...current,
        [parentChatId]: progress
      }))
    },
    []
  )

  const getOrchestrationKickoffProgress = useCallback(
    (parentChatId: string) => orchestrationKickoffProgress[parentChatId] ?? null,
    [orchestrationKickoffProgress]
  )

  const getOrchestrationError = useCallback(
    (parentChatId: string) => orchestrationErrorByChatId[parentChatId] ?? null,
    [orchestrationErrorByChatId]
  )

  const clearOrchestrationError = useCallback((parentChatId: string) => {
    setOrchestrationErrorByChatId((current) => {
      if (!(parentChatId in current) || current[parentChatId] === null) {
        return current
      }

      return { ...current, [parentChatId]: null }
    })
  }, [])

  const markPlanKickingOff = useCallback(
    (parentChatId: string, planId: string, kickingOff: boolean) => {
      const key = kickOffKey(parentChatId, planId)
      setKickingOffKeys((current) => {
        const hasKey = current.has(key)
        if (kickingOff === hasKey) {
          return current
        }

        const next = new Set(current)
        if (kickingOff) {
          next.add(key)
        } else {
          next.delete(key)
        }

        return next
      })
    },
    []
  )

  const isPlanKickingOff = useCallback(
    (parentChatId: string, planId: string) => kickingOffKeys.has(kickOffKey(parentChatId, planId)),
    [kickingOffKeys]
  )

  const isParentKickingOffAny = useCallback(
    (parentChatId: string) => isParentKickingOffAnyKeys(parentChatId, kickingOffKeys),
    [kickingOffKeys]
  )

  const purgeKickoffState = useCallback((parentChatId: string) => {
    setKickingOffKeys((current) => removeKickoffKeysForParent(parentChatId, current))
    setOrchestrationErrorByChatId((current) => {
      if (!(parentChatId in current)) {
        return current
      }

      const next = { ...current }
      delete next[parentChatId]
      return next
    })
  }, [])

  const performKickOff = useCallback(
    async (chatId: string, plan: ParsedPlan, navigateToChild: boolean) => {
      const response = await kickOffPlan(chatId, {
        planId: plan.planId,
        title: plan.title,
        contentMarkdown: plan.contentMarkdown
      })

      const parentChat = getChat(chatId)
      const childChat: ChatThread = {
        id: response.childChatId,
        title: plan.title,
        preview: response.initialPrompt,
        updatedAt: new Date().toISOString(),
        agentId: 'cursor',
        projectId: parentChat?.projectId ?? null,
        workspaceId: parentChat?.workspaceId ?? null,
        workspacePath: parentChat?.workspacePath ?? '',
        mode: 'implementation',
        modelId: parentChat?.modelId ?? null,
        parentChatId: chatId,
        planFilePath: response.planFilePath,
        messages: []
      }

      queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) => [
        childChat,
        ...current
      ])

      queryClient.setQueryData(chatKeys.detail(childChat.id), childChat)

      if (navigateToChild) {
        navigate({ to: '/chat/$chatId', params: { chatId: childChat.id } })
      }

      await sendMessage(childChat.id, response.kickoffMessage)
    },
    [getChat, navigate, queryClient, sendMessage]
  )

  const kickOffPlanAction = useCallback(
    async (chatId: string, plan: ParsedPlan) => {
      const siblings = getChildChats(chatId)
      if (findChildForPlan(plan.planId, siblings)) {
        return
      }

      markPlanKickingOff(chatId, plan.planId, true)
      clearOrchestrationError(chatId)

      try {
        await performKickOff(chatId, plan, true)
      } catch (error) {
        setOrchestrationErrorByChatId((current) => ({
          ...current,
          [chatId]: error instanceof Error ? error.message : 'Failed to kick off plan.'
        }))
      } finally {
        markPlanKickingOff(chatId, plan.planId, false)
      }
    },
    [clearOrchestrationError, getChildChats, markPlanKickingOff, performKickOff]
  )

  const kickOffAllPlansAction = useCallback(
    async (chatId: string) => {
      markPlanKickingOff(chatId, '__all__', true)
      clearOrchestrationError(chatId)

      try {
        const state = await kickOffAllOrchestration(chatId)
        const parentChat = getChat(chatId)

        if (parentChat) {
          const newChildIds = mergeOrchestrationChildren(parentChat, state.children, queryClient)
          for (const childId of newChildIds) {
            void loadChat(childId)
          }
        }

        setOrchestrationKickoffProgress(chatId, workflowProgressFromState(state))
      } catch (error) {
        setOrchestrationErrorByChatId((current) => ({
          ...current,
          [chatId]: error instanceof Error ? error.message : 'Failed to kick off plans.'
        }))
      } finally {
        markPlanKickingOff(chatId, '__all__', false)
      }
    },
    [
      clearOrchestrationError,
      getChat,
      loadChat,
      markPlanKickingOff,
      queryClient,
      setOrchestrationKickoffProgress
    ]
  )

  return {
    kickOffPlan: kickOffPlanAction,
    kickOffAllPlans: kickOffAllPlansAction,
    getOrchestrationKickoffProgress,
    setOrchestrationKickoffProgress,
    getOrchestrationError,
    isPlanKickingOff,
    isParentKickingOffAny,
    purgeKickoffState
  }
}
