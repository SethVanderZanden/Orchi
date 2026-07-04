export type ChatRole = 'user' | 'assistant'

export type ChatMessageStatus = 'complete' | 'streaming' | 'processing' | 'error'

export type AgentMode = string

export type AgentModeOption = {
  id: string
  label: string
  description: string | null
}

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
  projectId: string | null
  workspaceId: string | null
  workspacePath: string
  mode: AgentMode
  parentChatId: string | null
  planFilePath: string | null
  messages: ChatMessage[]
}

export type ChatMarker = {
  id: string
  content: string
  variant: 'status' | 'tool'
}

export type CreateChatRequest = {
  agent: string
  workspaceId: string
  mode?: AgentMode
}

export type CreateChatResponse = {
  id: string
  agentId: string
  projectId: string | null
  workspaceId: string | null
  workspacePath: string
  mode: AgentMode
  parentChatId: string | null
  planFilePath: string | null
}

export type UpdateChatModeRequest = {
  mode: AgentMode
}

export type UpdateChatModeResponse = {
  id: string
  mode: AgentMode
}

export type ChatSummaryResponse = {
  id: string
  title: string
  preview: string
  updatedAt: string
  agentId: string
  projectId: string | null
  workspaceId: string | null
  workspacePath: string
  mode: AgentMode
  parentChatId: string | null
  planFilePath: string | null
}

export type ChatDetailResponse = {
  id: string
  title: string
  agentId: string
  projectId: string | null
  workspaceId: string | null
  workspacePath: string
  mode: AgentMode
  parentChatId: string | null
  planFilePath: string | null
  messages: ChatMessage[]
}

export type KickOffPlanRequest = {
  planId: string
  title: string
  contentMarkdown: string
}

export type KickOffPlanResponse = {
  childChatId: string
  planFilePath: string
  initialPrompt: string
  kickoffMessage: string
}

export type KickOffReviewResponse = {
  reviewChildChatId: string
  reviewFilePath: string
  initialPrompt: string
}

export type SseHandlers = {
  onStatus?: (phase: string) => void
  onToken?: (text: string) => void
  onTool?: (label: string) => void
  onDone?: (messageId: string) => void
  onError?: (code: string, message: string) => void
}
