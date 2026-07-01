import type {
  ChatDetailResponse,
  ChatMode,
  ChatSummaryResponse,
  CreateChatRequest,
  CreateChatResponse,
  PlanResponse,
  SseHandlers,
  UpdateChatRequest
} from '@/lib/chat/types'
import { getApiBaseUrl } from '@/lib/api'

async function readErrorMessage(response: Response): Promise<string> {
  try {
    const body = (await response.json()) as { message?: string; Message?: string }
    return body.message ?? body.Message ?? `API error: ${response.status}`
  } catch {
    return `API error: ${response.status}`
  }
}

function mapSummaryFields(
  summary: Pick<ChatSummaryResponse, 'mode' | 'parentChatId' | 'attachedPlanId' | 'goalChatId'>
) {
  return {
    mode: summary.mode ?? 'agent',
    parentChatId: summary.parentChatId ?? null,
    attachedPlanId: summary.attachedPlanId ?? null,
    goalChatId: summary.goalChatId ?? null
  }
}

function mapSummary(summary: ChatSummaryResponse) {
  return {
    id: summary.id,
    title: summary.title,
    preview: summary.preview,
    updatedAt: summary.updatedAt,
    agentId: summary.agentId,
    workspacePath: summary.workspacePath,
    messages: [] as ChatDetailResponse['messages'],
    ...mapSummaryFields(summary)
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
    messages: detail.messages,
    ...mapSummaryFields(detail)
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

export async function createChat(request: CreateChatRequest) {
  const response = await fetch(`${getApiBaseUrl()}/chats`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      agent: request.agent,
      workspacePath: request.workspacePath,
      mode: request.mode ?? 'agent',
      parentChatId: request.parentChatId ?? null,
      attachedPlanId: request.attachedPlanId ?? null
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
    mode: created.mode ?? 'agent',
    parentChatId: created.parentChatId ?? null,
    attachedPlanId: created.attachedPlanId ?? null,
    goalChatId: created.goalChatId ?? null,
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

export async function closeChat(chatId: string) {
  const response = await fetch(`${getApiBaseUrl()}/chats/${chatId}`, {
    method: 'DELETE'
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }
}

export async function updateChat(chatId: string, request: UpdateChatRequest) {
  const response = await fetch(`${getApiBaseUrl()}/chats/${chatId}`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      mode: request.mode,
      attachedPlanId: request.attachedPlanId ?? null
    })
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  const updated = (await response.json()) as ChatSummaryResponse
  return mapSummary(updated)
}

export async function shutdownChats() {
  await fetch(`${getApiBaseUrl()}/chats/shutdown`, {
    method: 'POST'
  })
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

export async function getPlan(planId: string): Promise<PlanResponse> {
  const response = await fetch(`${getApiBaseUrl()}/plans/${planId}`)

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return response.json() as Promise<PlanResponse>
}

export type CreateChatPlanRequest = {
  title: string
  contentMarkdown: string
}

export async function createChatPlan(chatId: string, request: CreateChatPlanRequest) {
  const response = await fetch(`${getApiBaseUrl()}/chats/${chatId}/plans`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request)
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return response.json()
}

export async function handoffToGoal(orchestratorChatId: string) {
  const response = await fetch(`${getApiBaseUrl()}/chats/${orchestratorChatId}/handoff-to-goal`, {
    method: 'POST'
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return response.json() as Promise<{ goalChatId: string }>
}

export async function dispatchSubPlan(
  planId: string,
  subPlanId: string,
  childMode: Extract<ChatMode, 'plan' | 'implement'>
) {
  const response = await fetch(`${getApiBaseUrl()}/plans/${planId}/dispatch`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ subPlanId, childMode })
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return response.json() as Promise<{ childChatId: string; subPlanId: string }>
}
