import { planNewChatTab, type ResolvedNewChatWorkspace } from '@/lib/chat-tabs/resolve-workspace-for-new-tab'
import type { CreateLocalDraftOptions } from '@/lib/chat/create-local-draft'
import type { ChatThread } from '@/lib/chat/types'
import type { Project } from '@/lib/projects/types'

export type ResolvedChatFromSource = {
  workspace: ResolvedNewChatWorkspace
  draftOptions: Pick<
    CreateLocalDraftOptions,
    | 'mode'
    | 'agentId'
    | 'modelId'
    | 'contextSizeId'
    | 'reasoningEffortId'
    | 'approvalPolicyId'
  >
}

/**
 * Resolve a new chat in the same workspace as the source chat, copying runtime settings.
 */
export function resolveChatFromSource(
  sourceChat: ChatThread,
  projects: Project[]
): ResolvedChatFromSource | null {
  const plan = planNewChatTab(sourceChat, projects)
  if (plan.kind !== 'create') {
    return null
  }

  return {
    workspace: plan.workspace,
    draftOptions: {
      mode: sourceChat.mode,
      agentId: sourceChat.agentId,
      modelId: sourceChat.modelId,
      contextSizeId: sourceChat.contextSizeId,
      reasoningEffortId: sourceChat.reasoningEffortId,
      approvalPolicyId: sourceChat.approvalPolicyId
    }
  }
}
