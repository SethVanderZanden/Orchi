import type { QueryClient } from '@tanstack/react-query'

import { resolveDetailCache } from '@/lib/chat/resolve-detail-cache'
import { createTokenBatcher, type TokenBatcher } from '@/lib/chat/token-batcher'
import type { ChatThread } from '@/lib/chat/types'
import { mergeChatStatus, preferChatStatus } from '@/lib/chat/prefer-chat-status'
import {
  appendParentOrchestrationMessage,
  getOrchestration,
  mapChatCreatedToThread,
  type OrchestrationEventHandlers
} from '@/lib/orchestration/orchestration-events'
import type { OrchestrationChildResponse } from '@/lib/orchestration/orchestration-state'
import { chatKeys } from '@/lib/query-keys'

export function mergeOrchestrationChildren(
  parentChat: ChatThread,
  children: OrchestrationChildResponse[],
  queryClient: QueryClient
): string[] {
  const newChildIds: string[] = []

  for (const child of children) {
    const childChat = mapChatCreatedToThread(parentChat, {
      chatId: child.chatId,
      mode: child.mode,
      planId: child.planId,
      planFilePath: child.planFilePath
    })

    const list = queryClient.getQueryData<ChatThread[]>(chatKeys.lists()) ?? []
    if (!list.some((chat) => chat.id === childChat.id)) {
      newChildIds.push(childChat.id)
    }

    queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) => {
      const existing = current.find((chat) => chat.id === childChat.id)
      if (existing) {
        return current.map((chat) =>
          chat.id === childChat.id
            ? {
                ...chat,
                status: mergeChatStatus(chat.status, childChat.status),
                planFilePath: childChat.planFilePath ?? chat.planFilePath
              }
            : chat
        )
      }

      return [childChat, ...current]
    })
  }

  return newChildIds
}

function resolveChildDetailCache(
  queryClient: QueryClient,
  childChatId: string,
  getChat: (chatId: string) => ChatThread | undefined
): ChatThread | undefined {
  return resolveDetailCache(queryClient, childChatId, getChat)
}

function resolveParentDetailCache(
  queryClient: QueryClient,
  parentChat: ChatThread,
  getChat: (chatId: string) => ChatThread | undefined
): ChatThread {
  return resolveDetailCache(queryClient, parentChat.id, getChat) ?? parentChat
}

export function createOrchestrationEventHandlers(
  parentChat: ChatThread,
  queryClient: QueryClient,
  getChat: (chatId: string) => ChatThread | undefined,
  options?: {
    onWorkflow?: OrchestrationEventHandlers['onWorkflow']
    onChatCreated?: OrchestrationEventHandlers['onChatCreated']
    onParentMessage?: OrchestrationEventHandlers['onParentMessage']
    loadChat?: (chatId: string) => Promise<ChatThread | undefined>
  }
): OrchestrationEventHandlers {
  const tokenBatchers = new Map<string, TokenBatcher>()

  const appendChildToken = (childChatId: string, text: string): void => {
    if (!text) {
      return
    }

    queryClient.setQueryData<ChatThread>(chatKeys.detail(childChatId), (current) => {
      const base = current ?? resolveChildDetailCache(queryClient, childChatId, getChat)
      if (!base) {
        return current
      }

      const messages = [...base.messages]
      const last = messages.at(-1)

      if (!last || last.role !== 'assistant') {
        const now = new Date().toISOString()
        messages.push({
          id: crypto.randomUUID(),
          role: 'assistant',
          content: text,
          createdAt: now,
          status: 'streaming'
        })
      } else {
        messages[messages.length - 1] = {
          ...last,
          content: last.content + text,
          status: 'streaming'
        }
      }

      return { ...base, messages, updatedAt: new Date().toISOString() }
    })
  }

  const getTokenBatcher = (childChatId: string): TokenBatcher => {
    let batcher = tokenBatchers.get(childChatId)
    if (!batcher) {
      batcher = createTokenBatcher((text) => appendChildToken(childChatId, text))
      tokenBatchers.set(childChatId, batcher)
    }

    return batcher
  }

  const flushChildTokens = (childChatId: string): void => {
    tokenBatchers.get(childChatId)?.flush()
    tokenBatchers.delete(childChatId)
  }

  const patchChildChatStatus = (childChatId: string, status: ChatThread['status']): void => {
    const updatedAt = new Date().toISOString()

    queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) => {
      let changed = false
      const next = current.map((chat) => {
        if (chat.id !== childChatId) {
          return chat
        }

        const nextStatus = preferChatStatus(chat.status, status)
        if (nextStatus === chat.status) {
          return chat
        }

        changed = true
        return { ...chat, status: nextStatus, updatedAt }
      })

      return changed ? next : current
    })

    queryClient.setQueryData<ChatThread>(chatKeys.detail(childChatId), (current) => {
      if (!current) {
        return current
      }

      const nextStatus = preferChatStatus(current.status, status)
      if (nextStatus === current.status) {
        return current
      }

      return { ...current, status: nextStatus, updatedAt }
    })
  }

  const ensureProcessingAssistantMessage = (childChatId: string): void => {
    queryClient.setQueryData<ChatThread>(chatKeys.detail(childChatId), (current) => {
      const base = current ?? resolveChildDetailCache(queryClient, childChatId, getChat)
      if (!base) {
        return current
      }

      const hasActiveAssistant = base.messages.some(
        (message) =>
          message.role === 'assistant' &&
          (message.status === 'processing' || message.status === 'streaming')
      )

      if (hasActiveAssistant) {
        return base.status === 'inProgress' ? base : { ...base, status: 'inProgress' }
      }

      const now = new Date().toISOString()
      return {
        ...base,
        status: 'inProgress',
        updatedAt: now,
        messages: [
          ...base.messages,
          {
            id: crypto.randomUUID(),
            role: 'assistant',
            content: '',
            createdAt: now,
            status: 'processing'
          }
        ]
      }
    })

    patchChildChatStatus(childChatId, 'inProgress')
  }

  return {
    onWorkflow: options?.onWorkflow,
    onChatCreated: (payload) => {
      const childChat = mapChatCreatedToThread(parentChat, payload)
      queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) => {
        const existing = current.find((chat) => chat.id === childChat.id)
        if (existing) {
          return current.map((chat) =>
            chat.id === childChat.id
              ? {
                  ...chat,
                  status: mergeChatStatus(chat.status, childChat.status),
                  planFilePath: childChat.planFilePath ?? chat.planFilePath
                }
              : chat
          )
        }

        return [childChat, ...current]
      })
      queryClient.setQueryData(chatKeys.detail(childChat.id), (current) => current ?? childChat)
      options?.onChatCreated?.(payload)
    },
    onParentMessage: (payload) => {
      queryClient.setQueryData<ChatThread>(chatKeys.detail(parentChat.id), (current) => {
        const base = current ?? resolveParentDetailCache(queryClient, parentChat, getChat)
        return appendParentOrchestrationMessage(base, payload)
      })
      options?.onParentMessage?.(payload)
    },
    onAgentStatus: ({ childChatId, phase }) => {
      if (phase === 'processing') {
        ensureProcessingAssistantMessage(childChatId)
      }
    },
    onAgentToken: ({ childChatId, text }) => {
      getTokenBatcher(childChatId).push(text)
    },
    onAgentThought: () => {
      // Thought rows for orchestrated child runs are shown on child chat detail cache.
    },
    onAgentTool: () => {
      // Tool rows for orchestrated child runs are shown on child chat detail cache.
    },
    onAgentDone: ({ childChatId, messageId, succeeded }) => {
      flushChildTokens(childChatId)
      queryClient.setQueryData<ChatThread>(chatKeys.detail(childChatId), (current) => {
        const base = current ?? resolveChildDetailCache(queryClient, childChatId, getChat)
        if (!base) {
          return current
        }

        const messages = [...base.messages]
        let targetIndex = messageId ? messages.findIndex((message) => message.id === messageId) : -1

        // Token streaming may assign a local id before the server messageId arrives.
        if (targetIndex === -1) {
          targetIndex = messages.findLastIndex(
            (message) =>
              message.role === 'assistant' &&
              (message.status === 'processing' || message.status === 'streaming')
          )
        }

        if (targetIndex === -1) {
          return base
        }

        messages[targetIndex] = {
          ...messages[targetIndex],
          id: messageId || messages[targetIndex].id,
          status: succeeded ? 'complete' : 'error'
        }

        return { ...base, messages }
      })

      patchChildChatStatus(childChatId, succeeded ? 'readyForReview' : 'inProgress')
      void queryClient.invalidateQueries({ queryKey: chatKeys.lists() })
      void options?.loadChat?.(childChatId)
    },
    onAgentError: ({ childChatId, message }) => {
      flushChildTokens(childChatId)
      queryClient.setQueryData<ChatThread>(chatKeys.detail(childChatId), (current) => {
        const base = current ?? resolveChildDetailCache(queryClient, childChatId, getChat)
        if (!base) {
          return current
        }

        const messages = [...base.messages]
        const last = messages.at(-1)
        if (last?.role === 'assistant') {
          messages[messages.length - 1] = {
            ...last,
            content: last.content || message,
            status: 'error'
          }
        }

        return { ...base, messages }
      })
    }
  }
}

function delay(ms: number): Promise<void> {
  return new Promise((resolve) => {
    setTimeout(resolve, ms)
  })
}

export async function hydrateOrchestrationChildrenWithRetry(
  parentChat: ChatThread,
  queryClient: QueryClient,
  options?: { attempts?: number; intervalMs?: number }
): Promise<string[]> {
  const attempts = options?.attempts ?? 3
  const intervalMs = options?.intervalMs ?? 500
  const knownChildIds = new Set(
    (queryClient.getQueryData<ChatThread[]>(chatKeys.lists()) ?? [])
      .filter((chat) => chat.parentChatId === parentChat.id)
      .map((chat) => chat.id)
  )

  const discoveredIds: string[] = []

  for (let attempt = 0; attempt < attempts; attempt += 1) {
    if (attempt > 0) {
      await delay(intervalMs)
    }

    try {
      const state = await getOrchestration(parentChat.id)
      const newIds = mergeOrchestrationChildren(parentChat, state.children, queryClient).filter(
        (id) => !knownChildIds.has(id)
      )

      for (const id of newIds) {
        knownChildIds.add(id)
        discoveredIds.push(id)
      }

      if (discoveredIds.length > 0) {
        break
      }
    } catch {
      // Retry on transient failures.
    }
  }

  return discoveredIds
}

export function maybeHydrateOrchestrationAfterChildSend(
  completedChat: ChatThread | undefined,
  queryClient: QueryClient,
  getChat: (chatId: string) => ChatThread | undefined,
  loadChat: (chatId: string) => Promise<ChatThread | undefined>
): void {
  if (
    !completedChat?.parentChatId ||
    (completedChat.mode !== 'implementation' && completedChat.mode !== 'default')
  ) {
    return
  }

  const parentChat =
    queryClient.getQueryData<ChatThread>(chatKeys.detail(completedChat.parentChatId)) ??
    getChat(completedChat.parentChatId)

  if (parentChat?.mode !== 'orchestration') {
    return
  }

  void hydrateOrchestrationChildrenWithRetry(parentChat, queryClient).then((newChildIds) => {
    for (const childId of newChildIds) {
      void loadChat(childId)
    }
  })
}
