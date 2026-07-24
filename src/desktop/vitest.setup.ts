import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { afterEach, vi } from 'vitest'

vi.mock('electron-log/renderer', () => {
  const logger = {
    error: vi.fn(),
    warn: vi.fn(),
    info: vi.fn(),
    debug: vi.fn(),
    verbose: vi.fn(),
    silly: vi.fn(),
    log: vi.fn(),
    errorHandler: {
      startCatching: vi.fn()
    }
  }
  return { default: logger }
})

afterEach(() => {
  cleanup()
})

Object.defineProperty(window, 'electron', {
  value: {
    process: { versions: {} },
    ipcRenderer: { invoke: vi.fn() }
  },
  writable: true
})

Object.defineProperty(window, 'api', {
  value: {
    openDirectory: vi.fn(),
    openInEditor: vi.fn(),
    getLogPath: vi.fn().mockResolvedValue('/tmp/orchi.log'),
    openLogFolder: vi.fn().mockResolvedValue({
      logFile: '/tmp/orchi.log',
      logDirectory: '/tmp'
    })
  },
  writable: true
})
