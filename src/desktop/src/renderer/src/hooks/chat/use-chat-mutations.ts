import { useCallback, useEffect, useRef, useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { NavigateOptions } from '@tanstack/react-router'

import {
  closeChat,
  updateChatApprovalPolicy,
  updateChatContextSize,
  updateChatMode,
  updateChatModel,
  updateChatReasoningEffort
} from '@/lib/chat/api'
import { createLocalDraftChat } from '@/lib/chat/create-local-draft'
import { isLocalChat } from '@/lib/chat/chat-persistence'
import { registerChatIdMigrator } from '@/lib/chat/migrate-chat-client-state'
import {
  listModeRuntimeDefaults,
  resolveModeRuntimeDefault
} from '@/lib/chat/mode-runtime-defaults-api'
import type { AgentMode, ChatThread, CreateChatOptions, ModeRuntimeDefault } from '@/lib/chat/types'
import { getDefaultWorkspace } from '@/lib/projects/group-chats'
import type { Project } from '@/lib/projects/types'
import { agentKeys, chatKeys, projectKeys } from '@/lib/query-keys'

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
  updateChatContextSize: (chatId: string, contextSizeId: string | null) => Promise<void>
  getContextSizeUpdateError: (chatId: string) => string | undefined
  updateChatReasoningEffort: (chatId: string, reasoningEffortId: string | null) => Promise<void>
  getReasoningEffortUpdateError: (chatId: string) => string | undefined
  updateChatApprovalPolicy: (chatId: string, approvalPolicyId: string | null) => Promise<void>
  getApprovalPolicyUpdateError: (chatId: string) => string | undefined
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
  const [contextSizeUpdateErrorByChat, setContextSizeUpdateErrorByChat] = useState<
    Record<string, string>
  >({})
  const [reasoningEffortUpdateErrorByChat, setReasoningEffortUpdateErrorByChat] = useState<
    Record<string, string>
  >({})
  const [approvalPolicyUpdateErrorByChat, setApprovalPolicyUpdateErrorByChat] = useState<
    Record<string, string>
  >({})
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

      setContextSizeUpdateErrorByChat((current) => {
        if (!(fromId in current)) {
          return current
        }

        const next = { ...current, [toId]: current[fromId] }
        delete next[fromId]
        return next
      })

      setReasoningEffortUpdateErrorByChat((current) => {
        if (!(fromId in current)) {
          return current
        }

        const next = { ...current, [toId]: current[fromId] }
        delete next[fromId]
        return next
      })

      setApprovalPolicyUpdateErrorByChat((current) => {
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

    setContextSizeUpdateErrorByChat((current) => {
      if (!(chatId in current)) {
        return current
      }

      const next = { ...current }
      delete next[chatId]
      return next
    })

    setReasoningEffortUpdateErrorByChat((current) => {
      if (!(chatId in current)) {
        return current
      }

      const next = { ...current }
      delete next[chatId]
      return next
    })

    setApprovalPolicyUpdateErrorByChat((current) => {
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
    mutationFn: async (options: CreateChatOptions) => {
      const defaultsResponse =
        queryClient.getQueryData<{ defaults: ModeRuntimeDefault[] }>(agentKeys.modeDefaults()) ??
        (await queryClient.fetchQuery({
          queryKey: agentKeys.modeDefaults(),
          queryFn: listModeRuntimeDefaults,
          staleTime: 60 * 60 * 1000
        }))

      const draft = createLocalDraftChat({
        workspaceId: options.workspaceId,
        workspacePath: options.workspacePath,
        projectId: options.projectId ?? null
      })

      const modeDefault = resolveModeRuntimeDefault(defaultsResponse?.defaults ?? [], draft.mode)
      if (!modeDefault) {
        return draft
      }

      return {
        ...draft,
        agentId: modeDefault.agentId,
        modelId: modeDefault.modelId,
        contextSizeId: modeDefault.contextSizeId,
        reasoningEffortId: modeDefault.reasoningEffortId,
        approvalPolicyId: modeDefault.approvalPolicyId
      }
    },
    onSuccess: (chat, variables) => {
      queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) => [chat, ...current])
      queryClient.setQueryData(chatKeys.detail(chat.id), chat)
      setSearchQuery('')
      if (variables.navigate === false) {
        return
      }

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

  const updateChatCacheRuntime = useCallback(
    (
      chatId: string,
      patch: Partial<
        Pick<
          ChatThread,
          | 'mode'
          | 'agentId'
          | 'modelId'
          | 'contextSizeId'
          | 'reasoningEffortId'
          | 'approvalPolicyId'
        >
      >
    ) => {
      queryClient.setQueryData<ChatThread>(chatKeys.detail(chatId), (current) =>
        current ? { ...current, ...patch } : current
      )

      queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) =>
        current.map((chat) => (chat.id === chatId ? { ...chat, ...patch } : chat))
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

      const previous = currentChat
        ? {
            mode: currentChat.mode,
            agentId: currentChat.agentId,
            modelId: currentChat.modelId,
            contextSizeId: currentChat.contextSizeId,
            reasoningEffortId: currentChat.reasoningEffortId,
            approvalPolicyId: currentChat.approvalPolicyId
          }
        : undefined

      const generation = (modeUpdateGenerationByChatRef.current.get(chatId) ?? 0) + 1
      modeUpdateGenerationByChatRef.current.set(chatId, generation)

      const defaults = queryClient.getQueryData<{ defaults: ModeRuntimeDefault[] }>(
        agentKeys.modeDefaults()
      )?.defaults
      const modeDefault = defaults ? resolveModeRuntimeDefault(defaults, mode) : null

      updateChatCacheRuntime(chatId, {
        mode,
        ...(modeDefault
          ? {
              agentId: modeDefault.agentId,
              modelId: modeDefault.modelId,
              contextSizeId: modeDefault.contextSizeId,
              reasoningEffortId: modeDefault.reasoningEffortId,
              approvalPolicyId: modeDefault.approvalPolicyId
            }
          : {})
      })

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

        updateChatCacheRuntime(chatId, {
          mode: response.mode,
          agentId: response.agentId,
          modelId: response.modelId ?? null,
          contextSizeId: response.contextSizeId ?? null,
          reasoningEffortId: response.reasoningEffortId ?? null,
          approvalPolicyId: response.approvalPolicyId ?? null
        })
      } catch (error) {
        if (modeUpdateGenerationByChatRef.current.get(chatId) !== generation) {
          return
        }

        if (previous) {
          updateChatCacheRuntime(chatId, previous)
        }

        const message = error instanceof Error ? error.message : 'Failed to update chat mode.'
        setModeUpdateErrorByChat((current) => ({ ...current, [chatId]: message }))
      }
    },
    [queryClient, updateChatCacheRuntime]
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

  const updateChatContextSizeAction = useCallback(
    async (chatId: string, contextSizeId: string | null) => {
      queryClient.setQueryData<ChatThread>(chatKeys.detail(chatId), (current) =>
        current ? { ...current, contextSizeId } : current
      )

      queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) =>
        current.map((chat) => (chat.id === chatId ? { ...chat, contextSizeId } : chat))
      )

      setContextSizeUpdateErrorByChat((current) => {
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
        const response = await updateChatContextSize(chatId, { contextSizeId })

        queryClient.setQueryData<ChatThread>(chatKeys.detail(chatId), (current) =>
          current ? { ...current, contextSizeId: response.contextSizeId ?? null } : current
        )

        queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) =>
          current.map((chat) =>
            chat.id === chatId ? { ...chat, contextSizeId: response.contextSizeId ?? null } : chat
          )
        )
      } catch (error) {
        const message =
          error instanceof Error ? error.message : 'Failed to update chat context size.'
        setContextSizeUpdateErrorByChat((current) => ({ ...current, [chatId]: message }))
      }
    },
    [queryClient]
  )

  const getContextSizeUpdateError = useCallback(
    (chatId: string) => contextSizeUpdateErrorByChat[chatId],
    [contextSizeUpdateErrorByChat]
  )

  const updateChatReasoningEffortAction = useCallback(
    async (chatId: string, reasoningEffortId: string | null) => {
      queryClient.setQueryData<ChatThread>(chatKeys.detail(chatId), (current) =>
        current ? { ...current, reasoningEffortId } : current
      )

      queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) =>
        current.map((chat) => (chat.id === chatId ? { ...chat, reasoningEffortId } : chat))
      )

      setReasoningEffortUpdateErrorByChat((current) => {
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
        const response = await updateChatReasoningEffort(chatId, { reasoningEffortId })

        queryClient.setQueryData<ChatThread>(chatKeys.detail(chatId), (current) =>
          current ? { ...current, reasoningEffortId: response.reasoningEffortId ?? null } : current
        )

        queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) =>
          current.map((chat) =>
            chat.id === chatId
              ? { ...chat, reasoningEffortId: response.reasoningEffortId ?? null }
              : chat
          )
        )
      } catch (error) {
        const message =
          error instanceof Error ? error.message : 'Failed to update chat reasoning effort.'
        setReasoningEffortUpdateErrorByChat((current) => ({ ...current, [chatId]: message }))
      }
    },
    [queryClient]
  )

  const getReasoningEffortUpdateError = useCallback(
    (chatId: string) => reasoningEffortUpdateErrorByChat[chatId],
    [reasoningEffortUpdateErrorByChat]
  )

  const updateChatApprovalPolicyAction = useCallback(
    async (chatId: string, approvalPolicyId: string | null) => {
      queryClient.setQueryData<ChatThread>(chatKeys.detail(chatId), (current) =>
        current ? { ...current, approvalPolicyId } : current
      )

      queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) =>
        current.map((chat) => (chat.id === chatId ? { ...chat, approvalPolicyId } : chat))
      )

      setApprovalPolicyUpdateErrorByChat((current) => {
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
        const response = await updateChatApprovalPolicy(chatId, { approvalPolicyId })

        queryClient.setQueryData<ChatThread>(chatKeys.detail(chatId), (current) =>
          current ? { ...current, approvalPolicyId: response.approvalPolicyId ?? null } : current
        )

        queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) =>
          current.map((chat) =>
            chat.id === chatId
              ? { ...chat, approvalPolicyId: response.approvalPolicyId ?? null }
              : chat
          )
        )
      } catch (error) {
        const message =
          error instanceof Error ? error.message : 'Failed to update chat approval policy.'
        setApprovalPolicyUpdateErrorByChat((current) => ({ ...current, [chatId]: message }))
      }
    },
    [queryClient]
  )

  const getApprovalPolicyUpdateError = useCallback(
    (chatId: string) => approvalPolicyUpdateErrorByChat[chatId],
    [approvalPolicyUpdateErrorByChat]
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
    updateChatContextSize: updateChatContextSizeAction,
    getContextSizeUpdateError,
    updateChatReasoningEffort: updateChatReasoningEffortAction,
    getReasoningEffortUpdateError,
    updateChatApprovalPolicy: updateChatApprovalPolicyAction,
    getApprovalPolicyUpdateError,
    updateChatProject: updateChatProjectAction
  }
}
