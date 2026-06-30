export type ChatRole = 'user' | 'assistant'

export type ChatMessageStatus = 'complete' | 'streaming' | 'processing' | 'error'

export type ChatMode = 'agent' | 'plan' | 'implement' | 'orchestrate' | 'goal'

export type ChatMessage = {
  id: string
  role: ChatRole
  content: string
  createdAt: string
  status: ChatMessageStatus
}

export type ChatThread = {
  id: string
  title: string
  preview: string
  updatedAt: string
  agentId: string
  workspacePath: string
  mode: ChatMode
  parentChatId?: string | null
  attachedPlanId?: string | null
  goalChatId?: string | null
  messages: ChatMessage[]
}

export type ChatMarker = {
  id: string
  content: string
  variant: 'status' | 'tool'
}

export type CreateChatRequest = {
  agent: string
  workspacePath: string
  mode?: ChatMode
  parentChatId?: string | null
  attachedPlanId?: string | null
}

export type UpdateChatRequest = {
  mode: ChatMode
  attachedPlanId?: string | null
}

export type CreateChatResponse = {
  id: string
  agentId: string
  workspacePath: string
  mode: ChatMode
  parentChatId?: string | null
  attachedPlanId?: string | null
  goalChatId?: string | null
}

export type ChatSummaryResponse = {
  id: string
  title: string
  preview: string
  updatedAt: string
  agentId: string
  workspacePath: string
  mode: ChatMode
  parentChatId?: string | null
  attachedPlanId?: string | null
  goalChatId?: string | null
}

export type ChatDetailResponse = {
  id: string
  title: string
  agentId: string
  workspacePath: string
  mode: ChatMode
  parentChatId?: string | null
  attachedPlanId?: string | null
  goalChatId?: string | null
  messages: ChatMessage[]
}

export type SseHandlers = {
  onStatus?: (phase: string) => void
  onToken?: (text: string) => void
  onTool?: (label: string) => void
  onDone?: (messageId: string) => void
  onError?: (code: string, message: string) => void
}

export type ChatModeOption = {
  value: ChatMode
  label: string
  description: string
  requiresPlan?: boolean
}

export const CHAT_MODES: ChatModeOption[] = [
  { value: 'agent', label: 'Agent', description: 'General coding assistant' },
  { value: 'plan', label: 'Plan', description: 'Research and design before making changes' },
  {
    value: 'implement',
    label: 'Implement',
    description: 'Execute an attached plan (requires plan ID)',
    requiresPlan: true
  },
  {
    value: 'orchestrate',
    label: 'Orchestrate',
    description: 'Break work into sub-plans for parallel execution'
  },
  { value: 'goal', label: 'Goal', description: 'Track long-term progress across child chats' }
]

/** @deprecated Use CHAT_MODES */
export const USER_CREATABLE_MODES = CHAT_MODES

export function formatChatMode(mode: ChatMode): string {
  return CHAT_MODES.find((entry) => entry.value === mode)?.label ?? mode
}
