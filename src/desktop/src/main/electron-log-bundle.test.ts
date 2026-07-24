import { execSync } from 'node:child_process'
import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { describe, expect, it, beforeAll } from 'vitest'

const desktopRoot = resolve(process.cwd())
const electronViteConfigPath = resolve(desktopRoot, 'electron.vite.config.ts')
const mainBundlePath = resolve(desktopRoot, 'out/main/index.js')

describe('electron-log main-process packaging', () => {
  it('excludes electron-log from main-process dependency externalization', () => {
    const configSource = readFileSync(electronViteConfigPath, 'utf8')

    expect(configSource).toMatch(/externalizeDeps:\s*\{[\s\S]*exclude:\s*\[[^\]]*['"]electron-log['"]/)
  })

  describe('built main bundle', () => {
    beforeAll(() => {
      execSync('npx electron-vite build', {
        cwd: desktopRoot,
        stdio: 'pipe'
      })
    }, 120_000)

    it('bundles electron-log instead of requiring it at runtime', () => {
      const source = readFileSync(mainBundlePath, 'utf8')

      expect(source).not.toMatch(/require\(["']electron-log/)
    })
  })
})
