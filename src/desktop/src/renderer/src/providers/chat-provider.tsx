import { createContext, useCallback, useContext, useMemo, useState } from 'react'
import { useNavigate } from '@tanstack/react-router'

import {
  appendAssistantMessage,
  appendUserMessage,
  createEmptyChat
} from '@/lib/chat/chat-utils'
import { MOCK_CHATS } from '@/lib/chat/mock-chats'
import type { ChatThread } from '@/lib/chat/types'

type ChatContextValue = {
  chats: ChatThread[]
  searchQuery: string
  setSearchQuery: (query: string) => void
  getChat: (chatId: string) => ChatThread | undefined
  createChat: () => ChatThread
  sendMessage: (chatId: string, content: string) => void
  isSending: boolean
}

const ChatContext = createContext<ChatContextValue | null>(null)

export function ChatProvider({ children }: { children: React.ReactNode }): React.JSX.Element {
  const navigate = useNavigate()
  const [chats, setChats] = useState<ChatThread[]>(MOCK_CHATS)
  const [searchQuery, setSearchQuery] = useState('')
  const [isSending, setIsSending] = useState(false)

  const getChat = useCallback(
    (chatId: string) => chats.find((chat) => chat.id === chatId),
    [chats]
  )

  const createChat = useCallback(() => {
    const chat = createEmptyChat()
    setChats((current) => [chat, ...current])
    setSearchQuery('')
    navigate({ to: '/chat/$chatId', params: { chatId: chat.id } })
    return chat
  }, [navigate])

  const sendMessage = useCallback((chatId: string, content: string) => {
    setChats((current) =>
      current.map((chat) => (chat.id === chatId ? appendUserMessage(chat, content) : chat))
    )

    setIsSending(true)

    window.setTimeout(() => {
      setChats((current) =>
        current.map((chat) =>
          chat.id === chatId
            ? appendAssistantMessage(
                chat,
                'This is a placeholder response. Wire Orchi agents here when the backend is ready.'
              )
            : chat
        )
      )
      setIsSending(false)
    }, 700)
  }, [])

  const value = useMemo<ChatContextValue>(
    () => ({
      chats,
      searchQuery,
      setSearchQuery,
      getChat,
      createChat,
      sendMessage,
      isSending
    }),
    [chats, searchQuery, getChat, createChat, sendMessage, isSending]
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
