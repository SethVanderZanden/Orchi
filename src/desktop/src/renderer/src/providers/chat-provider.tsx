import { createContext, useCallback, useContext, useMemo } from 'react'
import { useMatch, useNavigate } from '@tanstack/react-router'

import type { AgentMode, ChatMarker, ChatThread, CreateChatOptions } from '@/lib/chat/types'
import type { ParsedPlan } from '@/lib/orchestration/parse-plans'
import type { OrchestrationWorkflowProgress } from '@/lib/orchestration/orchestration-state'
import type { ChatSidebarStatusVariant } from '@/lib/chat/chat-sidebar-status'

import { useChatCache } from '@/hooks/chat/use-chat-cache'
import { useChatList } from '@/hooks/chat/use-chat-list'
import { useChatMutations } from '@/hooks/chat/use-chat-mutations'
import { useChatOrchestration } from '@/hooks/chat/use-chat-orchestration'
import { useChatReadState } from '@/hooks/chat/use-chat-read-state'
import { useChatStream } from '@/hooks/chat/use-chat-stream'
import type { AgentActivityDetail } from '@/hooks/chat/types'

type ChatContextValue = {
  chats: ChatThread[]
  isLoadingChats: boolean
  isPendingChats: boolean
  isFetchingChats: boolean
  chatsError: Error | null
  refetchChats: () => Promise<unknown>
  searchQuery: string
  setSearchQuery: (query: string) => void
  getChat: (chatId: string) => ChatThread | undefined
  getChildChats: (parentChatId: string) => ChatThread[]
  loadChat: (chatId: string) => Promise<ChatThread | undefined>
  createChat: (options: CreateChatOptions) => Promise<ChatThread>
  updateChatMode: (chatId: string, mode: AgentMode) => Promise<void>
  getModeUpdateError: (chatId: string) => string | undefined
  updateChatModel: (chatId: string, modelId: string | null) => Promise<void>
  getModelUpdateError: (chatId: string) => string | undefined
  closeChat: (chatId: string) => Promise<void>
  deleteChat: (chatId: string) => Promise<void>
  sendMessage: (chatId: string, content: string) => Promise<void>
  kickOffPlan: (chatId: string, plan: ParsedPlan) => Promise<void>
  kickOffAllPlans: (chatId: string) => Promise<void>
  getOrchestrationKickoffProgress: (parentChatId: string) => OrchestrationWorkflowProgress | null
  setOrchestrationKickoffProgress: (
    parentChatId: string,
    progress: OrchestrationWorkflowProgress | null
  ) => void
  isChatSending: (chatId: string) => boolean
  isPlanKickingOff: (parentChatId: string, planId: string) => boolean
  isParentKickingOffAny: (parentChatId: string) => boolean
  getMarkers: (chatId: string) => ChatMarker[]
  subscribeAgentActivity: (listener: (detail: AgentActivityDetail) => void) => () => void
  activeChatId?: string
  markChatRead: (chatId: string) => void
  getChatSidebarStatus: (chat: ChatThread) => ChatSidebarStatusVariant
}

const ChatContext = createContext<ChatContextValue | null>(null)

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
    refetchChats: list.refetchChats
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
    chats: list.chats,
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
      closeChat: mutations.closeChat,
      deleteChat: mutations.deleteChat,
      sendMessage: stream.sendMessage,
      kickOffPlan: orchestration.kickOffPlan,
      kickOffAllPlans: orchestration.kickOffAllPlans,
      getOrchestrationKickoffProgress: orchestration.getOrchestrationKickoffProgress,
      setOrchestrationKickoffProgress: orchestration.setOrchestrationKickoffProgress,
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

export function useChat(): ChatContextValue {
  const context = useContext(ChatContext)

  if (!context) {
    throw new Error('useChat must be used within ChatProvider')
  }

  return context
}
