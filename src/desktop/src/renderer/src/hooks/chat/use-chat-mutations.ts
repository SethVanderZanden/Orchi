import { useCallback, useEffect, useRef, useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { NavigateOptions } from '@tanstack/react-router'

import { closeChat, updateChatMode, updateChatModel } from '@/lib/chat/api'
import { createLocalDraftChat } from '@/lib/chat/create-local-draft'
import { isLocalChat } from '@/lib/chat/chat-persistence'
import { registerChatIdMigrator } from '@/lib/chat/migrate-chat-client-state'
import type { AgentMode, ChatThread, CreateChatOptions } from '@/lib/chat/types'
import { getDefaultWorkspace } from '@/lib/projects/group-chats'
import type { Project } from '@/lib/projects/types'
import { chatKeys, projectKeys } from '@/lib/query-keys'

type UseChatMutationsOptions = {
  purgeFromQueryClient: (chatId: string) => void
  purgeStreamState: (chatId: string) => void
  purgeKickoffState: (chatId: string) => void
  navigateAwayIfDeleted: (chatId: string) => void
  refetchChats: () => Promise<unknown>
  setSearchQuery: (query: string) => void
  navigate: (options: NavigateOptions) => void
}

type UseChatMutationsResult = {
  createChat: (options: CreateChatOptions) => Promise<ChatThread>
  closeChat: (chatId: string) => Promise<void>
  deleteChat: (chatId: string) => Promise<void>
  updateChatMode: (chatId: string, mode: AgentMode) => Promise<void>
  getModeUpdateError: (chatId: string) => string | undefined
  updateChatModel: (chatId: string, modelId: string | null) => Promise<void>
  getModelUpdateError: (chatId: string) => string | undefined
  updateChatProject: (chatId: string, projectId: string) => void
}

export function useChatMutations({
  purgeFromQueryClient,
  purgeStreamState,
  purgeKickoffState,
  navigateAwayIfDeleted,
  refetchChats,
  setSearchQuery,
  navigate
}: UseChatMutationsOptions): UseChatMutationsResult {
  const queryClient = useQueryClient()
  const [modeUpdateErrorByChat, setModeUpdateErrorByChat] = useState<Record<string, string>>({})
  const [modelUpdateErrorByChat, setModelUpdateErrorByChat] = useState<Record<string, string>>({})
  const modeUpdateGenerationByChatRef = useRef<Map<string, number>>(new Map())

  useEffect(() => {
    return registerChatIdMigrator((fromId, toId) => {
      setModeUpdateErrorByChat((current) => {
        if (!(fromId in current)) {
          return current
        }

        const next = { ...current, [toId]: current[fromId] }
        delete next[fromId]
        return next
      })

      setModelUpdateErrorByChat((current) => {
        if (!(fromId in current)) {
          return current
        }

        const next = { ...current, [toId]: current[fromId] }
        delete next[fromId]
        return next
      })

      const modeGeneration = modeUpdateGenerationByChatRef.current.get(fromId)
      if (modeGeneration !== undefined) {
        modeUpdateGenerationByChatRef.current.delete(fromId)
        modeUpdateGenerationByChatRef.current.set(toId, modeGeneration)
      }
    })
  }, [])

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
    mutationFn: (options: CreateChatOptions) => {
      const chat = createLocalDraftChat({
        workspaceId: options.workspaceId,
        workspacePath: options.workspacePath,
        projectId: options.projectId ?? null
      })
      return Promise.resolve(chat)
    },
    onSuccess: (chat) => {
      queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) => [chat, ...current])
      queryClient.setQueryData(chatKeys.detail(chat.id), chat)
      setSearchQuery('')
      navigate({ to: '/chat/$chatId', params: { chatId: chat.id } })
    }
  })

  const closeChatMutation = useMutation({
    mutationFn: async (chatId: string) => {
      if (isLocalChat(chatId)) {
        return
      }

      await closeChat(chatId)
    },
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

  const updateChatCacheMode = useCallback(
    (chatId: string, mode: AgentMode) => {
      queryClient.setQueryData<ChatThread>(chatKeys.detail(chatId), (current) =>
        current ? { ...current, mode } : current
      )

      queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) =>
        current.map((chat) => (chat.id === chatId ? { ...chat, mode } : chat))
      )
    },
    [queryClient]
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

      updateChatCacheMode(chatId, mode)

      setModeUpdateErrorByChat((current) => {
        if (!(chatId in current)) {
          return current
        }

        const next = { ...current }
        delete next[chatId]
        return next
      })

      if (isLocalChat(chatId)) {
        return
      }

      try {
        const response = await updateChatMode(chatId, { mode })

        if (modeUpdateGenerationByChatRef.current.get(chatId) !== generation) {
          return
        }

        updateChatCacheMode(chatId, response.mode)
      } catch (error) {
        if (modeUpdateGenerationByChatRef.current.get(chatId) !== generation) {
          return
        }

        if (previousMode !== undefined) {
          updateChatCacheMode(chatId, previousMode)
        }

        const message = error instanceof Error ? error.message : 'Failed to update chat mode.'
        setModeUpdateErrorByChat((current) => ({ ...current, [chatId]: message }))
      }
    },
    [queryClient, updateChatCacheMode]
  )

  const getModeUpdateError = useCallback(
    (chatId: string) => modeUpdateErrorByChat[chatId],
    [modeUpdateErrorByChat]
  )

  const updateChatModelAction = useCallback(
    async (chatId: string, modelId: string | null) => {
      queryClient.setQueryData<ChatThread>(chatKeys.detail(chatId), (current) =>
        current ? { ...current, modelId } : current
      )

      queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) =>
        current.map((chat) => (chat.id === chatId ? { ...chat, modelId } : chat))
      )

      setModelUpdateErrorByChat((current) => {
        if (!(chatId in current)) {
          return current
        }

        const next = { ...current }
        delete next[chatId]
        return next
      })

      if (isLocalChat(chatId)) {
        return
      }

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

  const updateChatProjectAction = useCallback(
    (chatId: string, projectId: string) => {
      if (!isLocalChat(chatId)) {
        return
      }

      const currentChat =
        queryClient.getQueryData<ChatThread>(chatKeys.detail(chatId)) ??
        queryClient.getQueryData<ChatThread[]>(chatKeys.lists())?.find((chat) => chat.id === chatId)

      if (currentChat?.projectId === projectId) {
        return
      }

      const projects = queryClient.getQueryData<Project[]>(projectKeys.lists()) ?? []
      const project = projects.find((entry) => entry.id === projectId)
      const workspace = project ? getDefaultWorkspace(project) : undefined

      if (!workspace) {
        return
      }

      const applyProject = (chat: ChatThread): ChatThread => ({
        ...chat,
        projectId,
        workspaceId: workspace.id,
        workspacePath: workspace.path
      })

      queryClient.setQueryData<ChatThread>(chatKeys.detail(chatId), (current) =>
        current ? applyProject(current) : current
      )

      queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) =>
        current.map((chat) => (chat.id === chatId ? applyProject(chat) : chat))
      )
    },
    [queryClient]
  )

  return {
    createChat: (options: CreateChatOptions) => createChatMutation.mutateAsync(options),
    closeChat: (chatId: string) => closeChatMutation.mutateAsync(chatId),
    deleteChat,
    updateChatMode: updateChatModeAction,
    getModeUpdateError,
    updateChatModel: updateChatModelAction,
    getModelUpdateError,
    updateChatProject: updateChatProjectAction
  }
}
