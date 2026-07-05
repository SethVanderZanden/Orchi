import { describe, expect, it } from 'vitest'

import type { ChatMessage, ChatThread } from '@/lib/chat/types'

import {
  appendUserAndAssistantMessages,
  applyToken,
  finalizeAssistantMessages,
  updateMessageInThread
} from './message-updates'

function makeChat(overrides: Partial<ChatThread> = {}): ChatThread {
  return {
    id: 'chat-1',
    title: 'Test',
    preview: '',
    updatedAt: '2026-01-01T00:00:00.000Z',
    agentId: 'cursor',
    projectId: null,
    workspaceId: null,
    workspacePath: '/workspace',
    mode: 'default',
    modelId: null,
    parentChatId: null,
    planFilePath: null,
    messages: [],
    ...overrides
  }
}

function makeMessage(overrides: Partial<ChatMessage> & Pick<ChatMessage, 'id'>): ChatMessage {
  return {
    role: 'assistant',
    content: '',
    createdAt: '2026-01-01T00:00:00.000Z',
    status: 'processing',
    ...overrides
  }
}

describe('appendUserAndAssistantMessages', () => {
  it('appends user and assistant messages and sets title from first message', () => {
    const chat = makeChat()
    const result = appendUserAndAssistantMessages(chat, 'Hello world', 'assistant-1')

    expect(result.title).toBe('Hello world')
    expect(result.messages).toHaveLength(2)
    expect(result.messages[0].role).toBe('user')
    expect(result.messages[0].content).toBe('Hello world')
    expect(result.messages[1].id).toBe('assistant-1')
    expect(result.messages[1].status).toBe('processing')
  })

  it('preserves existing title when chat already has messages', () => {
    const chat = makeChat({
      title: 'Existing',
      messages: [makeMessage({ id: 'old', role: 'user', content: 'Hi', status: 'complete' })]
    })

    const result = appendUserAndAssistantMessages(chat, 'Follow up', 'assistant-1')

    expect(result.title).toBe('Existing')
  })
})

describe('applyToken', () => {
  it('appends text and marks message as streaming', () => {
    const message = makeMessage({ id: 'a', content: 'Hel' })

    const result = applyToken(message, 'lo')

    expect(result.content).toBe('Hello')
    expect(result.status).toBe('streaming')
  })
})

describe('finalizeAssistantMessages', () => {
  it('marks processing and streaming assistant messages complete', () => {
    const messages = [
      makeMessage({ id: 'a', status: 'processing' }),
      makeMessage({ id: 'b', status: 'streaming' }),
      makeMessage({ id: 'c', status: 'complete' })
    ]

    const { messages: next, changed } = finalizeAssistantMessages(messages)

    expect(changed).toBe(true)
    expect(next[0].status).toBe('complete')
    expect(next[1].status).toBe('complete')
    expect(next[2].status).toBe('complete')
  })

  it('returns unchanged when no assistant messages need finalizing', () => {
    const messages = [makeMessage({ id: 'a', status: 'complete' })]

    const { changed } = finalizeAssistantMessages(messages)

    expect(changed).toBe(false)
  })
})

describe('updateMessageInThread', () => {
  it('updates a message by id', () => {
    const chat = makeChat({
      messages: [makeMessage({ id: 'a', content: 'Hi' })]
    })

    const result = updateMessageInThread(chat, 'a', (message) => ({
      ...message,
      content: 'Hello',
      status: 'complete'
    }))

    expect(result.messages[0].content).toBe('Hello')
    expect(result.preview).toBe('Hello')
  })

  it('returns the original chat when the message id is missing', () => {
    const chat = makeChat()

    expect(updateMessageInThread(chat, 'missing', (message) => message)).toBe(chat)
  })
})
