import type { QueryClient } from '@tanstack/react-query'

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

    queryClient.setQueryData<ChatThread>(
      chatKeys.detail(childChat.id),
      (current) => current ?? childChat
    )
  }

  return newChildIds
}

export function createOrchestrationEventHandlers(
  parentChat: ChatThread,
  queryClient: QueryClient,
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
      queryClient.setQueryData(chatKeys.detail(childChat.id), childChat)
      options?.onChatCreated?.(payload)
    },
    onParentMessage: (payload) => {
      queryClient.setQueryData<ChatThread>(chatKeys.detail(parentChat.id), (current) =>
        current ? appendParentOrchestrationMessage(current, payload) : current
      )
    },
    onAgentToken: ({ childChatId, text }) => {
      queryClient.setQueryData<ChatThread>(chatKeys.detail(childChatId), (current) => {
        if (!current) {
          return current
        }

        const messages = [...current.messages]
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

        return { ...current, messages, updatedAt: new Date().toISOString() }
      })
    },
    onAgentTool: () => {
      // Tool rows for orchestrated child runs are shown on child chat detail cache.
    },
    onAgentDone: ({ childChatId, messageId, succeeded }) => {
      queryClient.setQueryData<ChatThread>(chatKeys.detail(childChatId), (current) => {
        if (!current) {
          return current
        }

        const messages = current.messages.map((message) =>
          message.id === messageId || message.role === 'assistant'
            ? {
                ...message,
                id: messageId || message.id,
                status: succeeded ? 'complete' : message.status
              }
            : message
        )

        return {
          ...current,
          messages: messages.map((message, index, all) =>
            index === all.length - 1 && message.role === 'assistant'
              ? { ...message, status: succeeded ? 'complete' : 'error' }
              : message
          )
        }
      })
      void queryClient.invalidateQueries({ queryKey: chatKeys.lists() })
    },
    onAgentError: ({ childChatId, message }) => {
      queryClient.setQueryData<ChatThread>(chatKeys.detail(childChatId), (current) => {
        if (!current) {
          return current
        }

        const messages = [...current.messages]
        const last = messages.at(-1)
        if (last?.role === 'assistant') {
          messages[messages.length - 1] = {
            ...last,
            content: last.content || message,
            status: 'error'
          }
        }

        return { ...current, messages }
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
