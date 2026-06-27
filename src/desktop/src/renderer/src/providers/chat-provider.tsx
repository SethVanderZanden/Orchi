import { createContext, useCallback, useContext, useMemo, useRef, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from '@tanstack/react-router'

import { closeChat, createChat, getChat, listChats, sendMessageStream } from '@/lib/chat/api'
import { formatToolMarker } from '@/lib/chat/format-tool-marker'
import { normalizeAgentText } from '@/lib/chat/normalize-agent-text'
import type { ChatMarker, ChatMessage, ChatThread } from '@/lib/chat/types'
import { chatKeys } from '@/lib/query-keys'

type ChatContextValue = {
  chats: ChatThread[]
  isLoadingChats: boolean
  chatsError: Error | null
  searchQuery: string
  setSearchQuery: (query: string) => void
  getChat: (chatId: string) => ChatThread | undefined
  loadChat: (chatId: string) => Promise<ChatThread | undefined>
  createChat: (workspacePath: string) => Promise<ChatThread>
  closeChat: (chatId: string) => Promise<void>
  sendMessage: (chatId: string, content: string) => Promise<void>
  isSending: boolean
  getMarkers: (chatId: string) => ChatMarker[]
}

const ChatContext = createContext<ChatContextValue | null>(null)

export function ChatProvider({ children }: { children: React.ReactNode }): React.JSX.Element {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [searchQuery, setSearchQuery] = useState('')
  const [isSending, setIsSending] = useState(false)
  const [markersByChat, setMarkersByChat] = useState<Record<string, ChatMarker[]>>({})
  const streamAbortRef = useRef<AbortController | null>(null)

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

      const normalizedDetail = {
        ...detail,
        messages: detail.messages.map((message) =>
          message.role === 'assistant'
            ? { ...message, content: normalizeAgentText(message.content) }
            : message
        )
      }

      queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) => {
        const others = current.filter((chat) => chat.id !== chatId)
        return [normalizedDetail, ...others]
      })

      queryClient.setQueryData(chatKeys.detail(chatId), normalizedDetail)

      return normalizedDetail
    },
    [queryClient]
  )

  const createChatMutation = useMutation({
    mutationFn: (workspacePath: string) => createChat({ agent: 'cursor', workspacePath }),
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
                content: message.content + normalizeAgentText(text),
                status: 'streaming'
              }))
            },
            onTool: (name, status, detail) => {
              appendMarker(chatId, {
                id: crypto.randomUUID(),
                content: formatToolMarker(name, status, detail),
                variant: 'tool'
              })
            },
            onDone: () => {
              updateAssistantMessage(chatId, (message) => ({
                ...message,
                content: normalizeAgentText(message.content),
                status: 'complete'
              }))
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
    [appendMarker, clearMarkers, loadChat, queryClient, updateAssistantMessage]
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
      createChat: (workspacePath: string) => createChatMutation.mutateAsync(workspacePath),
      closeChat: (chatId: string) => closeChatMutation.mutateAsync(chatId),
      sendMessage,
      isSending,
      getMarkers: (chatId: string) => markersByChat[chatId] ?? []
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
      isSending,
      markersByChat
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
