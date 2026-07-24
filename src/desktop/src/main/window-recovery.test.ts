import { describe, expect, it, vi } from 'vitest'

vi.mock('./logging', () => ({
  log: {
    error: vi.fn(),
    warn: vi.fn(),
    info: vi.fn()
  }
}))

import { attachWindowRecovery } from './window-recovery'

type HandlerMap = Map<string, (...args: unknown[]) => void>

function createFakeWindow(): {
  window: {
    isDestroyed: () => boolean
    webContents: {
      on: (event: string, handler: (...args: unknown[]) => void) => void
      off: (event: string, handler: (...args: unknown[]) => void) => void
      emit: (event: string, ...args: unknown[]) => void
    }
  }
  handlers: HandlerMap
} {
  const handlers: HandlerMap = new Map()

  const window = {
    isDestroyed: () => false,
    webContents: {
      on(event: string, handler: (...args: unknown[]) => void): void {
        handlers.set(event, handler)
      },
      off(event: string, handler: (...args: unknown[]) => void): void {
        if (handlers.get(event) === handler) {
          handlers.delete(event)
        }
      },
      emit(event: string, ...args: unknown[]): void {
        handlers.get(event)?.(...args)
      }
    }
  }

  return { window, handlers }
}

describe('attachWindowRecovery', () => {
  it('reloads after render-process-gone', () => {
    const { window } = createFakeWindow()
    const reload = vi.fn()
    const showRecoveryFailedDialog = vi.fn()

    attachWindowRecovery(window as never, { reload, showRecoveryFailedDialog })

    window.webContents.emit('render-process-gone', {}, { reason: 'crashed', exitCode: 1 })

    expect(reload).toHaveBeenCalledTimes(1)
    expect(showRecoveryFailedDialog).not.toHaveBeenCalled()
  })

  it('ignores clean-exit', () => {
    const { window } = createFakeWindow()
    const reload = vi.fn()

    attachWindowRecovery(window as never, { reload })

    window.webContents.emit('render-process-gone', {}, { reason: 'clean-exit', exitCode: 0 })

    expect(reload).not.toHaveBeenCalled()
  })

  it('stops reloading after the max attempts and shows a dialog', () => {
    const { window } = createFakeWindow()
    const reload = vi.fn()
    const showRecoveryFailedDialog = vi.fn().mockResolvedValue(undefined)

    attachWindowRecovery(window as never, {
      maxReloads: 2,
      reloadWindowMs: 60_000,
      reload,
      showRecoveryFailedDialog
    })

    window.webContents.emit('render-process-gone', {}, { reason: 'oom', exitCode: -1 })
    window.webContents.emit('render-process-gone', {}, { reason: 'oom', exitCode: -1 })
    window.webContents.emit('render-process-gone', {}, { reason: 'oom', exitCode: -1 })

    expect(reload).toHaveBeenCalledTimes(2)
    expect(showRecoveryFailedDialog).toHaveBeenCalledTimes(1)
    expect(showRecoveryFailedDialog).toHaveBeenCalledWith(window, 'oom')
  })

  it('detaches listeners on dispose', () => {
    const { window, handlers } = createFakeWindow()

    const dispose = attachWindowRecovery(window as never, { reload: vi.fn() })
    expect(handlers.has('render-process-gone')).toBe(true)

    dispose()
    expect(handlers.has('render-process-gone')).toBe(false)
  })
})
