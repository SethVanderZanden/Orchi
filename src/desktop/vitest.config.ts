import { resolve } from 'path'
import { defineConfig } from 'vitest/config'

export default defineConfig({
  resolve: {
    alias: {
      '@': resolve(__dirname, 'src/renderer/src')
    }
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./vitest.setup.ts'],
    include: ['src/**/*.{test,spec}.{ts,tsx}'],
    coverage: {
      provider: 'v8',
      include: ['src/renderer/src/**/*.{ts,tsx}'],
      exclude: ['**/*.test.{ts,tsx}', '**/routeTree.gen.ts', '**/*.d.ts'],
      thresholds: {
        lines: 10,
        functions: 45,
        branches: 60
      }
    }
  }
})
