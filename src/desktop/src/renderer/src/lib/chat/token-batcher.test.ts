import { afterEach, describe, expect, it, vi } from 'vitest'

import { createTokenBatcher } from './token-batcher'

describe('createTokenBatcher', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('coalesces multiple pushes into one flush per animation frame', () => {
    const rafCallbacks: FrameRequestCallback[] = []
    vi.stubGlobal(
      'requestAnimationFrame',
      vi.fn((callback: FrameRequestCallback) => {
        rafCallbacks.push(callback)
        return rafCallbacks.length
      })
    )
    vi.stubGlobal(
      'cancelAnimationFrame',
      vi.fn((id: number) => {
        rafCallbacks[id - 1] = () => {}
      })
    )

    const onFlush = vi.fn()
    const batcher = createTokenBatcher(onFlush)

    batcher.push('Hel')
    batcher.push('lo')
    batcher.push('!')

    expect(onFlush).not.toHaveBeenCalled()
    expect(requestAnimationFrame).toHaveBeenCalledTimes(1)

    rafCallbacks[0](0)

    expect(onFlush).toHaveBeenCalledTimes(1)
    expect(onFlush).toHaveBeenCalledWith('Hello!')
  })

  it('flush applies pending text immediately and cancels the frame', () => {
    const rafCallbacks: FrameRequestCallback[] = []
    vi.stubGlobal(
      'requestAnimationFrame',
      vi.fn((callback: FrameRequestCallback) => {
        rafCallbacks.push(callback)
        return 1
      })
    )
    const cancel = vi.fn()
    vi.stubGlobal('cancelAnimationFrame', cancel)

    const onFlush = vi.fn()
    const batcher = createTokenBatcher(onFlush)

    batcher.push('partial')
    batcher.flush()

    expect(cancel).toHaveBeenCalledWith(1)
    expect(onFlush).toHaveBeenCalledWith('partial')

    rafCallbacks[0]?.(0)
    expect(onFlush).toHaveBeenCalledTimes(1)
  })

  it('cancel drops pending text without flushing', () => {
    vi.stubGlobal(
      'requestAnimationFrame',
      vi.fn((callback: FrameRequestCallback) => {
        void callback
        return 1
      })
    )
    vi.stubGlobal('cancelAnimationFrame', vi.fn())

    const onFlush = vi.fn()
    const batcher = createTokenBatcher(onFlush)

    batcher.push('gone')
    batcher.cancel()
    batcher.flush()

    expect(onFlush).not.toHaveBeenCalled()
  })
})
