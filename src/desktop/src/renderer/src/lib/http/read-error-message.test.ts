import { describe, expect, it } from 'vitest'

import { formatChatModeUpdateError, readErrorMessage } from './read-error-message'

function jsonResponse(body: unknown, status = 400): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' }
  })
}

function textResponse(body: string, status = 500): Response {
  return new Response(body, { status })
}

describe('readErrorMessage', () => {
  it('returns ProblemDetails detail', async () => {
    const message = await readErrorMessage(jsonResponse({ detail: 'Not found', title: '404' }, 404))

    expect(message).toBe('Not found')
  })

  it('returns first validation error from errors map', async () => {
    const message = await readErrorMessage(jsonResponse({ errors: { mode: ['Invalid'] } }))

    expect(message).toBe('Invalid')
  })

  it('applies formatMessage for Mode.Busy', async () => {
    const message = await readErrorMessage(
      jsonResponse({ detail: 'Mode.Busy: agent is running', title: 'Mode.Busy' }),
      { formatMessage: formatChatModeUpdateError }
    )

    expect(message).toBe('Wait for the agent to finish before changing mode.')
  })

  it('falls back when body is not JSON', async () => {
    const message = await readErrorMessage(textResponse('<html>error</html>'))

    expect(message).toBe('API error: 500')
  })

  it('returns message field when detail and errors are absent', async () => {
    const message = await readErrorMessage(jsonResponse({ message: 'Something failed' }))

    expect(message).toBe('Something failed')
  })
})
