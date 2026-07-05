import { describe, expect, it } from 'vitest'

import { parseSseBlock, readSseStream } from './sse'

describe('parseSseBlock', () => {
  it('parses a single event', () => {
    expect(parseSseBlock('event: token\ndata: hello\n\n')).toEqual({
      event: 'token',
      data: 'hello'
    })
  })

  it('joins multiline data per SSE spec', () => {
    expect(parseSseBlock('data: line1\ndata: line2')).toEqual({
      event: 'message',
      data: 'line1\nline2'
    })
  })

  it('returns null for empty blocks', () => {
    expect(parseSseBlock('\n\n')).toBeNull()
    expect(parseSseBlock('')).toBeNull()
  })

  it('returns null for malformed blocks without data', () => {
    expect(parseSseBlock('not sse')).toBeNull()
  })

  it('skips comment lines', () => {
    expect(parseSseBlock(': keep-alive\nevent: ping\ndata: ok')).toEqual({
      event: 'ping',
      data: 'ok'
    })
  })

  it('normalizes CRLF line endings', () => {
    expect(parseSseBlock('event: token\r\ndata: hello\r\n')).toEqual({
      event: 'token',
      data: 'hello'
    })
  })
})

describe('readSseStream', () => {
  it('invokes onEvent for each parsed block', async () => {
    const body = 'event: token\ndata: hello\n\nevent: done\ndata: {}\n\n'
    const response = new Response(body, {
      headers: { 'Content-Type': 'text/event-stream' }
    })

    const events: Array<{ event: string; data: string }> = []
    await readSseStream(response, (event) => {
      events.push(event)
    })

    expect(events).toEqual([
      { event: 'token', data: 'hello' },
      { event: 'done', data: '{}' }
    ])
  })

  it('throws when response body is missing', async () => {
    const response = new Response(null)

    await expect(readSseStream(response, () => undefined)).rejects.toThrow(
      'Streaming response body was empty.'
    )
  })
})
