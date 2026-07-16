import { useCallback, useMemo } from 'react'
import { useMatch, useNavigate } from '@tanstack/react-router'

import { useChatCache } from '@/hooks/chat/use-chat-cache'
import { useChatList } from '@/hooks/chat/use-chat-list'
import { useChatMutations } from '@/hooks/chat/use-chat-mutations'
import { useChatOrchestration } from '@/hooks/chat/use-chat-orchestration'
import { useChatReadState } from '@/hooks/chat/use-chat-read-state'
import { useChatStream } from '@/hooks/chat/use-chat-stream'

import { ChatContext, type ChatContextValue } from '@/providers/chat-context'

export function ChatProvider({ children }: { children: React.ReactNode }): React.JSX.Element {
  const navigate = useNavigate()
  const chatMatch = useMatch({
    from: '/_app/chat/$chatId',
    shouldThrow: false
  })
  const activeChatId = chatMatch?.params?.chatId

  const list = useChatList()
  const cache = useChatCache({ chats: list.chats })
  const stream = useChatStream({
    getChat: cache.getChat,
    loadChat: cache.loadChat,
    refetchChats: list.refetchChats,
    activeChatId,
    navigate
  })

  const navigateAwayIfDeleted = useCallback(
    (chatId: string) => {
      if (chatMatch?.params.chatId === chatId) {
        navigate({ to: '/' })
      }
    },
    [chatMatch, navigate]
  )

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

  const readState = useChatReadState({
    activeChatId,
    getChat: cache.getChat,
    getChildChats: cache.getChildChats,
    loadChat: cache.loadChat,
    isChatSending: stream.isChatSending,
    isParentKickingOffAny: orchestration.isParentKickingOffAny
  })

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
      getChatSidebarStatus: readState.getChatSidebarStatus
    }),
    [activeChatId, cache, list, mutations, orchestration, readState, stream]
  )

  return <ChatContext.Provider value={value}>{children}</ChatContext.Provider>
}
