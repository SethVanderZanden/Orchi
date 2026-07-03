import { createContext, useCallback, useContext, useMemo, useRef, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from '@tanstack/react-router'

import { closeChat, createChat, getChat, kickOffPlan, listChats, sendMessageStream } from '@/lib/chat/api'
import type { NewChatOptions } from '@/components/chat/new-chat-dialog'
import type { ChatMarker, ChatMessage, ChatThread } from '@/lib/chat/types'
import type { ParsedPlan } from '@/lib/orchestration/parse-plans'
import { chatKeys } from '@/lib/query-keys'

type AgentActivityDetail = {
  phase: 'tool' | 'done'
  label?: string
}

type ChatContextValue = {
  chats: ChatThread[]
  isLoadingChats: boolean
  chatsError: Error | null
  searchQuery: string
  setSearchQuery: (query: string) => void
  getChat: (chatId: string) => ChatThread | undefined
  loadChat: (chatId: string) => Promise<ChatThread | undefined>
  createChat: (options: NewChatOptions) => Promise<ChatThread>
  closeChat: (chatId: string) => Promise<void>
  sendMessage: (chatId: string, content: string) => Promise<void>
  kickOffPlan: (chatId: string, plan: ParsedPlan) => Promise<void>
  isSending: boolean
  isKickingOff: boolean
  kickingOffPlanId: string | null
  getMarkers: (chatId: string) => ChatMarker[]
  subscribeAgentActivity: (listener: (detail: AgentActivityDetail) => void) => () => void
}

const ChatContext = createContext<ChatContextValue | null>(null)

export function ChatProvider({ children }: { children: React.ReactNode }): React.JSX.Element {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [searchQuery, setSearchQuery] = useState('')
  const [isSending, setIsSending] = useState(false)
  const [isKickingOff, setIsKickingOff] = useState(false)
  const [kickingOffPlanId, setKickingOffPlanId] = useState<string | null>(null)
  const [markersByChat, setMarkersByChat] = useState<Record<string, ChatMarker[]>>({})
  const streamAbortRef = useRef<AbortController | null>(null)
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
    queryFn: listChats
  })

  const chats = chatsQuery.data ?? []

  const getChatLocal = useCallback(
    (chatId: string) => {
      const cachedDetail = queryClient.getQueryData<ChatThread>(chatKeys.detail(chatId))
      return cachedDetail ?? chats.find((chat) => chat.id === chatId)
    },
    [chats, queryClient]
  )

  const loadChat = useCallback(
    async (chatId: string) => {
      const detail = await queryClient.fetchQuery({
        queryKey: chatKeys.detail(chatId),
        queryFn: () => getChat(chatId)
      })

      queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) => {
        const others = current.filter((chat) => chat.id !== chatId)
        return [detail, ...others]
      })

      queryClient.setQueryData(chatKeys.detail(chatId), detail)

      return detail
    },
    [queryClient]
  )

  const createChatMutation = useMutation({
    mutationFn: (options: NewChatOptions) =>
      createChat({
        agent: 'cursor',
        workspacePath: options.workspacePath,
        mode: options.mode
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

  const updateAssistantMessage = useCallback(
    (chatId: string, updater: (message: ChatMessage) => ChatMessage) => {
      queryClient.setQueryData<ChatThread>(chatKeys.detail(chatId), (current) => {
        if (!current) {
          return current
        }

        const messages = [...current.messages]
        const assistantIndex = messages.findLastIndex((message) => message.role === 'assistant')
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

  const sendMessage = useCallback(
    async (chatId: string, content: string) => {
      streamAbortRef.current?.abort()
      const controller = new AbortController()
      streamAbortRef.current = controller

      setIsSending(true)
      clearMarkers(chatId)

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
          id: crypto.randomUUID(),
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
              updateAssistantMessage(chatId, (message) => ({
                ...message,
                content: message.content + text,
                status: 'streaming'
              }))
            },
            onTool: (label) => {
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
              updateAssistantMessage(chatId, (message) => ({
                ...message,
                status: 'complete'
              }))
              notifyAgentActivity({ phase: 'done' })
              clearMarkers(chatId)
            },
            onError: (code, message) => {
              updateAssistantMessage(chatId, (currentMessage) => ({
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

        await queryClient.invalidateQueries({ queryKey: chatKeys.lists() })
        await loadChat(chatId)
      } finally {
        setIsSending(false)
      }
    },
    [appendMarker, clearMarkers, loadChat, notifyAgentActivity, queryClient, updateAssistantMessage]
  )

  const kickOffPlanAction = useCallback(
    async (chatId: string, plan: ParsedPlan) => {
      setIsKickingOff(true)
      setKickingOffPlanId(plan.planId)

      try {
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
          workspacePath: getChatLocal(chatId)?.workspacePath ?? '',
          mode: 'default',
          parentChatId: chatId,
          planFilePath: response.planFilePath,
          messages: []
        }

        queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) => [childChat, ...current])
        queryClient.setQueryData(chatKeys.detail(childChat.id), childChat)
        navigate({ to: '/chat/$chatId', params: { chatId: childChat.id } })

        await sendMessage(childChat.id, response.initialPrompt)
      } finally {
        setIsKickingOff(false)
        setKickingOffPlanId(null)
      }
    },
    [getChatLocal, navigate, queryClient, sendMessage]
  )

  const value = useMemo<ChatContextValue>(
    () => ({
      chats,
      isLoadingChats: chatsQuery.isLoading,
      chatsError: chatsQuery.error as Error | null,
      searchQuery,
      setSearchQuery,
      getChat: getChatLocal,
      loadChat,
      createChat: (options: NewChatOptions) => createChatMutation.mutateAsync(options),
      closeChat: (chatId: string) => closeChatMutation.mutateAsync(chatId),
      sendMessage,
      kickOffPlan: kickOffPlanAction,
      isSending,
      isKickingOff,
      kickingOffPlanId,
      getMarkers: (chatId: string) => markersByChat[chatId] ?? [],
      subscribeAgentActivity
    }),
    [
      chats,
      chatsQuery.isLoading,
      chatsQuery.error,
      searchQuery,
      getChatLocal,
      loadChat,
      createChatMutation.mutateAsync,
      closeChatMutation.mutateAsync,
      sendMessage,
      kickOffPlanAction,
      isSending,
      isKickingOff,
      kickingOffPlanId,
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
