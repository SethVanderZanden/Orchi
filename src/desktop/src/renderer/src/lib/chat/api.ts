import type {
  AgentModeOption,
  ChatDetailResponse,
  ChatSummaryResponse,
  ChatThread,
  CreateChatRequest,
  CreateChatResponse,
  KickOffPlanRequest,
  KickOffPlanResponse,
  KickOffReviewResponse,
  SseHandlers,
  UpdateChatModeRequest,
  UpdateChatModeResponse,
  UpdateChatModelRequest,
  UpdateChatModelResponse,
  UpdateChatContextSizeRequest,
  UpdateChatContextSizeResponse,
  UpdateChatReasoningEffortRequest,
  UpdateChatReasoningEffortResponse,
  UpdateChatApprovalPolicyRequest,
  UpdateChatApprovalPolicyResponse,
  UpdateChatWorkspaceRequest,
  UpdateChatWorkspaceResponse
} from '@/lib/chat/types'
import { getApiBaseUrl } from '@/lib/api'
import {
  formatChatModeUpdateError,
  formatChatModelUpdateError,
  readErrorMessage
} from '@/lib/http/read-error-message'
import { readSseStream } from '@/lib/http/sse'

const chatErrorOptions = { formatMessage: formatChatModeUpdateError }
const chatModelErrorOptions = { formatMessage: formatChatModelUpdateError }

function mapSummary(summary: ChatSummaryResponse): ChatThread {
  return {
    id: summary.id,
    title: summary.title,
    preview: summary.preview,
    updatedAt: summary.updatedAt,
    agentId: summary.agentId,
    projectId: summary.projectId,
    workspaceId: summary.workspaceId,
    workspacePath: summary.workspacePath,
    mode: summary.mode ?? 'default',
    modelId: summary.modelId ?? null,
    contextSizeId: summary.contextSizeId ?? null,
    reasoningEffortId: summary.reasoningEffortId ?? null,
    approvalPolicyId: summary.approvalPolicyId ?? null,
    parentChatId: summary.parentChatId,
    planFilePath: summary.planFilePath,
    status: summary.status ?? 'read',
    lastReadAt: summary.lastReadAt ?? null,
    messages: [] as ChatDetailResponse['messages']
  }
}

function mapDetail(detail: ChatDetailResponse): ChatThread {
  return {
    id: detail.id,
    title: detail.title,
    preview: detail.messages.at(-1)?.content ?? 'Start a conversation with Orchi',
    updatedAt: detail.messages.at(-1)?.createdAt ?? new Date().toISOString(),
    agentId: detail.agentId,
    projectId: detail.projectId,
    workspaceId: detail.workspaceId,
    workspacePath: detail.workspacePath,
    mode: detail.mode ?? 'default',
    modelId: detail.modelId ?? null,
    contextSizeId: detail.contextSizeId ?? null,
    reasoningEffortId: detail.reasoningEffortId ?? null,
    approvalPolicyId: detail.approvalPolicyId ?? null,
    parentChatId: detail.parentChatId,
    planFilePath: detail.planFilePath,
    status: detail.status ?? 'read',
    lastReadAt: detail.lastReadAt ?? null,
    messages: detail.messages
  }
}

export async function listChats(): Promise<ChatThread[]> {
  const response = await fetch(`${getApiBaseUrl()}/chats`)
  if (!response.ok) {
    throw new Error(await readErrorMessage(response, chatErrorOptions))
  }

  const summaries = (await response.json()) as ChatSummaryResponse[]
  return summaries.map(mapSummary)
}

export async function listAgentModes(): Promise<AgentModeOption[]> {
  const response = await fetch(`${getApiBaseUrl()}/agents/modes`)
  if (!response.ok) {
    throw new Error(await readErrorMessage(response, chatErrorOptions))
  }

  return (await response.json()) as AgentModeOption[]
}

export async function createChat(request: CreateChatRequest): Promise<ChatThread> {
  const response = await fetch(`${getApiBaseUrl()}/chats`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      workspaceId: request.workspaceId,
      agent: request.agent ?? null,
      mode: request.mode ?? 'default',
      modelId: request.modelId ?? null,
      contextSizeId: request.contextSizeId ?? null,
      reasoningEffortId: request.reasoningEffortId ?? null,
      approvalPolicyId: request.approvalPolicyId ?? null
    })
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response, chatErrorOptions))
  }

  const created = (await response.json()) as CreateChatResponse
  return {
    id: created.id,
    title: 'New chat',
    preview: 'Start a conversation with Orchi',
    updatedAt: new Date().toISOString(),
    agentId: created.agentId,
    projectId: created.projectId,
    workspaceId: created.workspaceId,
    workspacePath: created.workspacePath,
    mode: created.mode ?? 'default',
    modelId: created.modelId ?? null,
    contextSizeId: created.contextSizeId ?? null,
    reasoningEffortId: created.reasoningEffortId ?? null,
    approvalPolicyId: created.approvalPolicyId ?? null,
    parentChatId: created.parentChatId,
    planFilePath: created.planFilePath,
    status: 'read',
    lastReadAt: null,
    messages: []
  }
}

export async function getChat(chatId: string): Promise<ChatThread> {
  const response = await fetch(`${getApiBaseUrl()}/chats/${chatId}`)
  if (!response.ok) {
    throw new Error(await readErrorMessage(response, chatErrorOptions))
  }

  const detail = (await response.json()) as ChatDetailResponse
  return mapDetail(detail)
}

export async function markChatRead(chatId: string): Promise<ChatThread> {
  const response = await fetch(`${getApiBaseUrl()}/chats/${chatId}/read`, {
    method: 'POST'
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response, chatErrorOptions))
  }

  const summary = (await response.json()) as ChatSummaryResponse
  return mapSummary(summary)
}

export type ChatStatusEventHandlers = {
  onSnapshot?: (items: Array<{ chatId: string; status: ChatThread['status'] }>) => void
  onStatus?: (payload: { chatId: string; status: ChatThread['status'] }) => void
}

export async function subscribeChatStatusEvents(
  handlers: ChatStatusEventHandlers,
  signal?: AbortSignal
): Promise<void> {
  const response = await fetch(`${getApiBaseUrl()}/chats/status/events`, {
    headers: { Accept: 'text/event-stream' },
    signal
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response, chatErrorOptions))
  }

  await readSseStream(
    response,
    (parsed) => {
      dispatchChatStatusEvent(parsed.event, parsed.data, handlers)
    },
    signal
  )
}

function parseChatStatus(value: unknown): ChatThread['status'] {
  if (value === 'inProgress' || value === 'readyForReview' || value === 'read') {
    return value
  }

  return 'read'
}

function dispatchChatStatusEvent(
  eventName: string,
  data: string,
  handlers: ChatStatusEventHandlers
): void {
  const payload = JSON.parse(data) as unknown

  if (eventName === 'snapshot' && Array.isArray(payload)) {
    handlers.onSnapshot?.(
      payload.map((item) => {
        const row = item as Record<string, unknown>
        return {
          chatId: String(row.chatId ?? ''),
          status: parseChatStatus(row.status)
        }
      })
    )
    return
  }

  if (eventName === 'status' && payload && typeof payload === 'object') {
    const row = payload as Record<string, unknown>
    handlers.onStatus?.({
      chatId: String(row.chatId ?? ''),
      status: parseChatStatus(row.status)
    })
  }
}

export async function updateChatMode(
  chatId: string,
  request: UpdateChatModeRequest
): Promise<UpdateChatModeResponse> {
  const response = await fetch(`${getApiBaseUrl()}/chats/${chatId}/mode`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request)
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response, chatErrorOptions))
  }

  return (await response.json()) as UpdateChatModeResponse
}

export async function updateChatModel(
  chatId: string,
  request: UpdateChatModelRequest
): Promise<UpdateChatModelResponse> {
  const response = await fetch(`${getApiBaseUrl()}/chats/${chatId}/model`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request)
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response, chatModelErrorOptions))
  }

  return (await response.json()) as UpdateChatModelResponse
}

export async function updateChatContextSize(
  chatId: string,
  request: UpdateChatContextSizeRequest
): Promise<UpdateChatContextSizeResponse> {
  const response = await fetch(`${getApiBaseUrl()}/chats/${chatId}/context-size`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request)
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response, chatModelErrorOptions))
  }

  return (await response.json()) as UpdateChatContextSizeResponse
}

export async function updateChatReasoningEffort(
  chatId: string,
  request: UpdateChatReasoningEffortRequest
): Promise<UpdateChatReasoningEffortResponse> {
  const response = await fetch(`${getApiBaseUrl()}/chats/${chatId}/reasoning-effort`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request)
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response, chatModelErrorOptions))
  }

  return (await response.json()) as UpdateChatReasoningEffortResponse
}

export async function updateChatApprovalPolicy(
  chatId: string,
  request: UpdateChatApprovalPolicyRequest
): Promise<UpdateChatApprovalPolicyResponse> {
  const response = await fetch(`${getApiBaseUrl()}/chats/${chatId}/approval-policy`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request)
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response, chatModelErrorOptions))
  }

  return (await response.json()) as UpdateChatApprovalPolicyResponse
}

export async function updateChatWorkspace(
  chatId: string,
  request: UpdateChatWorkspaceRequest
): Promise<UpdateChatWorkspaceResponse> {
  const response = await fetch(`${getApiBaseUrl()}/chats/${chatId}/workspace`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request)
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response, chatErrorOptions))
  }

  return (await response.json()) as UpdateChatWorkspaceResponse
}

export async function closeChat(chatId: string): Promise<void> {
  const response = await fetch(`${getApiBaseUrl()}/chats/${chatId}`, {
    method: 'DELETE'
  })

  if (response.status === 404) {
    return
  }

  if (!response.ok) {
    throw new Error(await readErrorMessage(response, chatErrorOptions))
  }
}

export async function shutdownChats(): Promise<void> {
  await fetch(`${getApiBaseUrl()}/chats/shutdown`, {
    method: 'POST'
  })
}

export async function kickOffPlan(
  parentChatId: string,
  request: KickOffPlanRequest
): Promise<KickOffPlanResponse> {
  const response = await fetch(`${getApiBaseUrl()}/chats/${parentChatId}/plans/kickoff`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request)
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response, chatErrorOptions))
  }

  return (await response.json()) as KickOffPlanResponse
}

export async function kickOffReview(
  implementationChildChatId: string
): Promise<KickOffReviewResponse> {
  const response = await fetch(
    `${getApiBaseUrl()}/chats/${implementationChildChatId}/review/kickoff`,
    {
      method: 'POST'
    }
  )

  if (!response.ok) {
    throw new Error(await readErrorMessage(response, chatErrorOptions))
  }

  return (await response.json()) as KickOffReviewResponse
}

export async function sendMessageStream(
  chatId: string,
  content: string,
  handlers: SseHandlers,
  signal?: AbortSignal
): Promise<void> {
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
    throw new Error(await readErrorMessage(response, chatErrorOptions))
  }

  await readSseStream(response, (parsed) => dispatchChatSseEvent(parsed, handlers), signal)
}

function dispatchChatSseEvent(
  parsed: { event: string; data: string },
  handlers: SseHandlers
): void {
  const payload = JSON.parse(parsed.data) as Record<string, unknown>

  switch (parsed.event) {
    case 'status':
      handlers.onStatus?.(String(payload.phase ?? 'processing'))
      break
    case 'token':
      handlers.onToken?.(String(payload.text ?? ''))
      break
    case 'tool':
      handlers.onTool?.(String(payload.label ?? 'tool'))
      break
    case 'script': {
      const stepLabel = String(payload.stepLabel ?? payload.scriptName ?? 'script')
      handlers.onScript?.(stepLabel)
      break
    }
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
