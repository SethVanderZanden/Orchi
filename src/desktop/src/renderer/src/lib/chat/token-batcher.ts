/**
 * Coalesces streaming token fragments into one flush per animation frame.
 * Without this, a single SSE read can apply dozens of setQueryData updates
 * synchronously and starve input (clicks, shortcuts) while CSS keeps animating.
 */
export type TokenBatcher = {
  push: (text: string) => void
  /** Apply any pending text immediately (call before done/error). */
  flush: () => void
  /** Drop pending text and cancel a scheduled flush. */
  cancel: () => void
}

export function createTokenBatcher(onFlush: (text: string) => void): TokenBatcher {
  let pending = ''
  let frameId: number | null = null

  const releaseFrame = (): void => {
    if (frameId !== null) {
      cancelAnimationFrame(frameId)
      frameId = null
    }
  }

  const flushPending = (): void => {
    if (!pending) {
      return
    }

    const text = pending
    pending = ''
    onFlush(text)
  }

  return {
    push(text) {
      if (!text) {
        return
      }

      pending += text
      if (frameId !== null) {
        return
      }

      frameId = requestAnimationFrame(() => {
        frameId = null
        flushPending()
      })
    },
    flush() {
      releaseFrame()
      flushPending()
    },
    cancel() {
      releaseFrame()
      pending = ''
    }
  }
}
