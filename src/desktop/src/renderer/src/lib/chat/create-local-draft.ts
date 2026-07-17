import type { AgentMode, ChatThread } from '@/lib/chat/types'
import { getDefaultChatMode } from '@/lib/preferences/default-chat-mode'

export type CreateLocalDraftOptions = {
  workspaceId: string
  workspacePath: string
  projectId: string | null
  agentId?: string
  mode?: AgentMode
  modelId?: string | null
  contextSizeId?: string | null
  reasoningEffortId?: string | null
  approvalPolicyId?: string | null
}

export function createLocalDraftChat(options: CreateLocalDraftOptions): ChatThread {
  return {
    id: `local:${crypto.randomUUID()}`,
    title: 'New chat',
    preview: 'Start a conversation with Orchi',
    updatedAt: new Date().toISOString(),
    agentId: options.agentId ?? 'cursor',
    projectId: options.projectId,
    workspaceId: options.workspaceId,
    workspacePath: options.workspacePath,
    mode: options.mode ?? getDefaultChatMode(),
    modelId: options.modelId ?? null,
    contextSizeId: options.contextSizeId ?? null,
    reasoningEffortId: options.reasoningEffortId ?? null,
    approvalPolicyId: options.approvalPolicyId ?? null,
    parentChatId: null,
    planFilePath: null,
    status: 'read',
    lastReadAt: null,
    messages: []
  }
}
