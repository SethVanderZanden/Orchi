export enum BeforeQuitState {
  NotStarted = 'not_started',
  InProgress = 'in_progress',
  Completed = 'completed'
}

export const SHUTDOWN_SESSION_TIMEOUT_MS = 10_000
export const SHUTDOWN_TOTAL_TIMEOUT_MS = 15_000

export interface ShutdownDeps {
  isDev: boolean
  getApiBaseUrl: () => string
  stopApiHost: () => Promise<void>
  sleep: (ms: number) => Promise<void>
  fetchShutdown: (url: string, signal: AbortSignal) => Promise<void>
}

export function createDefaultShutdownDeps(options: {
  isDev: boolean
  getApiBaseUrl: () => string
  stopApiHost: () => Promise<void>
}): ShutdownDeps {
  return {
    isDev: options.isDev,
    getApiBaseUrl: options.getApiBaseUrl,
    stopApiHost: options.stopApiHost,
    sleep: (ms) => new Promise((resolve) => setTimeout(resolve, ms)),
    fetchShutdown: async (url, signal) => {
      await fetch(url, { method: 'POST', signal })
    }
  }
}

export async function shutdownApiSessions(deps: ShutdownDeps): Promise<void> {
  const controller = new AbortController()
  const timeout = setTimeout(() => controller.abort(), SHUTDOWN_SESSION_TIMEOUT_MS)

  try {
    await deps.fetchShutdown(`${deps.getApiBaseUrl()}/chats/shutdown`, controller.signal)
  } catch {
    // Best-effort cleanup when the API is unavailable or slow.
  } finally {
    clearTimeout(timeout)
  }
}

export async function shutdownGracefully(deps: ShutdownDeps): Promise<void> {
  await Promise.race([
    (async () => {
      await shutdownApiSessions(deps)
      if (!deps.isDev) {
        await deps.stopApiHost()
      }
    })(),
    deps.sleep(SHUTDOWN_TOTAL_TIMEOUT_MS)
  ])
}

export function handleBeforeQuit(
  event: { preventDefault: () => void },
  state: { current: BeforeQuitState },
  deps: ShutdownDeps,
  onComplete: () => void
): void {
  switch (state.current) {
    case BeforeQuitState.Completed:
      return
    case BeforeQuitState.InProgress:
      event.preventDefault()
      return
    case BeforeQuitState.NotStarted:
      event.preventDefault()
      state.current = BeforeQuitState.InProgress
      void shutdownGracefully(deps).finally(() => {
        state.current = BeforeQuitState.Completed
        onComplete()
      })
  }
}
