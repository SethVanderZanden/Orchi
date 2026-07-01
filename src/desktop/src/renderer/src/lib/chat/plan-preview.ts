import type { ChatMessage, ChatMode, ChatThread } from '@/lib/chat/types'

const PLAN_MODES: ChatMode[] = ['plan', 'orchestrate']

export function isPlanOrOrchestrateMode(mode: ChatMode): boolean {
  return PLAN_MODES.includes(mode)
}

export function getLatestAssistantMessage(messages: ChatMessage[]): ChatMessage | null {
  for (let index = messages.length - 1; index >= 0; index -= 1) {
    const message = messages[index]
    if (message.role === 'assistant') {
      return message
    }
  }

  return null
}

export function extractPlanTitle(markdown: string): string {
  const headingMatch = markdown.match(/^#{1,3}\s+(.+)$/m)
  if (headingMatch?.[1]) {
    return headingMatch[1].trim()
  }

  const firstLine = markdown
    .split('\n')
    .map((line) => line.trim())
    .find((line) => line.length > 0)

  if (firstLine) {
    return firstLine.length > 80 ? `${firstLine.slice(0, 77)}…` : firstLine
  }

  return 'Plan'
}

export function isAssistantMessageStreaming(message: ChatMessage | null): boolean {
  return message?.status === 'streaming' || message?.status === 'processing'
}

export function isPlanResponseMessage(chat: ChatThread, message: ChatMessage): boolean {
  return message.role === 'assistant' && isPlanOrOrchestrateMode(chat.mode)
}

export function hasPlanPreviewContent(chat: ChatThread): boolean {
  if (chat.attachedPlanId) {
    return true
  }

  if (!isPlanOrOrchestrateMode(chat.mode)) {
    return false
  }

  const latestAssistant = getLatestAssistantMessage(chat.messages)
  return latestAssistant !== null && (latestAssistant.content.length > 0 || isAssistantMessageStreaming(latestAssistant))
}
