export type ChatRole = 'user' | 'assistant'

export type ChatMessageStatus = 'complete' | 'streaming' | 'processing' | 'error'

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
}

export type CreateChatResponse = {
  id: string
  agentId: string
  workspacePath: string
}

export type ChatSummaryResponse = {
  id: string
  title: string
  preview: string
  updatedAt: string
  agentId: string
  workspacePath: string
}

export type ChatDetailResponse = {
  id: string
  title: string
  agentId: string
  workspacePath: string
  messages: ChatMessage[]
}

export type SseHandlers = {
  onStatus?: (phase: string) => void
  onToken?: (text: string) => void
  onTool?: (label: string) => void
  onDone?: (messageId: string) => void
  onError?: (code: string, message: string) => void
}
