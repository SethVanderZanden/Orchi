import type { AgentActivityDetail } from '@/lib/chat/types'
import type { ChatMarker, ChatMessage, SseHandlers } from '@/lib/chat/types'
import { applyToken } from '@/lib/chat/message-updates'
import { createTokenBatcher } from '@/lib/chat/token-batcher'

type CreateMessageStreamHandlersOptions = {
  isActiveTurn: () => boolean
  assistantMessageId: string
  chatId: string
  updateAssistantMessage: (
    chatId: string,
    assistantMessageId: string,
    updater: (message: ChatMessage) => ChatMessage
  ) => void
  appendMarker: (chatId: string, marker: ChatMarker) => void
  clearMarkers: (chatId: string) => void
  notifyAgentActivity: (detail: AgentActivityDetail) => void
}

export function createMessageStreamHandlers({
  isActiveTurn,
  assistantMessageId,
  chatId,
  updateAssistantMessage,
  appendMarker,
  clearMarkers,
  notifyAgentActivity
}: CreateMessageStreamHandlersOptions): SseHandlers {
  const tokens = createTokenBatcher((text) => {
    if (!isActiveTurn()) {
      return
    }

    updateAssistantMessage(chatId, assistantMessageId, (message) => applyToken(message, text))
  })

  return {
    onToken: (text) => {
      if (!isActiveTurn()) {
        return
      }

      tokens.push(text)
    },
    onTool: (label) => {
      if (!isActiveTurn()) {
        return
      }

      tokens.flush()

      appendMarker(chatId, {
        id: crypto.randomUUID(),
        content: label,
        variant: 'tool'
      })

      if (label.startsWith('Writing') || label.startsWith('Running')) {
        notifyAgentActivity({ phase: 'tool', label })
      }
    },
    onDone: () => {
      if (!isActiveTurn()) {
        tokens.cancel()
        return
      }

      tokens.flush()

      updateAssistantMessage(chatId, assistantMessageId, (message) => ({
        ...message,
        status: 'complete'
      }))
      notifyAgentActivity({ phase: 'done' })
      clearMarkers(chatId)
    },
    onError: (code, message) => {
      if (!isActiveTurn()) {
        tokens.cancel()
        return
      }

      tokens.flush()

      updateAssistantMessage(chatId, assistantMessageId, (currentMessage) => ({
        ...currentMessage,
        content: currentMessage.content || message,
        status: 'error'
      }))
      appendMarker(chatId, {
        id: crypto.randomUUID(),
        content: `${code}: ${message}`,
        variant: 'tool'
      })
      clearMarkers(chatId)
    }
  }
}
