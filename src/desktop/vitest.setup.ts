import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { afterEach, vi } from 'vitest'

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
