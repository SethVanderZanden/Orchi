import { useCallback, useEffect, useRef, useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import type { NavigateOptions } from '@tanstack/react-router'

import { sendMessageStream } from '@/lib/chat/api'
import { isLocalChat } from '@/lib/chat/chat-persistence'
import {
  appendUserAndAssistantMessages,
  isAbortError,
  finalizeAssistantMessages,
  updateMessageInThread
} from '@/lib/chat/message-updates'
import { createMessageStreamHandlers } from '@/lib/chat/message-stream-handlers'
import { registerChatIdMigrator } from '@/lib/chat/migrate-chat-client-state'
import { resolveDetailCache } from '@/lib/chat/resolve-detail-cache'
import { maybeHydrateOrchestrationAfterChildSend } from '@/lib/orchestration/orchestration-cache'
import { promoteLocalChat } from '@/lib/chat/promote-local-chat'
import type { ChatMarker, ChatThread, SendMessageOptions } from '@/lib/chat/types'
import { chatKeys } from '@/lib/query-keys'

import type { AgentActivityDetail } from '@/lib/chat/types'

type UseChatStreamOptions = {
  getChat: (chatId: string) => ChatThread | undefined
  loadChat: (chatId: string) => Promise<ChatThread | undefined>
  refetchChats: () => Promise<unknown>
  activeChatId?: string
  navigate: (options: NavigateOptions) => void
  applyPostMessageBehavior?: (chatId: string) => void | Promise<void>
}

type UseChatStreamResult = {
  sendMessage: (chatId: string, content: string, options?: SendMessageOptions) => Promise<void>
  isChatSending: (chatId: string) => boolean
  getMarkers: (chatId: string) => ChatMarker[]
  subscribeAgentActivity: (listener: (detail: AgentActivityDetail) => void) => () => void
  abortStream: (chatId: string) => void
  purgeStreamState: (chatId: string) => void
}

export function useChatStream({
  getChat,
  loadChat,
  refetchChats,
  activeChatId,
  navigate,
  applyPostMessageBehavior
}: UseChatStreamOptions): UseChatStreamResult {
  const queryClient = useQueryClient()
  const [sendingChatIds, setSendingChatIds] = useState<Set<string>>(() => new Set())
  const [markersByChat, setMarkersByChat] = useState<Record<string, ChatMarker[]>>({})
  const streamAbortByChatRef = useRef<Map<string, AbortController>>(new Map())
  const turnGenerationByChatRef = useRef<Map<string, number>>(new Map())
  const agentActivityListenersRef = useRef(new Set<(detail: AgentActivityDetail) => void>())

  useEffect(() => {
    return registerChatIdMigrator((fromId, toId) => {
      setSendingChatIds((current) => {
        if (!current.has(fromId)) {
          return current
        }

        const next = new Set(current)
        next.delete(fromId)
        next.add(toId)
        return next
      })

      setMarkersByChat((current) => {
        if (!(fromId in current)) {
          return current
        }

        const next = { ...current }
        next[toId] = next[fromId] ?? []
        delete next[fromId]
        return next
      })

      const controller = streamAbortByChatRef.current.get(fromId)
      if (controller) {
        streamAbortByChatRef.current.delete(fromId)
        streamAbortByChatRef.current.set(toId, controller)
      }

      const turnGeneration = turnGenerationByChatRef.current.get(fromId)
      if (turnGeneration !== undefined) {
        turnGenerationByChatRef.current.delete(fromId)
        turnGenerationByChatRef.current.set(toId, turnGeneration)
      }
    })
  }, [])

  const subscribeAgentActivity = useCallback((listener: (detail: AgentActivityDetail) => void) => {
    agentActivityListenersRef.current.add(listener)
    return () => {
      agentActivityListenersRef.current.delete(listener)
    }
  }, [])

  const notifyAgentActivity = useCallback((detail: AgentActivityDetail) => {
    agentActivityListenersRef.current.forEach((listener) => listener(detail))
  }, [])

  const appendMarker = useCallback((chatId: string, marker: ChatMarker) => {
    setMarkersByChat((current) => ({
      ...current,
      [chatId]: [...(current[chatId] ?? []), marker]
    }))
  }, [])

  const clearMarkers = useCallback((chatId: string) => {
    setMarkersByChat((current) => {
      if (!(chatId in current)) {
        return current
      }

      const next = { ...current }
      delete next[chatId]
      return next
    })
  }, [])

  const markChatSending = useCallback((chatId: string, sending: boolean) => {
    setSendingChatIds((current) => {
      const hasChat = current.has(chatId)
      if (sending === hasChat) {
        return current
      }

      const next = new Set(current)
      if (sending) {
        next.add(chatId)
      } else {
        next.delete(chatId)
      }

      return next
    })
  }, [])

  const isChatSending = useCallback(
    (chatId: string) => sendingChatIds.has(chatId),
    [sendingChatIds]
  )

  const abortStream = useCallback((chatId: string) => {
    streamAbortByChatRef.current.get(chatId)?.abort()
    streamAbortByChatRef.current.delete(chatId)
  }, [])

  const purgeStreamState = useCallback(
    (chatId: string) => {
      abortStream(chatId)
      markChatSending(chatId, false)
      clearMarkers(chatId)
    },
    [abortStream, clearMarkers, markChatSending]
  )

  const finalizeInterruptedAssistant = useCallback(
    (chatId: string) => {
      queryClient.setQueryData<ChatThread>(chatKeys.detail(chatId), (current) => {
        const base = current ?? resolveDetailCache(queryClient, chatId, getChat)
        if (!base) {
          return current
        }

        const { messages, changed } = finalizeAssistantMessages(base.messages)
        if (!changed) {
          return base
        }

        return {
          ...base,
          messages,
          updatedAt: new Date().toISOString()
        }
      })
    },
    [getChat, queryClient]
  )

  const updateAssistantMessage = useCallback(
    (
      chatId: string,
      assistantMessageId: string,
      updater: Parameters<typeof updateMessageInThread>[2]
    ) => {
      queryClient.setQueryData<ChatThread>(chatKeys.detail(chatId), (current) => {
        const base = current ?? resolveDetailCache(queryClient, chatId, getChat)
        if (!base) {
          return current
        }

        return updateMessageInThread(base, assistantMessageId, updater)
      })
    },
    [getChat, queryClient]
  )

  const releaseStream = useCallback(
    (chatId: string, controller: AbortController) => {
      if (streamAbortByChatRef.current.get(chatId) !== controller) {
        return
      }

      streamAbortByChatRef.current.delete(chatId)
      markChatSending(chatId, false)
    },
    [markChatSending]
  )

  const sendMessage = useCallback(
    async (chatId: string, content: string, options?: SendMessageOptions) => {
      let resolvedChatId = chatId

      try {
        if (isLocalChat(chatId)) {
          resolvedChatId = await promoteLocalChat(queryClient, chatId)
          if (activeChatId === chatId) {
            navigate({
              to: '/chat/$chatId',
              params: { chatId: resolvedChatId },
              replace: true
            })
          }
        }
      } catch (error) {
        throw error instanceof Error ? error : new Error('Failed to create chat.')
      }

      abortStream(resolvedChatId)
      finalizeInterruptedAssistant(resolvedChatId)

      const controller = new AbortController()
      streamAbortByChatRef.current.set(resolvedChatId, controller)

      const turnGeneration = (turnGenerationByChatRef.current.get(resolvedChatId) ?? 0) + 1
      turnGenerationByChatRef.current.set(resolvedChatId, turnGeneration)

      const isActiveTurn = (): boolean =>
        turnGenerationByChatRef.current.get(resolvedChatId) === turnGeneration

      markChatSending(resolvedChatId, true)
      clearMarkers(resolvedChatId)

      const assistantMessageId = crypto.randomUUID()
      const startedAt = new Date().toISOString()

      queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) =>
        current.map((chat) =>
          chat.id === resolvedChatId
            ? { ...chat, status: 'inProgress' as const, updatedAt: startedAt }
            : chat
        )
      )

      queryClient.setQueryData<ChatThread>(chatKeys.detail(resolvedChatId), (current) => {
        const base = current ?? resolveDetailCache(queryClient, resolvedChatId, getChat)
        if (!base) {
          return current
        }

        return appendUserAndAssistantMessages(
          { ...base, status: 'inProgress', updatedAt: startedAt },
          content,
          assistantMessageId
        )
      })

      appendMarker(resolvedChatId, {
        id: crypto.randomUUID(),
        content: 'Agent is working…',
        variant: 'status'
      })

      try {
        await sendMessageStream(
          resolvedChatId,
          content,
          createMessageStreamHandlers({
            isActiveTurn,
            assistantMessageId,
            chatId: resolvedChatId,
            updateAssistantMessage,
            appendMarker,
            clearMarkers,
            notifyAgentActivity
          }),
          controller.signal
        )

        if (!isActiveTurn()) {
          return
        }

        // Optimistically leave InProgress so the tab does not stay amber if SSE lags.
        const readyAt = new Date().toISOString()
        const markReadyForReview = (): void => {
          queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) =>
            current.map((chat) =>
              chat.id === resolvedChatId
                ? { ...chat, status: 'readyForReview' as const, updatedAt: readyAt }
                : chat
            )
          )
          queryClient.setQueryData<ChatThread>(chatKeys.detail(resolvedChatId), (current) => {
            if (!current) {
              return current
            }

            return { ...current, status: 'readyForReview', updatedAt: readyAt }
          })
        }

        markReadyForReview()

        await loadChat(resolvedChatId)

        // loadChat can briefly reintroduce stale in-memory InProgress; re-assert.
        const loaded = queryClient.getQueryData<ChatThread>(chatKeys.detail(resolvedChatId))
        if (loaded?.status === 'inProgress') {
          markReadyForReview()
        }

        void refetchChats()

        maybeHydrateOrchestrationAfterChildSend(
          queryClient.getQueryData<ChatThread>(chatKeys.detail(resolvedChatId)),
          queryClient,
          getChat,
          loadChat
        )

        if (
          !options?.skipPostMessageBehavior &&
          applyPostMessageBehavior &&
          resolvedChatId === activeChatId
        ) {
          await applyPostMessageBehavior(resolvedChatId)
        }
      } catch (error) {
        if (isAbortError(error) || !isActiveTurn()) {
          return
        }

        updateAssistantMessage(resolvedChatId, assistantMessageId, (currentMessage) => ({
          ...currentMessage,
          content:
            currentMessage.content ||
            (error instanceof Error ? error.message : 'Failed to send message.'),
          status: 'error'
        }))
        clearMarkers(resolvedChatId)
      } finally {
        releaseStream(resolvedChatId, controller)
      }
    },
    [
      abortStream,
      activeChatId,
      appendMarker,
      applyPostMessageBehavior,
      clearMarkers,
      finalizeInterruptedAssistant,
      getChat,
      loadChat,
      markChatSending,
      navigate,
      notifyAgentActivity,
      queryClient,
      refetchChats,
      releaseStream,
      updateAssistantMessage
    ]
  )

  const getMarkers = useCallback((chatId: string) => markersByChat[chatId] ?? [], [markersByChat])

  return {
    sendMessage,
    isChatSending,
    getMarkers,
    subscribeAgentActivity,
    abortStream,
    purgeStreamState
  }
}
