import { useCallback, useMemo, useState } from 'react'

import { DeleteChatDialog } from '@/components/chat/delete-chat-dialog'
import type { ChatThread } from '@/lib/chat/types'
import { isLocalChat } from '@/lib/chat/chat-persistence'
import { useChat } from '@/providers/chat-context'

import { DeleteChatContext } from '@/providers/delete-chat-context'

type PendingDelete = {
  chats: ChatThread[]
  scopeLabel?: string
}

export function DeleteChatProvider({ children }: { children: React.ReactNode }): React.JSX.Element {
  const { deleteChat, deleteChats } = useChat()
  const [pending, setPending] = useState<PendingDelete | null>(null)
  const [deletingChatIds, setDeletingChatIds] = useState<Set<string>>(() => new Set())
  const [isConfirming, setIsConfirming] = useState(false)

  const requestDelete = useCallback(
    (chat: ChatThread) => {
      if (isLocalChat(chat.id)) {
        void deleteChat(chat.id)
        return
      }

      setPending({ chats: [chat] })
    },
    [deleteChat]
  )

  const requestDeleteMany = useCallback(
    (chats: ChatThread[], scopeLabel?: string) => {
      const unique = new Map<string, ChatThread>()
      for (const chat of chats) {
        unique.set(chat.id, chat)
      }

      const nextChats = [...unique.values()]
      if (nextChats.length === 0) {
        return
      }

      if (nextChats.length === 1 && isLocalChat(nextChats[0].id)) {
        void deleteChat(nextChats[0].id)
        return
      }

      const onlyLocal = nextChats.every((chat) => isLocalChat(chat.id))
      if (onlyLocal) {
        void deleteChats(nextChats.map((chat) => chat.id))
        return
      }

      setPending({ chats: nextChats, scopeLabel })
    },
    [deleteChat, deleteChats]
  )

  const confirmDelete = useCallback(async () => {
    const current = pending
    if (!current || isConfirming) {
      return
    }

    const chatIds = current.chats.map((chat) => chat.id)
    setIsConfirming(true)
    setPending(null)

    try {
      setDeletingChatIds(new Set(chatIds))
      if (chatIds.length === 1) {
        await deleteChat(chatIds[0])
      } else {
        await deleteChats(chatIds)
      }
    } finally {
      setDeletingChatIds(new Set())
      setIsConfirming(false)
    }
  }, [deleteChat, deleteChats, isConfirming, pending])

  const isDeletingChat = useCallback(
    (chatId: string) => deletingChatIds.has(chatId),
    [deletingChatIds]
  )

  const value = useMemo(
    () => ({
      requestDelete,
      requestDeleteMany,
      isDeletingChat
    }),
    [isDeletingChat, requestDelete, requestDeleteMany]
  )

  const pendingCount = pending?.chats.length ?? 0

  return (
    <DeleteChatContext.Provider value={value}>
      {children}
      <DeleteChatDialog
        open={pending !== null}
        chatCount={pendingCount}
        chatTitle={pending?.chats[0]?.title ?? ''}
        scopeLabel={pending?.scopeLabel}
        onOpenChange={(open) => {
          if (!open && !isConfirming) {
            setPending(null)
          }
        }}
        onConfirm={() => void confirmDelete()}
        isDeleting={isConfirming}
      />
    </DeleteChatContext.Provider>
  )
}
