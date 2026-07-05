import { createContext, useContext } from 'react'

import type { AgentMode, ChatMarker, ChatThread, CreateChatOptions } from '@/lib/chat/types'
import type { ChatSidebarStatusVariant } from '@/lib/chat/chat-sidebar-status'
import type { ParsedPlan } from '@/lib/orchestration/parse-plans'
import type { OrchestrationWorkflowProgress } from '@/lib/orchestration/orchestration-state'

import type { AgentActivityDetail } from '@/hooks/chat/types'

export type ChatContextValue = {
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
  updateChatProject: (chatId: string, projectId: string) => void
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

export const ChatContext = createContext<ChatContextValue | null>(null)

export function useChat(): ChatContextValue {
  const context = useContext(ChatContext)

  if (!context) {
    throw new Error('useChat must be used within ChatProvider')
  }

  return context
}
