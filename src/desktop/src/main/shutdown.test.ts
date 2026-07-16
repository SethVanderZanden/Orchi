import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import {
  BeforeQuitState,
  SHUTDOWN_SESSION_TIMEOUT_MS,
  SHUTDOWN_TOTAL_TIMEOUT_MS,
  handleBeforeQuit,
  shutdownApiSessions,
  shutdownGracefully,
  type ShutdownDeps
} from './shutdown'

function createHungFetchShutdown(): ShutdownDeps['fetchShutdown'] {
  return (_url, signal) =>
    new Promise<void>((_resolve, reject) => {
      if (signal.aborted) {
        reject(new DOMException('Aborted', 'AbortError'))
        return
      }

      signal.addEventListener('abort', () => {
        reject(new DOMException('Aborted', 'AbortError'))
      })
    })
}

function createDeps(overrides: Partial<ShutdownDeps> = {}): ShutdownDeps {
  return {
    isDev: false,
    getApiBaseUrl: () => 'http://127.0.0.1:5265',
    stopApiHost: vi.fn().mockResolvedValue(undefined),
    sleep: vi.fn().mockImplementation((ms) => new Promise((resolve) => setTimeout(resolve, ms))),
    fetchShutdown: vi.fn().mockResolvedValue(undefined),
    ...overrides
  }
}

describe('handleBeforeQuit', () => {
  it('allows exit when shutdown is completed', () => {
    const event = { preventDefault: vi.fn() }
    const state = { current: BeforeQuitState.Completed }

    handleBeforeQuit(event, state, createDeps(), vi.fn())

    expect(event.preventDefault).not.toHaveBeenCalled()
  })

  it('blocks duplicate quit while shutdown is in progress', () => {
    const event = { preventDefault: vi.fn() }
    const state = { current: BeforeQuitState.InProgress }

    handleBeforeQuit(event, state, createDeps(), vi.fn())

    expect(event.preventDefault).toHaveBeenCalledTimes(1)
  })

  it('starts graceful shutdown and completes before allowing exit', async () => {
    const event = { preventDefault: vi.fn() }
    const state = { current: BeforeQuitState.NotStarted }
    const onComplete = vi.fn()
    const stopApiHost = vi.fn().mockResolvedValue(undefined)
    const deps = createDeps({ stopApiHost })

    handleBeforeQuit(event, state, deps, onComplete)

    expect(event.preventDefault).toHaveBeenCalledTimes(1)
    expect(state.current).toBe(BeforeQuitState.InProgress)

    await vi.waitFor(() => {
      expect(state.current).toBe(BeforeQuitState.Completed)
    })

    expect(deps.fetchShutdown).toHaveBeenCalledWith(
      'http://127.0.0.1:5265/chats/shutdown',
      expect.any(AbortSignal)
    )
    expect(stopApiHost).toHaveBeenCalledTimes(1)
    expect(onComplete).toHaveBeenCalledTimes(1)
  })

  it('still stops the api host when a second quit arrives during shutdown', async () => {
    const event = { preventDefault: vi.fn() }
    const state = { current: BeforeQuitState.NotStarted }
    let resolveFetch: (() => void) | undefined
    const stopApiHost = vi.fn().mockResolvedValue(undefined)
    const deps = createDeps({
      stopApiHost,
      fetchShutdown: vi.fn(
        () =>
          new Promise<void>((resolve) => {
            resolveFetch = resolve
          })
      )
    })

    handleBeforeQuit(event, state, deps, vi.fn())

    const duplicateEvent = { preventDefault: vi.fn() }
    handleBeforeQuit(duplicateEvent, state, deps, vi.fn())

    expect(duplicateEvent.preventDefault).toHaveBeenCalledTimes(1)
    expect(stopApiHost).not.toHaveBeenCalled()

    resolveFetch?.()
    await vi.waitFor(() => {
      expect(stopApiHost).toHaveBeenCalledTimes(1)
    })
  })
})

describe('shutdownApiSessions', () => {
  beforeEach(() => {
    vi.useFakeTimers()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('times out a hung shutdown request', async () => {
    const fetchShutdown = vi.fn(createHungFetchShutdown())
    const deps = createDeps({ fetchShutdown })

    const shutdownPromise = shutdownApiSessions(deps)
    await vi.advanceTimersByTimeAsync(SHUTDOWN_SESSION_TIMEOUT_MS)
    await shutdownPromise

    expect(fetchShutdown).toHaveBeenCalledTimes(1)
  })
})

describe('shutdownGracefully', () => {
  beforeEach(() => {
    vi.useFakeTimers()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('still stops the api host after the session timeout', async () => {
    const stopApiHost = vi.fn().mockResolvedValue(undefined)
    const deps = createDeps({
      stopApiHost,
      fetchShutdown: vi.fn(createHungFetchShutdown())
    })

    const shutdownPromise = shutdownGracefully(deps)
    await vi.advanceTimersByTimeAsync(SHUTDOWN_SESSION_TIMEOUT_MS)
    await shutdownPromise

    expect(stopApiHost).toHaveBeenCalledTimes(1)
  })

  it('completes after the total shutdown deadline even when cleanup hangs', async () => {
    const stopApiHost = vi.fn(
      () =>
        new Promise<void>(() => {
          // Never resolves.
        })
    )
    const deps = createDeps({
      stopApiHost,
      sleep: (ms) => new Promise((resolve) => setTimeout(resolve, ms))
    })

    const shutdownPromise = shutdownGracefully(deps)
    await vi.advanceTimersByTimeAsync(SHUTDOWN_TOTAL_TIMEOUT_MS)
    await shutdownPromise

    expect(deps.fetchShutdown).toHaveBeenCalledTimes(1)
    expect(stopApiHost).toHaveBeenCalledTimes(1)
  })

  it('skips api host stop in dev mode', async () => {
    const stopApiHost = vi.fn().mockResolvedValue(undefined)
    const deps = createDeps({ isDev: true, stopApiHost })

    await shutdownGracefully(deps)

    expect(stopApiHost).not.toHaveBeenCalled()
  })
})
