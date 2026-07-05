import type { ChatMessage, ChatThread } from '@/lib/chat/types'
import { getApiBaseUrl } from '@/lib/api'
import { readErrorMessage } from '@/lib/http/read-error-message'
import { readSseStream } from '@/lib/http/sse'
import type { OrchestrationStateResponse } from '@/lib/orchestration/orchestration-state'

export type OrchestrationEventHandlers = {
  onWorkflow?: (payload: {
    status: string
    currentStep: number | null
    totalSteps: number | null
    planId: string | null
  }) => void
  onChatCreated?: (payload: {
    chatId: string
    mode: string
    parentChatId: string
    planId: string | null
    planFilePath: string | null
  }) => void
  onParentMessage?: (payload: {
    messageId: string
    role: string
    content: string
  }) => void
  onAgentStatus?: (payload: { childChatId: string; phase: string }) => void
  onAgentToken?: (payload: { childChatId: string; text: string }) => void
  onAgentTool?: (payload: { childChatId: string; label: string }) => void
  onAgentDone?: (payload: { childChatId: string; messageId: string; succeeded: boolean }) => void
  onAgentError?: (payload: { childChatId: string; code: string; message: string }) => void
}

export async function getOrchestration(parentChatId: string): Promise<OrchestrationStateResponse> {
  const response = await fetch(`${getApiBaseUrl()}/chats/${parentChatId}/orchestration`)

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return (await response.json()) as OrchestrationStateResponse
}

export async function kickOffAllOrchestration(
  parentChatId: string
): Promise<OrchestrationStateResponse> {
  const response = await fetch(`${getApiBaseUrl()}/chats/${parentChatId}/orchestration/kickoff-all`, {
    method: 'POST'
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return (await response.json()) as OrchestrationStateResponse
}

export async function subscribeOrchestrationEvents(
  parentChatId: string,
  handlers: OrchestrationEventHandlers,
  signal?: AbortSignal
): Promise<void> {
  const response = await fetch(`${getApiBaseUrl()}/chats/${parentChatId}/orchestration/events`, {
    headers: { Accept: 'text/event-stream' },
    signal
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  await readSseStream(
    response,
    (parsed) => {
      dispatchOrchestrationEvent(parsed.event, parsed.data, handlers)
    },
    signal
  )
}

function dispatchOrchestrationEvent(
  eventName: string,
  data: string,
  handlers: OrchestrationEventHandlers
): void {
  const payload = JSON.parse(data) as Record<string, unknown>

  switch (eventName) {
    case 'workflow':
      handlers.onWorkflow?.({
        status: String(payload.status),
        currentStep: payload.currentStep == null ? null : Number(payload.currentStep),
        totalSteps: payload.totalSteps == null ? null : Number(payload.totalSteps),
        planId: payload.planId == null ? null : String(payload.planId)
      })
      break

    case 'chat_created':
      handlers.onChatCreated?.({
        chatId: String(payload.chatId),
        mode: String(payload.mode),
        parentChatId: String(payload.parentChatId),
        planId: payload.planId == null ? null : String(payload.planId),
        planFilePath: payload.planFilePath == null ? null : String(payload.planFilePath)
      })
      break

    case 'parent_message':
      handlers.onParentMessage?.({
        messageId: String(payload.messageId),
        role: String(payload.role),
        content: String(payload.content)
      })
      break

    case 'agent_status':
      handlers.onAgentStatus?.({
        childChatId: String(payload.childChatId),
        phase: String(payload.phase)
      })
      break

    case 'agent_token':
      handlers.onAgentToken?.({
        childChatId: String(payload.childChatId),
        text: String(payload.text)
      })
      break

    case 'agent_tool':
      handlers.onAgentTool?.({
        childChatId: String(payload.childChatId),
        label: String(payload.label)
      })
      break

    case 'agent_done':
      handlers.onAgentDone?.({
        childChatId: String(payload.childChatId),
        messageId: String(payload.messageId),
        succeeded: Boolean(payload.succeeded)
      })
      break

    case 'agent_error':
      handlers.onAgentError?.({
        childChatId: String(payload.childChatId),
        code: String(payload.code),
        message: String(payload.message)
      })
      break
  }
}

export function mapChatCreatedToThread(
  parentChat: ChatThread,
  payload: {
    chatId: string
    mode: string
    planId: string | null
    planFilePath: string | null
  }
): ChatThread {
  const title = payload.planId
    ? payload.mode === 'review'
      ? `${payload.planId
          .split('-')
          .map((word, index) =>
            index === 0 ? word.charAt(0).toUpperCase() + word.slice(1) : word
          )
          .join(' ')} review`
      : payload.planId
          .split('-')
          .map((word, index) =>
            index === 0 ? word.charAt(0).toUpperCase() + word.slice(1) : word
          )
          .join(' ')
    : payload.mode === 'review'
      ? 'Review'
      : 'Implementation'

  return {
    id: payload.chatId,
    title,
    preview: payload.planFilePath ?? 'Agent run',
    updatedAt: new Date().toISOString(),
    agentId: parentChat.agentId,
    projectId: parentChat.projectId,
    workspaceId: parentChat.workspaceId,
    workspacePath: parentChat.workspacePath,
    mode: payload.mode,
    modelId: parentChat.modelId,
    parentChatId: parentChat.id,
    planFilePath: payload.planFilePath,
    messages: []
  }
}

export function appendParentOrchestrationMessage(
  parentChat: ChatThread,
  payload: { messageId: string; role: string; content: string }
): ChatThread {
  const message: ChatMessage = {
    id: payload.messageId,
    role: payload.role as ChatMessage['role'],
    content: payload.content,
    createdAt: new Date().toISOString(),
    status: 'complete'
  }

  return {
    ...parentChat,
    preview: payload.content,
    updatedAt: message.createdAt,
    messages: [...parentChat.messages, message]
  }
}
