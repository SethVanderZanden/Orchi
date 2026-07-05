import { useCallback, useRef, useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'

import { sendMessageStream } from '@/lib/chat/api'
import type { ChatMarker, ChatThread } from '@/lib/chat/types'
import {
  appendUserAndAssistantMessages,
  isAbortError,
  finalizeAssistantMessages,
  updateMessageInThread
} from '@/lib/chat/message-updates'
import { createMessageStreamHandlers } from '@/lib/chat/message-stream-handlers'
import { maybeHydrateOrchestrationAfterChildSend } from '@/lib/orchestration/orchestration-cache'
import { chatKeys } from '@/lib/query-keys'

import type { AgentActivityDetail } from '@/lib/chat/types'

type UseChatStreamOptions = {
  getChat: (chatId: string) => ChatThread | undefined
  loadChat: (chatId: string) => Promise<ChatThread | undefined>
  refetchChats: () => Promise<unknown>
}

export function useChatStream({ getChat, loadChat, refetchChats }: UseChatStreamOptions) {
  const queryClient = useQueryClient()
  const [sendingChatIds, setSendingChatIds] = useState<Set<string>>(() => new Set())
  const [markersByChat, setMarkersByChat] = useState<Record<string, ChatMarker[]>>({})
  const streamAbortByChatRef = useRef<Map<string, AbortController>>(new Map())
  const turnGenerationByChatRef = useRef<Map<string, number>>(new Map())
  const agentActivityListenersRef = useRef(new Set<(detail: AgentActivityDetail) => void>())

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
        if (!current) {
          return current
        }

        const { messages, changed } = finalizeAssistantMessages(current.messages)
        if (!changed) {
          return current
        }

        return {
          ...current,
          messages,
          updatedAt: new Date().toISOString()
        }
      })
    },
    [queryClient]
  )

  const updateAssistantMessage = useCallback(
    (chatId: string, assistantMessageId: string, updater: Parameters<typeof updateMessageInThread>[2]) => {
      queryClient.setQueryData<ChatThread>(chatKeys.detail(chatId), (current) => {
        if (!current) {
          return current
        }

        return updateMessageInThread(current, assistantMessageId, updater)
      })
    },
    [queryClient]
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
    async (chatId: string, content: string) => {
      abortStream(chatId)
      finalizeInterruptedAssistant(chatId)

      const controller = new AbortController()
      streamAbortByChatRef.current.set(chatId, controller)

      const turnGeneration = (turnGenerationByChatRef.current.get(chatId) ?? 0) + 1
      turnGenerationByChatRef.current.set(chatId, turnGeneration)

      const isActiveTurn = () => turnGenerationByChatRef.current.get(chatId) === turnGeneration

      markChatSending(chatId, true)
      clearMarkers(chatId)

      const assistantMessageId = crypto.randomUUID()

      queryClient.setQueryData<ChatThread>(chatKeys.detail(chatId), (current) => {
        if (!current) {
          return current
        }

        return appendUserAndAssistantMessages(current, content, assistantMessageId)
      })

      appendMarker(chatId, {
        id: crypto.randomUUID(),
        content: 'Agent is working…',
        variant: 'status'
      })

      try {
        await sendMessageStream(
          chatId,
          content,
          createMessageStreamHandlers({
            isActiveTurn,
            assistantMessageId,
            chatId,
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

        await loadChat(chatId)
        void refetchChats()

        maybeHydrateOrchestrationAfterChildSend(
          queryClient.getQueryData<ChatThread>(chatKeys.detail(chatId)),
          queryClient,
          getChat,
          loadChat
        )
      } catch (error) {
        if (isAbortError(error) || !isActiveTurn()) {
          return
        }

        updateAssistantMessage(chatId, assistantMessageId, (currentMessage) => ({
          ...currentMessage,
          content:
            currentMessage.content ||
            (error instanceof Error ? error.message : 'Failed to send message.'),
          status: 'error'
        }))
        clearMarkers(chatId)
      } finally {
        releaseStream(chatId, controller)
      }
    },
    [
      abortStream,
      appendMarker,
      clearMarkers,
      finalizeInterruptedAssistant,
      getChat,
      loadChat,
      markChatSending,
      notifyAgentActivity,
      queryClient,
      refetchChats,
      releaseStream,
      updateAssistantMessage
    ]
  )

  const getMarkers = useCallback(
    (chatId: string) => markersByChat[chatId] ?? [],
    [markersByChat]
  )

  return {
    sendMessage,
    isChatSending,
    getMarkers,
    subscribeAgentActivity,
    abortStream,
    purgeStreamState
  }
}
