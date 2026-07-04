import { useCallback, useState } from 'react'

import type { ChatThread } from '@/lib/chat/types'
import { isLocalChat } from '@/lib/chat/chat-persistence'
import { useChat } from '@/providers/chat-provider'

export function useDeleteChat() {
  const { deleteChat } = useChat()
  const [pendingChat, setPendingChat] = useState<ChatThread | null>(null)
  const [isDeleting, setIsDeleting] = useState(false)

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
    if (!pendingChat) {
      return
    }

    setIsDeleting(true)
    try {
      await deleteChat(pendingChat.id)
      setPendingChat(null)
    } finally {
      setIsDeleting(false)
    }
  }, [deleteChat, pendingChat])

  return {
    requestDelete,
    isDeleting,
    dialogProps: {
      open: pendingChat !== null,
      chatTitle: pendingChat?.title ?? '',
      onOpenChange: (open: boolean) => {
        if (!open && !isDeleting) {
          setPendingChat(null)
        }
      },
      onConfirm: () => void confirmDelete(),
      isDeleting
    }
  }
}
