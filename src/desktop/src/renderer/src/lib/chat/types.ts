export type ChatRole = 'user' | 'assistant'

export type ChatMessageStatus = 'complete' | 'streaming' | 'processing' | 'error'

export type ChatStatus = 'read' | 'inProgress' | 'readyForReview'

export type AgentMode = string

export type AgentModeOption = {
  id: string
  label: string
  description: string | null
}

export type AgentModel = {
  id: string
  label: string
  isDefault: boolean
  isCurrent: boolean
  isEnabled: boolean
  source: string
}

export type AgentModelListResponse = {
  models: AgentModel[]
  lastSyncedAt: string | null
}

export type AgentModelSyncResponse = {
  models: AgentModel[]
  syncedAt: string
}

export type AgentInfo = {
  id: string
  label: string
}

export type AgentContextSize = {
  id: string
  label: string
  tokenCount: number
  isEnabled: boolean
  source: string
}

export type AgentContextSizeListResponse = {
  contextSizes: AgentContextSize[]
}

export type AgentCliOptionKind = 'model_reasoning_effort' | 'approval_policy'

export type AgentCliOption = {
  kind: string
  id: string
  label: string
  cliValue: string
  isEnabled: boolean
  source: string
}

export type AgentCliOptionListResponse = {
  options: AgentCliOption[]
}

export type ModeRuntimeDefault = {
  mode: string
  label: string
  agentId: string
  modelId: string | null
  contextSizeId: string | null
  reasoningEffortId: string | null
  approvalPolicyId: string | null
}

export type ModeRuntimeDefaultsListResponse = {
  defaults: ModeRuntimeDefault[]
}

export type UpdateModeRuntimeDefaultRequest = {
  agentId: string
  modelId: string | null
  contextSizeId: string | null
  reasoningEffortId: string | null
  approvalPolicyId: string | null
}

export type UpdateModeRuntimeDefaultResponse = ModeRuntimeDefault

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
  modelId: string | null
  contextSizeId: string | null
  reasoningEffortId: string | null
  approvalPolicyId: string | null
  parentChatId: string | null
  planFilePath: string | null
  status: ChatStatus
  lastReadAt: string | null
  messages: ChatMessage[]
}

export type ChatMarker = {
  id: string
  content: string
  variant: 'status' | 'tool'
}

export type CreateChatOptions = {
  workspaceId: string
  workspacePath: string
  projectId?: string
  /** When false, create the draft without navigating to it (e.g. open in split). */
  navigate?: boolean
}

export type SendMessageOptions = {
  /** When true, skip the user's post-message navigation preference. */
  skipPostMessageBehavior?: boolean
}

export type CreateChatRequest = {
  workspaceId: string
  agent?: string | null
  mode?: AgentMode
  modelId?: string | null
  contextSizeId?: string | null
  reasoningEffortId?: string | null
  approvalPolicyId?: string | null
}

export type CreateChatResponse = {
  id: string
  agentId: string
  projectId: string | null
  workspaceId: string | null
  workspacePath: string
  mode: AgentMode
  modelId: string | null
  contextSizeId: string | null
  reasoningEffortId: string | null
  approvalPolicyId: string | null
  parentChatId: string | null
  planFilePath: string | null
}

export type UpdateChatModeRequest = {
  mode: AgentMode
}

export type UpdateChatModeResponse = {
  id: string
  mode: AgentMode
  agentId: string
  modelId: string | null
  contextSizeId: string | null
  reasoningEffortId: string | null
  approvalPolicyId: string | null
}

export type UpdateChatModelRequest = {
  modelId: string | null
}

export type UpdateChatModelResponse = {
  id: string
  modelId: string | null
}

export type UpdateChatContextSizeRequest = {
  contextSizeId: string | null
}

export type UpdateChatContextSizeResponse = {
  id: string
  contextSizeId: string | null
}

export type UpdateChatReasoningEffortRequest = {
  reasoningEffortId: string | null
}

export type UpdateChatReasoningEffortResponse = {
  id: string
  reasoningEffortId: string | null
}

export type UpdateChatApprovalPolicyRequest = {
  approvalPolicyId: string | null
}

export type UpdateChatApprovalPolicyResponse = {
  id: string
  approvalPolicyId: string | null
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
  modelId: string | null
  contextSizeId: string | null
  reasoningEffortId: string | null
  approvalPolicyId: string | null
  parentChatId: string | null
  planFilePath: string | null
  status: ChatStatus
  lastReadAt: string | null
}

export type ChatDetailResponse = {
  id: string
  title: string
  agentId: string
  projectId: string | null
  workspaceId: string | null
  workspacePath: string
  mode: AgentMode
  modelId: string | null
  contextSizeId: string | null
  reasoningEffortId: string | null
  approvalPolicyId: string | null
  parentChatId: string | null
  planFilePath: string | null
  status: ChatStatus
  lastReadAt: string | null
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
  kickoffMessage: string
}

export type SseHandlers = {
  onStatus?: (phase: string) => void
  onToken?: (text: string) => void
  onTool?: (label: string) => void
  onDone?: (messageId: string) => void
  onError?: (code: string, message: string) => void
}

export type AgentActivityDetail = {
  phase: 'tool' | 'done'
  label?: string
}
