import type { ChatSummaryResponse, ChatThread } from '@/lib/chat/types'
import { getApiBaseUrl } from '@/lib/api'
import { formatChatModeUpdateError, readErrorMessage } from '@/lib/http/read-error-message'

const chatErrorOptions = { formatMessage: formatChatModeUpdateError }

function mapSummary(summary: ChatSummaryResponse): ChatThread {
  return {
    id: summary.id,
    title: summary.title,
    preview: summary.preview,
    updatedAt: summary.updatedAt,
    agentId: summary.agentId,
    projectId: summary.projectId,
    workspaceId: summary.workspaceId,
    workspacePath: summary.workspacePath,
    mode: summary.mode ?? 'default',
    modelId: summary.modelId ?? null,
    contextSizeId: summary.contextSizeId ?? null,
    reasoningEffortId: summary.reasoningEffortId ?? null,
    approvalPolicyId: summary.approvalPolicyId ?? null,
    parentChatId: summary.parentChatId,
    planFilePath: summary.planFilePath,
    status: summary.status ?? 'read',
    lastReadAt: summary.lastReadAt ?? null,
    messages: []
  }
}

export type SearchChatsParams = {
  q?: string
  limit?: number
}

export async function searchChats(params: SearchChatsParams = {}): Promise<ChatThread[]> {
  const search = new URLSearchParams()
  if (params.q) {
    search.set('q', params.q)
  }
  if (params.limit !== undefined) {
    search.set('limit', String(params.limit))
  }

  const query = search.toString()
  const response = await fetch(`${getApiBaseUrl()}/chats/search${query ? `?${query}` : ''}`)

  if (!response.ok) {
    throw new Error(await readErrorMessage(response, chatErrorOptions))
  }

  const summaries = (await response.json()) as ChatSummaryResponse[]
  return summaries.map(mapSummary)
}
