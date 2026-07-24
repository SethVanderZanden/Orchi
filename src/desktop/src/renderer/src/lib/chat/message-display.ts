import type { AgentMode, ChatMarker, ChatMessage } from '@/lib/chat/types'
import {
  stripPlanBlocksForChatDisplay,
  stripReviewPlanBlocksForChatDisplay
} from '@/lib/orchestration/strip-plan-blocks'

export type ChatMessageDisplayState = {
  displayContent: string
  showPlaceholder: boolean
  showActivity: boolean
  showEmptyResponse: boolean
  shouldRender: boolean
}

type GetChatMessageDisplayStateOptions = {
  message: ChatMessage
  mode: AgentMode
  rowMarkers: ChatMarker[]
}

function resolveDisplayContent(message: ChatMessage, mode: AgentMode): string {
  if (message.role === 'user') {
    return message.content
  }

  if (mode === 'orchestration') {
    return stripPlanBlocksForChatDisplay(message.content)
  }

  if (mode === 'review') {
    return stripReviewPlanBlocksForChatDisplay(message.content)
  }

  return message.content
}

function isPlanOnlyAssistantMessage(content: string, mode: AgentMode): boolean {
  if (mode === 'orchestration') {
    return content.includes('orchi-plan:')
  }

  if (mode === 'review') {
    return content.includes('orchi-review-plan:')
  }

  return false
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
  const isPlanOnly =
    !isUser &&
    message.status === 'complete' &&
    displayContent.trim().length === 0 &&
    isPlanOnlyAssistantMessage(message.content, mode)
  const showEmptyResponse =
    !isUser &&
    message.status === 'complete' &&
    displayContent.trim().length === 0 &&
    !showActivity &&
    !isPlanOnly

  // Plan-only orchestration turns render via PlanCards / Plan review — skip empty bubbles.
  const shouldRender =
    isUser ||
    message.status === 'error' ||
    displayContent.length > 0 ||
    showActivity ||
    showPlaceholder ||
    showEmptyResponse ||
    isActive

  return {
    displayContent,
    showPlaceholder,
    showActivity,
    showEmptyResponse,
    shouldRender
  }
}
