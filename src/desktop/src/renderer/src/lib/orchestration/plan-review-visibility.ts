import type { ChatThread } from '@/lib/chat/types'

export function isChildRunning(childChat: ChatThread | undefined): boolean {
  if (!childChat) {
    return false
  }

  return childChat.messages.some(
    (message) => message.status === 'processing' || message.status === 'streaming'
  )
}

export function getPlanReviewVisibility(
  reviewChild: ChatThread | undefined,
  reviewReady: boolean
): { reviewing: boolean; reviewStarted: boolean } {
  const reviewing = isChildRunning(reviewChild)
  const reviewStarted = Boolean(reviewChild) && !reviewReady && !reviewing

  return { reviewing, reviewStarted }
}
