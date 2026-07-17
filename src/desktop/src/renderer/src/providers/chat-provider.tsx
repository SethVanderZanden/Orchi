import { useCallback, useEffect, useMemo, useRef } from 'react'
import { useMatch, useNavigate } from '@tanstack/react-router'

import { useChatCache } from '@/hooks/chat/use-chat-cache'
import { useChatList } from '@/hooks/chat/use-chat-list'
import { useChatMutations } from '@/hooks/chat/use-chat-mutations'
import { useChatOrchestration } from '@/hooks/chat/use-chat-orchestration'
import { useChatStatus } from '@/hooks/chat/use-chat-status'
import { useChatStatusEvents } from '@/hooks/chat/use-chat-status-events'
import { useChatStream } from '@/hooks/chat/use-chat-stream'
import { useUserPreferences } from '@/hooks/use-user-preferences'
import { notifyChatDeleted } from '@/lib/chat-tabs/chat-deleted'
import { resolveWorkspaceForNewTab } from '@/lib/chat-tabs/resolve-workspace-for-new-tab'

import { ChatContext, type ChatContextValue } from '@/providers/chat-context'
import { useProjects } from '@/providers/project-provider'

export function ChatProvider({ children }: { children: React.ReactNode }): React.JSX.Element {
  const navigate = useNavigate()
  const chatMatch = useMatch({
    from: '/_app/chat/$chatId',
    shouldThrow: false
  })
  const activeChatId = chatMatch?.params?.chatId

  const list = useChatList()
  const cache = useChatCache({ chats: list.chats })
  const { projects } = useProjects()
  const { postMessageBehavior } = useUserPreferences()

  const applyPostMessageBehaviorRef = useRef<(chatId: string) => void | Promise<void>>(
    async () => {}
  )

  const applyPostMessageBehavior = useCallback(
    (chatId: string) => applyPostMessageBehaviorRef.current(chatId),
    []
  )

  const stream = useChatStream({
    getChat: cache.getChat,
    loadChat: cache.loadChat,
    refetchChats: list.refetchChats,
    activeChatId,
    navigate,
    applyPostMessageBehavior
  })

  const navigateAwayIfDeleted = useCallback((chatId: string) => {
    notifyChatDeleted(chatId)
  }, [])

  const orchestration = useChatOrchestration({
    getChat: cache.getChat,
    getChildChats: cache.getChildChats,
    loadChat: cache.loadChat,
    sendMessage: stream.sendMessage,
    navigate
  })

  const mutations = useChatMutations({
    purgeFromQueryClient: cache.purgeFromQueryClient,
    purgeStreamState: stream.purgeStreamState,
    purgeKickoffState: orchestration.purgeKickoffState,
    navigateAwayIfDeleted,
    refetchChats: list.refetchChats,
    setSearchQuery: list.setSearchQuery,
    navigate
  })
  const { createChat } = mutations

  useEffect(() => {
    applyPostMessageBehaviorRef.current = async (sentChatId: string) => {
      if (sentChatId !== activeChatId) {
        return
      }

      if (postMessageBehavior === 'goToBoard') {
        navigate({ to: '/board' })
        return
      }

      if (postMessageBehavior === 'openNewChat') {
        const chat = cache.getChat(sentChatId)
        const workspace = resolveWorkspaceForNewTab(chat, projects)
        if (!workspace) {
          return
        }

        await createChat({
          workspaceId: workspace.workspaceId,
          workspacePath: workspace.workspacePath,
          projectId: workspace.projectId ?? undefined
        })
      }
    }
  }, [activeChatId, cache, createChat, navigate, postMessageBehavior, projects])

  const readState = useChatStatus({
    activeChatId,
    getChat: cache.getChat,
    getChildChats: cache.getChildChats,
    loadChat: cache.loadChat,
    isChatSending: stream.isChatSending,
    isParentKickingOffAny: orchestration.isParentKickingOffAny
  })

  useChatStatusEvents()

  const value = useMemo<ChatContextValue>(
    () => ({
      chats: list.chats,
      isLoadingChats: list.isLoadingChats,
      isPendingChats: list.isPendingChats,
      isFetchingChats: list.isFetchingChats,
      chatsError: list.chatsError,
      refetchChats: list.refetchChats,
      searchQuery: list.searchQuery,
      setSearchQuery: list.setSearchQuery,
      getChat: cache.getChat,
      getChildChats: cache.getChildChats,
      loadChat: cache.loadChat,
      createChat: mutations.createChat,
      updateChatMode: mutations.updateChatMode,
      getModeUpdateError: mutations.getModeUpdateError,
      updateChatModel: mutations.updateChatModel,
      getModelUpdateError: mutations.getModelUpdateError,
      updateChatContextSize: mutations.updateChatContextSize,
      getContextSizeUpdateError: mutations.getContextSizeUpdateError,
      updateChatReasoningEffort: mutations.updateChatReasoningEffort,
      getReasoningEffortUpdateError: mutations.getReasoningEffortUpdateError,
      updateChatApprovalPolicy: mutations.updateChatApprovalPolicy,
      getApprovalPolicyUpdateError: mutations.getApprovalPolicyUpdateError,
      updateChatProject: mutations.updateChatProject,
      closeChat: mutations.closeChat,
      deleteChat: mutations.deleteChat,
      sendMessage: stream.sendMessage,
      kickOffPlan: orchestration.kickOffPlan,
      kickOffAllPlans: orchestration.kickOffAllPlans,
      getOrchestrationKickoffProgress: orchestration.getOrchestrationKickoffProgress,
      setOrchestrationKickoffProgress: orchestration.setOrchestrationKickoffProgress,
      getOrchestrationError: orchestration.getOrchestrationError,
      isChatSending: stream.isChatSending,
      isPlanKickingOff: orchestration.isPlanKickingOff,
      isParentKickingOffAny: orchestration.isParentKickingOffAny,
      getMarkers: stream.getMarkers,
      subscribeAgentActivity: stream.subscribeAgentActivity,
      activeChatId,
      markChatRead: readState.markChatRead,
      getChatStatusVariant: readState.getChatStatusVariant
    }),
    [activeChatId, cache, list, mutations, orchestration, readState, stream]
  )

  return <ChatContext.Provider value={value}>{children}</ChatContext.Provider>
}
