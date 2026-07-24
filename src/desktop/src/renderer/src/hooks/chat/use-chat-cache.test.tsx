import { describe, expect, it } from 'vitest'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { renderHook } from '@testing-library/react'
import type { ReactNode } from 'react'

import { useChatCache } from '@/hooks/chat/use-chat-cache'
import type { ChatThread } from '@/lib/chat/types'
import { chatKeys } from '@/lib/query-keys'

function createChat(overrides: Partial<ChatThread> = {}): ChatThread {
  return {
    id: 'parent-id',
    title: 'Parent',
    preview: 'Preview',
    updatedAt: '2026-07-04T12:00:00.000Z',
    agentId: 'cursor',
    projectId: 'project-1',
    workspaceId: 'workspace-1',
    workspacePath: '/workspace',
    mode: 'orchestration',
    modelId: null,
    contextSizeId: null,
    reasoningEffortId: null,
    approvalPolicyId: null,
    parentChatId: null,
    planFilePath: null,
    status: 'read',
    lastReadAt: null,
    messages: [],
    ...overrides
  }
}

describe('useChatCache', () => {
  it('reads child chats from the live query cache before hook props refresh', () => {
    const queryClient = new QueryClient()
    const parent = createChat()
    const staleChats = [parent]

    const wrapper = ({ children }: { children: ReactNode }) => (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    )

    queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), staleChats)

    const { result, rerender } = renderHook(({ chats }) => useChatCache({ chats }), {
      initialProps: { chats: staleChats },
      wrapper
    })

    const child = createChat({
      id: 'child-id',
      parentChatId: parent.id,
      mode: 'implementation',
      planFilePath: '.orchi/plan-auth.md',
      title: 'Auth'
    })

    queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), [child, parent])

    expect(result.current.getChildChats(parent.id)).toEqual([child])
    expect(result.current.getChat(child.id)?.id).toBe(child.id)

    rerender({ chats: [child, parent] })

    expect(result.current.getChildChats(parent.id)).toEqual([child])
  })
})
