import type { QueryClient } from '@tanstack/react-query'

import { createChat } from '@/lib/chat/api'
import { isLocalChat } from '@/lib/chat/chat-persistence'
import { migrateChatClientState } from '@/lib/chat/migrate-chat-client-state'
import type { ChatThread } from '@/lib/chat/types'
import { chatKeys } from '@/lib/query-keys'

const promotionByLocalId = new Map<string, Promise<string>>()

function mapCreatedToThread(
  draft: ChatThread,
  created: Awaited<ReturnType<typeof createChat>>
): ChatThread {
  return {
    id: created.id,
    title: draft.title,
    preview: draft.preview,
    updatedAt: draft.updatedAt,
    agentId: created.agentId,
    projectId: created.projectId ?? draft.projectId,
    workspaceId: created.workspaceId ?? draft.workspaceId,
    workspacePath: created.workspacePath,
    mode: created.mode,
    modelId: created.modelId ?? null,
    contextSizeId: created.contextSizeId ?? null,
    reasoningEffortId: created.reasoningEffortId ?? null,
    approvalPolicyId: created.approvalPolicyId ?? null,
    parentChatId: created.parentChatId,
    planFilePath: created.planFilePath,
    status: created.status,
    lastReadAt: created.lastReadAt,
    messages: []
  }
}

function migrateDraftCache(queryClient: QueryClient, localId: string, persisted: ChatThread): void {
  queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) => {
    const withoutLocal = current.filter((chat) => chat.id !== localId)
    return [persisted, ...withoutLocal.filter((chat) => chat.id !== persisted.id)]
  })

  queryClient.removeQueries({ queryKey: chatKeys.detail(localId) })
  queryClient.setQueryData(chatKeys.detail(persisted.id), persisted)
  migrateChatClientState(localId, persisted.id)
}

async function promoteDraftOnce(
  queryClient: QueryClient,
  localId: string,
  draft: ChatThread
): Promise<string> {
  if (!draft.workspaceId) {
    throw new Error('Draft chat is missing a workspace.')
  }

  const created = await createChat({
    workspaceId: draft.workspaceId,
    agent: draft.agentId,
    mode: draft.mode,
    modelId: draft.modelId,
    contextSizeId: draft.contextSizeId,
    reasoningEffortId: draft.reasoningEffortId,
    approvalPolicyId: draft.approvalPolicyId
  })

  const persisted = mapCreatedToThread(draft, created)
  migrateDraftCache(queryClient, localId, persisted)
  return persisted.id
}

export async function promoteLocalChat(queryClient: QueryClient, localId: string): Promise<string> {
  if (!isLocalChat(localId)) {
    return localId
  }

  const inFlight = promotionByLocalId.get(localId)
  if (inFlight) {
    return inFlight
  }

  const draft =
    queryClient.getQueryData<ChatThread>(chatKeys.detail(localId)) ??
    queryClient.getQueryData<ChatThread[]>(chatKeys.lists())?.find((chat) => chat.id === localId)

  if (!draft) {
    throw new Error('Draft chat was not found.')
  }

  const promise = promoteDraftOnce(queryClient, localId, draft)
  promotionByLocalId.set(localId, promise)

  try {
    return await promise
  } finally {
    promotionByLocalId.delete(localId)
  }
}

export function __resetPromotionLocksForTests(): void {
  promotionByLocalId.clear()
}
