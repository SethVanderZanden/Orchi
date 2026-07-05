import { isLocalChat } from '@/lib/chat/chat-persistence'
import type { ChatThread } from '@/lib/chat/types'

export function needsOrchestrationHydration(
  chat: ChatThread | undefined,
  childCount: number,
  isKickoffActive: boolean
): boolean {
  if (!chat || isLocalChat(chat.id)) {
    return false
  }

  if (chat.mode !== 'orchestration') {
    return false
  }

  return chat.messages.length > 0 || childCount > 0 || isKickoffActive
}
