import { existsSync, mkdirSync, renameSync, rmSync } from 'fs'
import { join } from 'path'

/** Stable product profile name (package.json still says "desktop" from electron-vite). */
export const ORCHI_USER_DATA_DIR = 'Orchi'

/** Legacy electron-vite default: package.json `name` → `desktop`. */
export const LEGACY_USER_DATA_DIR = 'desktop'

/** Chromium dirs that commonly corrupt and log disk_cache "Critical error found -8". */
export const CHROMIUM_CACHE_DIRS = ['Cache', 'Code Cache', 'GPUCache'] as const

export type ChromiumProfileDeps = {
  getAppDataPath: () => string
  setAppName: (name: string) => void
  setUserDataPath: (path: string) => void
  getUserDataPath: () => string
  pathExists: (path: string) => boolean
  ensureDir: (path: string) => void
  moveFile: (from: string, to: string) => void
  removeDir: (path: string) => void
  appendSwitch: (switchName: string, value?: string) => void
  isDev: boolean
}

/**
 * Pin Orchi to a dedicated userData profile and avoid Chromium HTTP disk-cache
 * spam during Vite dev (Windows often logs Critical error found -8 / No file for …).
 * Call before `app.whenReady()`.
 */
export function configureChromiumProfile(deps: ChromiumProfileDeps): string {
  const userDataPath = join(deps.getAppDataPath(), ORCHI_USER_DATA_DIR)
  deps.setAppName(ORCHI_USER_DATA_DIR)
  deps.setUserDataPath(userDataPath)
  migrateLegacyUserDataIfNeeded(deps, userDataPath)

  if (deps.isDev) {
    // Renderer is served from localhost by Vite; HTTP disk cache is unused and is
    // a frequent source of net\disk_cache\blockfile ERROR lines on Windows.
    deps.appendSwitch('disable-http-cache')
  }

  return userDataPath
}

export function migrateLegacyUserDataIfNeeded(
  deps: Pick<ChromiumProfileDeps, 'getAppDataPath' | 'pathExists' | 'ensureDir' | 'moveFile'>,
  userDataPath: string
): void {
  const legacyPath = join(deps.getAppDataPath(), LEGACY_USER_DATA_DIR)
  if (!deps.pathExists(legacyPath) || deps.pathExists(userDataPath)) {
    return
  }

  try {
    deps.ensureDir(userDataPath)
    const legacyDb = join(legacyPath, 'orchi.db')
    const nextDb = join(userDataPath, 'orchi.db')
    if (deps.pathExists(legacyDb) && !deps.pathExists(nextDb)) {
      // Leave legacy Cache behind — it is often the corrupted part.
      deps.moveFile(legacyDb, nextDb)
    }
  } catch {
    // Best-effort; a fresh profile is preferable to failing startup.
  }
}

/**
 * Drop Chromium cache dirs before the network service opens them so a corrupt
 * blockfile index cannot log Critical error found -8.
 * Safe to call when recovering a known-bad profile (not every launch).
 */
export function recoverCorruptedHttpDiskCache(
  deps: Pick<ChromiumProfileDeps, 'getUserDataPath' | 'pathExists' | 'removeDir'>
): void {
  const userData = deps.getUserDataPath()
  for (const dir of CHROMIUM_CACHE_DIRS) {
    const cachePath = join(userData, dir)
    try {
      if (deps.pathExists(cachePath)) {
        deps.removeDir(cachePath)
      }
    } catch {
      // Ignore locked files; Chromium still attempts its own recovery.
    }
  }
}

export function createDefaultChromiumProfileDeps(options: {
  app: {
    getPath: (name: 'appData' | 'userData') => string
    setName: (name: string) => void
    setPath: (name: 'userData', path: string) => void
    commandLine: { appendSwitch: (switchName: string, value?: string) => void }
  }
  isDev: boolean
}): ChromiumProfileDeps {
  const { app, isDev } = options
  return {
    getAppDataPath: () => app.getPath('appData'),
    setAppName: (name) => app.setName(name),
    setUserDataPath: (path) => app.setPath('userData', path),
    getUserDataPath: () => app.getPath('userData'),
    pathExists: existsSync,
    ensureDir: (path) => mkdirSync(path, { recursive: true }),
    moveFile: (from, to) => renameSync(from, to),
    removeDir: (path) => rmSync(path, { recursive: true, force: true }),
    appendSwitch: (switchName, value) => app.commandLine.appendSwitch(switchName, value),
    isDev
  }
}
