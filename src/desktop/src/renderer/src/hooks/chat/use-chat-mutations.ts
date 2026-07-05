import { useCallback, useRef, useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { NavigateOptions } from '@tanstack/react-router'

import {
  closeChat,
  createChat,
  updateChatMode,
  updateChatModel
} from '@/lib/chat/api'
import { isLocalChat } from '@/lib/chat/chat-persistence'
import type { AgentMode, ChatThread, CreateChatOptions } from '@/lib/chat/types'
import { chatKeys } from '@/lib/query-keys'

type UseChatMutationsOptions = {
  purgeFromQueryClient: (chatId: string) => void
  purgeStreamState: (chatId: string) => void
  purgeKickoffState: (chatId: string) => void
  navigateAwayIfDeleted: (chatId: string) => void
  refetchChats: () => Promise<unknown>
  setSearchQuery: (query: string) => void
  navigate: (options: NavigateOptions) => void
}

export function useChatMutations({
  purgeFromQueryClient,
  purgeStreamState,
  purgeKickoffState,
  navigateAwayIfDeleted,
  refetchChats,
  setSearchQuery,
  navigate
}: UseChatMutationsOptions) {
  const queryClient = useQueryClient()
  const [modeUpdateErrorByChat, setModeUpdateErrorByChat] = useState<Record<string, string>>({})
  const [modelUpdateErrorByChat, setModelUpdateErrorByChat] = useState<Record<string, string>>({})
  const modeUpdateGenerationByChatRef = useRef<Map<string, number>>(new Map())

  const purgeMutationState = useCallback((chatId: string) => {
    setModeUpdateErrorByChat((current) => {
      if (!(chatId in current)) {
        return current
      }

      const next = { ...current }
      delete next[chatId]
      return next
    })

    setModelUpdateErrorByChat((current) => {
      if (!(chatId in current)) {
        return current
      }

      const next = { ...current }
      delete next[chatId]
      return next
    })
  }, [])

  const purgeChatFromClient = useCallback(
    (chatId: string) => {
      purgeStreamState(chatId)
      purgeMutationState(chatId)
      purgeKickoffState(chatId)
      purgeFromQueryClient(chatId)
    },
    [purgeFromQueryClient, purgeKickoffState, purgeMutationState, purgeStreamState]
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
      purgeChatFromClient(chatId)
    }
  })

  const deleteChat = useCallback(
    async (chatId: string) => {
      if (isLocalChat(chatId)) {
        purgeChatFromClient(chatId)
        navigateAwayIfDeleted(chatId)
        return
      }

      purgeChatFromClient(chatId)
      navigateAwayIfDeleted(chatId)

      try {
        await closeChat(chatId)
      } catch (error) {
        await refetchChats()
        throw error
      }
    },
    [navigateAwayIfDeleted, purgeChatFromClient, refetchChats]
  )

  const updateChatModeAction = useCallback(
    async (chatId: string, mode: AgentMode) => {
      const currentChat =
        queryClient.getQueryData<ChatThread>(chatKeys.detail(chatId)) ??
        queryClient.getQueryData<ChatThread[]>(chatKeys.lists())?.find((chat) => chat.id === chatId)

      if (currentChat?.mode.toLowerCase() === mode.toLowerCase()) {
        return
      }

      const previousMode = currentChat?.mode
      const generation = (modeUpdateGenerationByChatRef.current.get(chatId) ?? 0) + 1
      modeUpdateGenerationByChatRef.current.set(chatId, generation)

      queryClient.setQueryData<ChatThread>(chatKeys.detail(chatId), (current) =>
        current ? { ...current, mode } : current
      )

      queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) =>
        current.map((chat) => (chat.id === chatId ? { ...chat, mode } : chat))
      )

      setModeUpdateErrorByChat((current) => {
        if (!(chatId in current)) {
          return current
        }

        const next = { ...current }
        delete next[chatId]
        return next
      })

      try {
        const response = await updateChatMode(chatId, { mode })

        if (modeUpdateGenerationByChatRef.current.get(chatId) !== generation) {
          return
        }

        queryClient.setQueryData<ChatThread>(chatKeys.detail(chatId), (current) =>
          current ? { ...current, mode: response.mode } : current
        )

        queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) =>
          current.map((chat) => (chat.id === chatId ? { ...chat, mode: response.mode } : chat))
        )
      } catch (error) {
        if (modeUpdateGenerationByChatRef.current.get(chatId) !== generation) {
          return
        }

        if (previousMode !== undefined) {
          queryClient.setQueryData<ChatThread>(chatKeys.detail(chatId), (current) =>
            current ? { ...current, mode: previousMode } : current
          )

          queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) =>
            current.map((chat) =>
              chat.id === chatId ? { ...chat, mode: previousMode } : chat
            )
          )
        }

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

  const updateChatModelAction = useCallback(
    async (chatId: string, modelId: string | null) => {
      try {
        const response = await updateChatModel(chatId, { modelId })

        queryClient.setQueryData<ChatThread>(chatKeys.detail(chatId), (current) =>
          current ? { ...current, modelId: response.modelId ?? null } : current
        )

        queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) =>
          current.map((chat) =>
            chat.id === chatId ? { ...chat, modelId: response.modelId ?? null } : chat
          )
        )

        setModelUpdateErrorByChat((current) => {
          if (!(chatId in current)) {
            return current
          }

          const next = { ...current }
          delete next[chatId]
          return next
        })
      } catch (error) {
        const message = error instanceof Error ? error.message : 'Failed to update chat model.'
        setModelUpdateErrorByChat((current) => ({ ...current, [chatId]: message }))
      }
    },
    [queryClient]
  )

  const getModelUpdateError = useCallback(
    (chatId: string) => modelUpdateErrorByChat[chatId],
    [modelUpdateErrorByChat]
  )

  return {
    createChat: (options: CreateChatOptions) => createChatMutation.mutateAsync(options),
    closeChat: (chatId: string) => closeChatMutation.mutateAsync(chatId),
    deleteChat,
    updateChatMode: updateChatModeAction,
    getModeUpdateError,
    updateChatModel: updateChatModelAction,
    getModelUpdateError
  }
}
