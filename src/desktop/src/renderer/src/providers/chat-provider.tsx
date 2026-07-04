import { createContext, useCallback, useContext, useMemo, useRef, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from '@tanstack/react-router'

import { closeChat, createChat, getChat, kickOffPlan, kickOffReview, listChats, sendMessageStream, updateChatMode } from '@/lib/chat/api'
import type { CreateChatOptions } from '@/components/chat/chat-mode-selector'
import type { AgentMode, ChatMarker, ChatMessage, ChatThread } from '@/lib/chat/types'
import type { ParsedPlan } from '@/lib/orchestration/parse-plans'
import { mergeChatLists } from '@/lib/chat/merge-chat-lists'
import { chatKeys } from '@/lib/query-keys'
import {
  findChildForPlan,
  findReviewChildForPlan,
  isImplementationChildChat,
  planIdFromPlanFilePath
} from '@/lib/workspaces/chat-tree'

type AgentActivityDetail = {
  phase: 'tool' | 'done'
  label?: string
}

type ChatContextValue = {
  chats: ChatThread[]
  isLoadingChats: boolean
  isPendingChats: boolean
  isFetchingChats: boolean
  chatsError: Error | null
  refetchChats: () => Promise<unknown>
  searchQuery: string
  setSearchQuery: (query: string) => void
  getChat: (chatId: string) => ChatThread | undefined
  getChildChats: (parentChatId: string) => ChatThread[]
  loadChat: (chatId: string) => Promise<ChatThread | undefined>
  createChat: (options: CreateChatOptions) => Promise<ChatThread>
  updateChatMode: (chatId: string, mode: AgentMode) => Promise<void>
  getModeUpdateError: (chatId: string) => string | undefined
  closeChat: (chatId: string) => Promise<void>
  sendMessage: (chatId: string, content: string) => Promise<void>
  kickOffPlan: (chatId: string, plan: ParsedPlan) => Promise<void>
  kickOffAllPlans: (chatId: string, plans: ParsedPlan[]) => Promise<void>
  isChatSending: (chatId: string) => boolean
  isPlanKickingOff: (parentChatId: string, planId: string) => boolean
  isParentKickingOffAny: (parentChatId: string) => boolean
  getMarkers: (chatId: string) => ChatMarker[]
  subscribeAgentActivity: (listener: (detail: AgentActivityDetail) => void) => () => void
}

const ChatContext = createContext<ChatContextValue | null>(null)

function kickOffKey(parentChatId: string, planId: string): string {
  return `${parentChatId}:${planId}`
}

function isAbortError(error: unknown): boolean {
  return error instanceof DOMException && error.name === 'AbortError'
}

export function ChatProvider({ children }: { children: React.ReactNode }): React.JSX.Element {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [searchQuery, setSearchQuery] = useState('')
  const [sendingChatIds, setSendingChatIds] = useState<Set<string>>(() => new Set())
  const [kickingOffKeys, setKickingOffKeys] = useState<Set<string>>(() => new Set())
  const [markersByChat, setMarkersByChat] = useState<Record<string, ChatMarker[]>>({})
  const [modeUpdateErrorByChat, setModeUpdateErrorByChat] = useState<Record<string, string>>({})
  const streamAbortByChatRef = useRef<Map<string, AbortController>>(new Map())
  const turnGenerationByChatRef = useRef<Map<string, number>>(new Map())
  const reviewKickOffStartedRef = useRef<Set<string>>(new Set())
  const maybeKickOffReviewRef = useRef<(chatId: string) => void>(() => {})
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

  const chatsQuery = useQuery({
    queryKey: chatKeys.lists(),
    queryFn: listChats,
    refetchOnMount: 'always',
    retry: 3,
    retryDelay: (attempt) => Math.min(1000 * 2 ** attempt, 8000),
    placeholderData: (previous) => previous
  })

  const refetchChats = useCallback(async () => {
    const result = await chatsQuery.refetch()
    if (result.data) {
      queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) =>
        mergeChatLists(current, result.data!)
      )
    }
    return result
  }, [chatsQuery, queryClient])

  const chats = chatsQuery.data ?? []

  const getChatLocal = useCallback(
    (chatId: string) => {
      const cachedDetail = queryClient.getQueryData<ChatThread>(chatKeys.detail(chatId))
      return cachedDetail ?? chats.find((chat) => chat.id === chatId)
    },
    [chats, queryClient]
  )

  const getChildChats = useCallback(
    (parentChatId: string) =>
      chats.filter((chat) => chat.parentChatId === parentChatId),
    [chats]
  )

  const loadChat = useCallback(
    async (chatId: string) => {
      const detail = await queryClient.fetchQuery({
        queryKey: chatKeys.detail(chatId),
        queryFn: () => getChat(chatId)
      })

      queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) =>
        mergeChatLists(current, [detail])
      )

      queryClient.setQueryData(chatKeys.detail(chatId), detail)

      return detail
    },
    [queryClient]
  )

  const createChatMutation = useMutation({
    mutationFn: (options: CreateChatOptions) =>
      createChat({
        agent: 'cursor',
        workspaceId: options.workspaceId,
        mode: 'default'
      }),
    onSuccess: (chat) => {
      queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) => [chat, ...current])
      queryClient.setQueryData(chatKeys.detail(chat.id), chat)
      setSearchQuery('')
      navigate({ to: '/chat/$chatId', params: { chatId: chat.id } })
    }
  })

  const closeChatMutation = useMutation({
    mutationFn: closeChat,
    onSuccess: (_, chatId) => {
      queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) =>
        current.filter((chat) => chat.id !== chatId)
      )
      queryClient.removeQueries({ queryKey: chatKeys.detail(chatId) })
      setMarkersByChat((current) => {
        const next = { ...current }
        delete next[chatId]
        return next
      })
    }
  })

  const appendMarker = useCallback((chatId: string, marker: ChatMarker) => {
    setMarkersByChat((current) => ({
      ...current,
      [chatId]: [...(current[chatId] ?? []), marker]
    }))
  }, [])

  const clearMarkers = useCallback((chatId: string) => {
    setMarkersByChat((current) => {
      const next = { ...current }
      delete next[chatId]
      return next
    })
  }, [])

  const isChatSending = useCallback(
    (chatId: string) => sendingChatIds.has(chatId),
    [sendingChatIds]
  )

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
    (parentChatId: string) => {
      const prefix = `${parentChatId}:`
      for (const key of kickingOffKeys) {
        if (key.startsWith(prefix)) {
          return true
        }
      }

      return false
    },
    [kickingOffKeys]
  )

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

  const finalizeInterruptedAssistant = useCallback(
    (chatId: string) => {
      queryClient.setQueryData<ChatThread>(chatKeys.detail(chatId), (current) => {
        if (!current) {
          return current
        }

        let changed = false
        const messages = current.messages.map((message) => {
          if (
            message.role === 'assistant' &&
            (message.status === 'processing' || message.status === 'streaming')
          ) {
            changed = true
            return { ...message, status: 'complete' as const }
          }

          return message
        })

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
    (chatId: string, assistantMessageId: string, updater: (message: ChatMessage) => ChatMessage) => {
      queryClient.setQueryData<ChatThread>(chatKeys.detail(chatId), (current) => {
        if (!current) {
          return current
        }

        const messages = [...current.messages]
        const assistantIndex = messages.findIndex((message) => message.id === assistantMessageId)
        if (assistantIndex === -1) {
          return current
        }

        messages[assistantIndex] = updater(messages[assistantIndex])

        return {
          ...current,
          messages,
          preview: messages[assistantIndex].content || current.preview,
          updatedAt: new Date().toISOString()
        }
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
      streamAbortByChatRef.current.get(chatId)?.abort()
      streamAbortByChatRef.current.delete(chatId)
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

        const now = new Date().toISOString()
        const userMessage: ChatMessage = {
          id: crypto.randomUUID(),
          role: 'user',
          content,
          createdAt: now,
          status: 'complete'
        }

        const assistantMessage: ChatMessage = {
          id: assistantMessageId,
          role: 'assistant',
          content: '',
          createdAt: now,
          status: 'processing'
        }

        return {
          ...current,
          title: current.messages.length === 0 ? content.slice(0, 42) : current.title,
          preview: content,
          updatedAt: now,
          messages: [...current.messages, userMessage, assistantMessage]
        }
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
          {
            onToken: (text) => {
              if (!isActiveTurn()) {
                return
              }

              updateAssistantMessage(chatId, assistantMessageId, (message) => ({
                ...message,
                content: message.content + text,
                status: 'streaming'
              }))
            },
            onTool: (label) => {
              if (!isActiveTurn()) {
                return
              }

              appendMarker(chatId, {
                id: crypto.randomUUID(),
                content: label,
                variant: 'tool'
              })

              if (label.startsWith('Writing') || label.startsWith('Running')) {
                notifyAgentActivity({ phase: 'tool', label })
              }
            },
            onDone: () => {
              if (!isActiveTurn()) {
                return
              }

              updateAssistantMessage(chatId, assistantMessageId, (message) => ({
                ...message,
                status: 'complete'
              }))
              notifyAgentActivity({ phase: 'done' })
              clearMarkers(chatId)
              maybeKickOffReviewRef.current(chatId)
            },
            onError: (code, message) => {
              if (!isActiveTurn()) {
                return
              }

              updateAssistantMessage(chatId, assistantMessageId, (currentMessage) => ({
                ...currentMessage,
                content: currentMessage.content || message,
                status: 'error'
              }))
              appendMarker(chatId, {
                id: crypto.randomUUID(),
                content: `${code}: ${message}`,
                variant: 'tool'
              })
              clearMarkers(chatId)
            }
          },
          controller.signal
        )

        if (!isActiveTurn()) {
          return
        }

        await loadChat(chatId)
        void refetchChats()
      } catch (error) {
        if (isAbortError(error) || !isActiveTurn()) {
          return
        }

        updateAssistantMessage(chatId, assistantMessageId, (currentMessage) => ({
          ...currentMessage,
          content: currentMessage.content || (error instanceof Error ? error.message : 'Failed to send message.'),
          status: 'error'
        }))
        clearMarkers(chatId)
      } finally {
        releaseStream(chatId, controller)
      }
    },
    [
      appendMarker,
      clearMarkers,
      finalizeInterruptedAssistant,
      loadChat,
      refetchChats,
      markChatSending,
      notifyAgentActivity,
      queryClient,
      releaseStream,
      updateAssistantMessage
    ]
  )

  const updateChatModeAction = useCallback(
    async (chatId: string, mode: AgentMode) => {
      try {
        const response = await updateChatMode(chatId, { mode })

        queryClient.setQueryData<ChatThread>(chatKeys.detail(chatId), (current) =>
          current ? { ...current, mode: response.mode } : current
        )

        queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) =>
          current.map((chat) => (chat.id === chatId ? { ...chat, mode: response.mode } : chat))
        )

        setModeUpdateErrorByChat((current) => {
          if (!(chatId in current)) {
            return current
          }

          const next = { ...current }
          delete next[chatId]
          return next
        })
      } catch (error) {
        const message = error instanceof Error ? error.message : 'Failed to update chat mode.'
        setModeUpdateErrorByChat((current) => ({ ...current, [chatId]: message }))
      }
    },
    [queryClient]
  )

  const getModeUpdateError = useCallback(
    (chatId: string) => modeUpdateErrorByChat[chatId],
    [modeUpdateErrorByChat]
  )

  const performKickOff = useCallback(
    async (chatId: string, plan: ParsedPlan, navigateToChild: boolean) => {
      const response = await kickOffPlan(chatId, {
        planId: plan.planId,
        title: plan.title,
        contentMarkdown: plan.contentMarkdown
      })

      const childChat: ChatThread = {
        id: response.childChatId,
        title: plan.title,
        preview: response.initialPrompt,
        updatedAt: new Date().toISOString(),
        agentId: 'cursor',
        projectId: getChatLocal(chatId)?.projectId ?? null,
        workspaceId: getChatLocal(chatId)?.workspaceId ?? null,
        workspacePath: getChatLocal(chatId)?.workspacePath ?? '',
        mode: 'implementation',
        parentChatId: chatId,
        planFilePath: response.planFilePath,
        messages: []
      }

      queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) => [childChat, ...current])
      queryClient.setQueryData(chatKeys.detail(childChat.id), childChat)

      if (navigateToChild) {
        navigate({ to: '/chat/$chatId', params: { chatId: childChat.id } })
      }

      void sendMessage(childChat.id, response.kickoffMessage)
    },
    [getChatLocal, navigate, queryClient, sendMessage]
  )

  const kickOffPlanAction = useCallback(
    async (chatId: string, plan: ParsedPlan) => {
      const siblings = getChildChats(chatId)
      if (findChildForPlan(plan.planId, siblings)) {
        return
      }

      markPlanKickingOff(chatId, plan.planId, true)

      try {
        await performKickOff(chatId, plan, true)
      } finally {
        markPlanKickingOff(chatId, plan.planId, false)
      }
    },
    [getChildChats, markPlanKickingOff, performKickOff]
  )

  const kickOffAllPlansAction = useCallback(
    async (chatId: string, plans: ParsedPlan[]) => {
      const siblings = getChildChats(chatId)
      const plansToKick = plans.filter((plan) => !findChildForPlan(plan.planId, siblings))

      if (plansToKick.length === 0) {
        return
      }

      await Promise.all(
        plansToKick.map(async (plan) => {
          markPlanKickingOff(chatId, plan.planId, true)

          try {
            await performKickOff(chatId, plan, false)
          } finally {
            markPlanKickingOff(chatId, plan.planId, false)
          }
        })
      )
    },
    [getChildChats, markPlanKickingOff, performKickOff]
  )

  const performKickOffReview = useCallback(
    async (implementationChildId: string) => {
      if (reviewKickOffStartedRef.current.has(implementationChildId)) {
        return
      }

      const implementationChild = getChatLocal(implementationChildId)
      if (!implementationChild || !isImplementationChildChat(implementationChild)) {
        return
      }

      const parentId = implementationChild.parentChatId
      if (!parentId) {
        return
      }

      const siblings = getChildChats(parentId)
      const planId = planIdFromPlanFilePath(implementationChild.planFilePath)
      if (planId && findReviewChildForPlan(planId, siblings)) {
        reviewKickOffStartedRef.current.add(implementationChildId)
        return
      }

      reviewKickOffStartedRef.current.add(implementationChildId)

      const response = await kickOffReview(implementationChildId)

      const reviewChild: ChatThread = {
        id: response.reviewChildChatId,
        title: planId
          ? `${planId
              .split('-')
              .map((word, index) =>
                index === 0 ? word.charAt(0).toUpperCase() + word.slice(1) : word
              )
              .join(' ')} review`
          : 'Review',
        preview: response.initialPrompt,
        updatedAt: new Date().toISOString(),
        agentId: 'cursor',
        projectId: implementationChild.projectId,
        workspaceId: implementationChild.workspaceId,
        workspacePath: implementationChild.workspacePath,
        mode: 'review',
        parentChatId: parentId,
        planFilePath: response.reviewFilePath,
        messages: []
      }

      queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) => [reviewChild, ...current])
      queryClient.setQueryData(chatKeys.detail(reviewChild.id), reviewChild)

      await sendMessage(reviewChild.id, response.initialPrompt)
    },
    [getChatLocal, getChildChats, queryClient, sendMessage]
  )

  const maybeKickOffReview = useCallback(
    (chatId: string) => {
      const chat = getChatLocal(chatId)
      if (!chat || !isImplementationChildChat(chat)) {
        return
      }

      const hasCompleteAssistant = chat.messages.some(
        (message) => message.role === 'assistant' && message.status === 'complete'
      )

      if (!hasCompleteAssistant || sendingChatIds.has(chatId)) {
        return
      }

      void performKickOffReview(chatId).catch(() => {
        reviewKickOffStartedRef.current.delete(chatId)
      })
    },
    [getChatLocal, performKickOffReview, sendingChatIds]
  )

  maybeKickOffReviewRef.current = maybeKickOffReview

  const value = useMemo<ChatContextValue>(
    () => ({
      chats,
      isLoadingChats: chatsQuery.isLoading,
      isPendingChats: chatsQuery.isPending,
      isFetchingChats: chatsQuery.isFetching,
      chatsError: chatsQuery.error as Error | null,
      refetchChats,
      searchQuery,
      setSearchQuery,
      getChat: getChatLocal,
      getChildChats,
      loadChat,
      createChat: (options: CreateChatOptions) => createChatMutation.mutateAsync(options),
      updateChatMode: updateChatModeAction,
      getModeUpdateError,
      closeChat: (chatId: string) => closeChatMutation.mutateAsync(chatId),
      sendMessage,
      kickOffPlan: kickOffPlanAction,
      kickOffAllPlans: kickOffAllPlansAction,
      isChatSending,
      isPlanKickingOff,
      isParentKickingOffAny,
      getMarkers: (chatId: string) => markersByChat[chatId] ?? [],
      subscribeAgentActivity
    }),
    [
      chats,
      chatsQuery.isLoading,
      chatsQuery.isPending,
      chatsQuery.isFetching,
      chatsQuery.error,
      refetchChats,
      searchQuery,
      getChatLocal,
      getChildChats,
      loadChat,
      createChatMutation.mutateAsync,
      updateChatModeAction,
      getModeUpdateError,
      closeChatMutation.mutateAsync,
      sendMessage,
      kickOffPlanAction,
      kickOffAllPlansAction,
      isChatSending,
      isPlanKickingOff,
      isParentKickingOffAny,
      markersByChat,
      subscribeAgentActivity
    ]
  )

  return <ChatContext.Provider value={value}>{children}</ChatContext.Provider>
}

export function useChat(): ChatContextValue {
  const context = useContext(ChatContext)

  if (!context) {
    throw new Error('useChat must be used within ChatProvider')
  }

  return context
}
