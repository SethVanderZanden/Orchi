import type { AgentMode, ChatThread } from '@/lib/chat/types'

export type CreateLocalDraftOptions = {
  workspaceId: string
  workspacePath: string
  projectId: string | null
  agentId?: string
  mode?: AgentMode
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
    mode: options.mode ?? 'default',
    modelId: null,
    parentChatId: null,
    planFilePath: null,
    messages: []
  }
}
