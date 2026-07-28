import { useCallback, useMemo, useState } from 'react'

import { DeleteChatDialog } from '@/components/chat/delete-chat-dialog'
import type { ChatThread } from '@/lib/chat/types'
import { isLocalChat } from '@/lib/chat/chat-persistence'
import { useChat } from '@/providers/chat-context'

import { DeleteChatContext } from '@/providers/delete-chat-context'

type PendingDelete =
  | { kind: 'single'; chat: ChatThread }
  | { kind: 'bulk'; chats: ChatThread[]; skippedSendingCount: number }

export function DeleteChatProvider({ children }: { children: React.ReactNode }): React.JSX.Element {
  const { deleteChat } = useChat()
  const [pendingDelete, setPendingDelete] = useState<PendingDelete | null>(null)
  const [deletingChatIds, setDeletingChatIds] = useState<Set<string>>(() => new Set())
  const [isConfirming, setIsConfirming] = useState(false)

  const requestDelete = useCallback(
    (chat: ChatThread) => {
      if (isLocalChat(chat.id)) {
        void deleteChat(chat.id)
        return
      }

      setPendingDelete({ kind: 'single', chat })
    },
    [deleteChat]
  )

  const requestDeleteMany = useCallback(
    (chats: ChatThread[], skippedSendingCount = 0) => {
      if (chats.length === 0) {
        return
      }

      if (chats.length === 1 && skippedSendingCount === 0) {
        requestDelete(chats[0]!)
        return
      }

      const localChats = chats.filter((chat) => isLocalChat(chat.id))
      const persistedChats = chats.filter((chat) => !isLocalChat(chat.id))

      for (const chat of localChats) {
        void deleteChat(chat.id)
      }

      if (persistedChats.length === 0) {
        return
      }

      if (persistedChats.length === 1 && skippedSendingCount === 0) {
        setPendingDelete({ kind: 'single', chat: persistedChats[0]! })
        return
      }

      setPendingDelete({
        kind: 'bulk',
        chats: persistedChats,
        skippedSendingCount
      })
    },
    [deleteChat, requestDelete]
  )

  const confirmDelete = useCallback(async () => {
    if (!pendingDelete || isConfirming) {
      return
    }

    const chatsToDelete =
      pendingDelete.kind === 'single' ? [pendingDelete.chat] : pendingDelete.chats

    setIsConfirming(true)
    setPendingDelete(null)

    try {
      for (const chat of chatsToDelete) {
        setDeletingChatIds((current) => new Set(current).add(chat.id))

        try {
          await deleteChat(chat.id)
        } finally {
          setDeletingChatIds((current) => {
            const next = new Set(current)
            next.delete(chat.id)
            return next
          })
        }
      }
    } finally {
      setIsConfirming(false)
    }
  }, [deleteChat, isConfirming, pendingDelete])

  const isDeletingChat = useCallback(
    (chatId: string) => deletingChatIds.has(chatId),
    [deletingChatIds]
  )

  const isDeleteInProgress = isConfirming || deletingChatIds.size > 0

  const value = useMemo(
    () => ({
      requestDelete,
      requestDeleteMany,
      isDeletingChat,
      isDeleteInProgress
    }),
    [isDeleteInProgress, isDeletingChat, requestDelete, requestDeleteMany]
  )

  const dialogChatCount =
    pendingDelete?.kind === 'bulk' ? pendingDelete.chats.length : pendingDelete ? 1 : 0

  return (
    <DeleteChatContext.Provider value={value}>
      {children}
      <DeleteChatDialog
        open={pendingDelete !== null}
        chatCount={dialogChatCount}
        chatTitle={pendingDelete?.kind === 'single' ? pendingDelete.chat.title : undefined}
        skippedSendingCount={
          pendingDelete?.kind === 'bulk' ? pendingDelete.skippedSendingCount : undefined
        }
        onOpenChange={(open) => {
          if (!open && !isConfirming) {
            setPendingDelete(null)
          }
        }}
        onConfirm={() => void confirmDelete()}
        isDeleting={isConfirming}
      />
    </DeleteChatContext.Provider>
  )
}
