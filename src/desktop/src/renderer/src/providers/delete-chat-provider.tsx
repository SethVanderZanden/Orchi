import { createContext, useCallback, useContext, useMemo, useState } from 'react'

import { DeleteChatDialog } from '@/components/chat/delete-chat-dialog'
import type { ChatThread } from '@/lib/chat/types'
import { isLocalChat } from '@/lib/chat/chat-persistence'
import { useChat } from '@/providers/chat-provider'

export type DeleteChatContextValue = {
  requestDelete: (chat: ChatThread) => void
  isDeletingChat: (chatId: string) => boolean
}

export const DeleteChatContext = createContext<DeleteChatContextValue | null>(null)

export function useDeleteChatContext(): DeleteChatContextValue {
  const context = useContext(DeleteChatContext)

  if (!context) {
    throw new Error('useDeleteChat must be used within DeleteChatProvider')
  }

  return context
}

export function DeleteChatProvider({
  children
}: {
  children: React.ReactNode
}): React.JSX.Element {
  const { deleteChat } = useChat()
  const [pendingChat, setPendingChat] = useState<ChatThread | null>(null)
  const [deletingChatId, setDeletingChatId] = useState<string | null>(null)
  const [isConfirming, setIsConfirming] = useState(false)

  const requestDelete = useCallback(
    (chat: ChatThread) => {
      if (isLocalChat(chat.id)) {
        void deleteChat(chat.id)
        return
      }

      setPendingChat(chat)
    },
    [deleteChat]
  )

  const confirmDelete = useCallback(async () => {
    const chat = pendingChat
    if (!chat || isConfirming) {
      return
    }

    const chatId = chat.id
    setIsConfirming(true)
    setPendingChat(null)

    try {
      setDeletingChatId(chatId)
      await deleteChat(chatId)
    } finally {
      setDeletingChatId(null)
      setIsConfirming(false)
    }
  }, [deleteChat, isConfirming, pendingChat])

  const isDeletingChat = useCallback(
    (chatId: string) => deletingChatId === chatId,
    [deletingChatId]
  )

  const value = useMemo(
    () => ({
      requestDelete,
      isDeletingChat
    }),
    [isDeletingChat, requestDelete]
  )

  return (
    <DeleteChatContext.Provider value={value}>
      {children}
      <DeleteChatDialog
        open={pendingChat !== null}
        chatTitle={pendingChat?.title ?? ''}
        onOpenChange={(open) => {
          if (!open && !isConfirming) {
            setPendingChat(null)
          }
        }}
        onConfirm={() => void confirmDelete()}
        isDeleting={isConfirming}
      />
    </DeleteChatContext.Provider>
  )
}
