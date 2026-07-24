import type { AgentMode, ChatMarker, ChatMessage } from '@/lib/chat/types'
import { stripPlanBlocksForChatDisplay } from '@/lib/orchestration/strip-plan-blocks'

export type ChatMessageDisplayState = {
  displayContent: string
  showPlaceholder: boolean
  showActivity: boolean
  shouldRender: boolean
}

type GetChatMessageDisplayStateOptions = {
  message: ChatMessage
  mode: AgentMode
  rowMarkers: ChatMarker[]
}

function resolveDisplayContent(message: ChatMessage, mode: AgentMode): string {
  if (message.role === 'user' || mode !== 'orchestration') {
    return message.content
  }

  return stripPlanBlocksForChatDisplay(message.content)
}

/**
 * Derives per-row chat bubble visibility for the message list.
 * Plan-only orchestration turns are hidden from the bubble (Plan review owns that content).
 */
export function getChatMessageDisplayState({
  message,
  mode,
  rowMarkers
}: GetChatMessageDisplayStateOptions): ChatMessageDisplayState {
  const isUser = message.role === 'user'
  const displayContent = resolveDisplayContent(message, mode)
  const isActive = message.status === 'processing' || message.status === 'streaming'
  const showActivity = !isUser && rowMarkers.length > 0
  const showPlaceholder = !isUser && isActive && displayContent.length === 0 && !showActivity

  // Plan-only orchestration turns render via PlanCards / Plan review — skip empty bubbles.
  const shouldRender =
    isUser ||
    message.status === 'error' ||
    displayContent.length > 0 ||
    showActivity ||
    showPlaceholder ||
    isActive

  return {
    displayContent,
    showPlaceholder,
    showActivity,
    shouldRender
  }
}
