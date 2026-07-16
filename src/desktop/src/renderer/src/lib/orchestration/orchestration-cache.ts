import type { QueryClient } from '@tanstack/react-query'

import { resolveDetailCache } from '@/lib/chat/resolve-detail-cache'
import type { ChatThread } from '@/lib/chat/types'
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
      if (current.some((chat) => chat.id === childChat.id)) {
        return current
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
  }
): OrchestrationEventHandlers {
  return {
    onWorkflow: options?.onWorkflow,
    onChatCreated: (payload) => {
      const childChat = mapChatCreatedToThread(parentChat, payload)
      queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) => {
        if (current.some((chat) => chat.id === childChat.id)) {
          return current
        }

        return [childChat, ...current]
      })
      options?.onChatCreated?.(payload)
    },
    onParentMessage: (payload) => {
      queryClient.setQueryData<ChatThread>(chatKeys.detail(parentChat.id), (current) => {
        const base = current ?? resolveParentDetailCache(queryClient, parentChat, getChat)
        return appendParentOrchestrationMessage(base, payload)
      })
    },
    onAgentToken: ({ childChatId, text }) => {
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
    },
    onAgentTool: () => {
      // Tool rows for orchestrated child runs are shown on child chat detail cache.
    },
    onAgentDone: ({ childChatId, messageId, succeeded }) => {
      queryClient.setQueryData<ChatThread>(chatKeys.detail(childChatId), (current) => {
        const base = current ?? resolveChildDetailCache(queryClient, childChatId, getChat)
        if (!base) {
          return current
        }

        const messages = [...base.messages]
        const targetIndex = messageId
          ? messages.findIndex((message) => message.id === messageId)
          : messages.findLastIndex(
              (message) =>
                message.role === 'assistant' &&
                (message.status === 'processing' || message.status === 'streaming')
            )

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
      void queryClient.invalidateQueries({ queryKey: chatKeys.lists() })
    },
    onAgentError: ({ childChatId, message }) => {
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
