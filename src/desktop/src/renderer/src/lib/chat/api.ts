import type {
  AgentModeOption,
  ChatDetailResponse,
  ChatSummaryResponse,
  CreateChatRequest,
  CreateChatResponse,
  KickOffPlanRequest,
  KickOffPlanResponse,
  KickOffReviewResponse,
  SseHandlers,
  UpdateChatModeRequest,
  UpdateChatModeResponse
} from '@/lib/chat/types'
import { getApiBaseUrl } from '@/lib/api'

async function readErrorMessage(response: Response): Promise<string> {
  try {
    const body = (await response.json()) as {
      message?: string
      Message?: string
      title?: string
      detail?: string
      errors?: Record<string, string[]>
    }

    if (body.errors) {
      const messages = Object.values(body.errors).flat()

      if (messages.length > 0) {
        return formatModeUpdateError(messages[0]!, body.title)
      }
    }

    if (body.detail) {
      return formatModeUpdateError(body.detail, body.title)
    }

    return body.message ?? body.Message ?? `API error: ${response.status}`
  } catch {
    return `API error: ${response.status}`
  }
}

function formatModeUpdateError(message: string, code?: string): string {
  if (
    code === 'Mode.Busy' ||
    message.includes('agent is running') ||
    message.startsWith('Mode.Busy')
  ) {
    return 'Wait for the agent to finish before changing mode.'
  }

  return message
}

function mapSummary(summary: ChatSummaryResponse) {
  return {
    id: summary.id,
    title: summary.title,
    preview: summary.preview,
    updatedAt: summary.updatedAt,
    agentId: summary.agentId,
    workspacePath: summary.workspacePath,
    mode: summary.mode ?? 'default',
    parentChatId: summary.parentChatId,
    planFilePath: summary.planFilePath,
    messages: [] as ChatDetailResponse['messages']
  }
}

function mapDetail(detail: ChatDetailResponse) {
  return {
    id: detail.id,
    title: detail.title,
    preview: detail.messages.at(-1)?.content ?? 'Start a conversation with Orchi',
    updatedAt: detail.messages.at(-1)?.createdAt ?? new Date().toISOString(),
    agentId: detail.agentId,
    workspacePath: detail.workspacePath,
    mode: detail.mode ?? 'default',
    parentChatId: detail.parentChatId,
    planFilePath: detail.planFilePath,
    messages: detail.messages
  }
}

export async function listChats() {
  const response = await fetch(`${getApiBaseUrl()}/chats`)
  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  const summaries = (await response.json()) as ChatSummaryResponse[]
  return summaries.map(mapSummary)
}

export async function listAgentModes() {
  const response = await fetch(`${getApiBaseUrl()}/agents/modes`)
  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return (await response.json()) as AgentModeOption[]
}

export async function createChat(request: CreateChatRequest) {
  const response = await fetch(`${getApiBaseUrl()}/chats`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      agent: request.agent,
      workspacePath: request.workspacePath,
      mode: request.mode ?? 'default'
    })
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  const created = (await response.json()) as CreateChatResponse
  return {
    id: created.id,
    title: 'New chat',
    preview: 'Start a conversation with Orchi',
    updatedAt: new Date().toISOString(),
    agentId: created.agentId,
    workspacePath: created.workspacePath,
    mode: created.mode ?? 'default',
    parentChatId: created.parentChatId,
    planFilePath: created.planFilePath,
    messages: []
  }
}

export async function getChat(chatId: string) {
  const response = await fetch(`${getApiBaseUrl()}/chats/${chatId}`)
  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  const detail = (await response.json()) as ChatDetailResponse
  return mapDetail(detail)
}

export async function updateChatMode(chatId: string, request: UpdateChatModeRequest) {
  const response = await fetch(`${getApiBaseUrl()}/chats/${chatId}/mode`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request)
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return (await response.json()) as UpdateChatModeResponse
}

export async function closeChat(chatId: string) {
  const response = await fetch(`${getApiBaseUrl()}/chats/${chatId}`, {
    method: 'DELETE'
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }
}

export async function shutdownChats() {
  await fetch(`${getApiBaseUrl()}/chats/shutdown`, {
    method: 'POST'
  })
}

export async function kickOffPlan(parentChatId: string, request: KickOffPlanRequest) {
  const response = await fetch(`${getApiBaseUrl()}/chats/${parentChatId}/plans/kickoff`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request)
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return (await response.json()) as KickOffPlanResponse
}

export async function kickOffReview(implementationChildChatId: string) {
  const response = await fetch(
    `${getApiBaseUrl()}/chats/${implementationChildChatId}/review/kickoff`,
    {
      method: 'POST'
    }
  )

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return (await response.json()) as KickOffReviewResponse
}

export async function sendMessageStream(
  chatId: string,
  content: string,
  handlers: SseHandlers,
  signal?: AbortSignal
) {
  const response = await fetch(`${getApiBaseUrl()}/chats/${chatId}/messages`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Accept: 'text/event-stream'
    },
    body: JSON.stringify({ content }),
    signal
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  if (!response.body) {
    throw new Error('Streaming response body was empty.')
  }

  const reader = response.body.getReader()
  const decoder = new TextDecoder()
  let buffer = ''

  while (true) {
    const { done, value } = await reader.read()
    if (done) {
      break
    }

    buffer += decoder.decode(value, { stream: true })

    while (true) {
      const boundary = buffer.indexOf('\n\n')
      if (boundary === -1) {
        break
      }

      const rawEvent = buffer.slice(0, boundary)
      buffer = buffer.slice(boundary + 2)
      parseSseEvent(rawEvent, handlers)
    }
  }
}

function parseSseEvent(rawEvent: string, handlers: SseHandlers) {
  let eventName = 'message'
  const dataLines: string[] = []

  for (const line of rawEvent.split('\n')) {
    if (line.startsWith('event:')) {
      eventName = line.slice('event:'.length).trim()
    } else if (line.startsWith('data:')) {
      dataLines.push(line.slice('data:'.length).trim())
    }
  }

  if (dataLines.length === 0) {
    return
  }

  const payload = JSON.parse(dataLines.join('\n')) as Record<string, unknown>

  switch (eventName) {
    case 'status':
      handlers.onStatus?.(String(payload.phase ?? 'processing'))
      break
    case 'token':
      handlers.onToken?.(String(payload.text ?? ''))
      break
    case 'tool':
      handlers.onTool?.(String(payload.label ?? 'tool'))
      break
    case 'done':
      handlers.onDone?.(String(payload.messageId ?? ''))
      break
    case 'error':
      handlers.onError?.(
        String(payload.code ?? 'Stream.Error'),
        String(payload.message ?? 'An unknown streaming error occurred.')
      )
      break
  }
}
