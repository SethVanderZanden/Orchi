import { useCallback, useMemo, useState } from 'react'

import { ArchiveChatDialog } from '@/components/chat/archive-chat-dialog'
import type { ChatThread } from '@/lib/chat/types'
import { isLocalChat } from '@/lib/chat/chat-persistence'
import { useChat } from '@/providers/chat-context'
import { useProjects } from '@/providers/project-provider'

import { ArchiveChatContext } from '@/providers/archive-chat-context'

export function ArchiveChatProvider({ children }: { children: React.ReactNode }): React.JSX.Element {
  const { archiveChat } = useChat()
  const { refetchProjects } = useProjects()
  const [pendingChat, setPendingChat] = useState<ChatThread | null>(null)
  const [archivingChatId, setArchivingChatId] = useState<string | null>(null)
  const [isConfirming, setIsConfirming] = useState(false)

  const requestArchive = useCallback(
    (chat: ChatThread) => {
      if (isLocalChat(chat.id)) {
        void archiveChat(chat.id)
        return
      }

      setPendingChat(chat)
    },
    [archiveChat]
  )

  const confirmArchive = useCallback(async () => {
    const chat = pendingChat
    if (!chat || isConfirming) {
      return
    }

    const chatId = chat.id
    setIsConfirming(true)
    setPendingChat(null)

    try {
      setArchivingChatId(chatId)
      await archiveChat(chatId)
      await refetchProjects()
    } finally {
      setArchivingChatId(null)
      setIsConfirming(false)
    }
  }, [archiveChat, isConfirming, pendingChat, refetchProjects])

  const isArchivingChat = useCallback(
    (chatId: string) => archivingChatId === chatId,
    [archivingChatId]
  )

  const value = useMemo(
    () => ({
      requestArchive,
      isArchivingChat
    }),
    [isArchivingChat, requestArchive]
  )

  return (
    <ArchiveChatContext.Provider value={value}>
      {children}
      <ArchiveChatDialog
        open={pendingChat !== null}
        chatTitle={pendingChat?.title ?? ''}
        onOpenChange={(open) => {
          if (!open && !isConfirming) {
            setPendingChat(null)
          }
        }}
        onConfirm={() => void confirmArchive()}
        isArchiving={isConfirming}
      />
    </ArchiveChatContext.Provider>
  )
}
