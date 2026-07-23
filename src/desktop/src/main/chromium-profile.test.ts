import { join } from 'path'
import { describe, expect, it } from 'vitest'
import {
  CHROMIUM_CACHE_DIRS,
  LEGACY_USER_DATA_DIR,
  ORCHI_USER_DATA_DIR,
  configureChromiumProfile,
  migrateLegacyUserDataIfNeeded,
  recoverCorruptedHttpDiskCache,
  type ChromiumProfileDeps
} from './chromium-profile'

type TestDeps = ChromiumProfileDeps & {
  readonly paths: Set<string>
  readonly moved: Array<{ from: string; to: string }>
  readonly removed: string[]
  readonly switches: string[]
  readonly getAppName: () => string | null
  readonly getConfiguredUserDataPath: () => string | null
}

function createDeps(overrides: Partial<ChromiumProfileDeps> = {}): TestDeps {
  const paths = new Set<string>()
  const moved: Array<{ from: string; to: string }> = []
  const removed: string[] = []
  const switches: string[] = []
  let appName: string | null = null
  let userDataPath: string | null = null

  const deps: TestDeps = {
    paths,
    moved,
    removed,
    switches,
    getAppName: () => appName,
    getConfiguredUserDataPath: () => userDataPath,
    getAppDataPath: () => '/app-data',
    setAppName: (name) => {
      appName = name
    },
    setUserDataPath: (path) => {
      userDataPath = path
    },
    getUserDataPath: () => userDataPath ?? join('/app-data', ORCHI_USER_DATA_DIR),
    pathExists: (path) => paths.has(path),
    ensureDir: (path) => {
      paths.add(path)
    },
    moveFile: (from, to) => {
      moved.push({ from, to })
      paths.delete(from)
      paths.add(to)
    },
    removeDir: (path) => {
      removed.push(path)
      paths.delete(path)
    },
    appendSwitch: (switchName) => {
      switches.push(switchName)
    },
    isDev: false,
    ...overrides
  }

  return deps
}

describe('configureChromiumProfile', () => {
  it('pins userData to Orchi and disables HTTP disk cache in development', () => {
    const deps = createDeps({ isDev: true })

    const path = configureChromiumProfile(deps)

    expect(path).toBe(join('/app-data', ORCHI_USER_DATA_DIR))
    expect(deps.getAppName()).toBe(ORCHI_USER_DATA_DIR)
    expect(deps.getConfiguredUserDataPath()).toBe(path)
    expect(deps.switches).toEqual(['disable-http-cache'])
    expect(deps.removed).toEqual([])
  })

  it('does not disable HTTP cache or wipe caches in production', () => {
    const deps = createDeps({ isDev: false })
    deps.paths.add(join('/app-data', ORCHI_USER_DATA_DIR, 'Cache'))

    configureChromiumProfile(deps)

    expect(deps.switches).toEqual([])
    expect(deps.removed).toEqual([])
  })
})

describe('migrateLegacyUserDataIfNeeded', () => {
  it('moves orchi.db from legacy desktop profile and leaves Cache behind', () => {
    const deps = createDeps()
    const legacyRoot = join('/app-data', LEGACY_USER_DATA_DIR)
    const nextRoot = join('/app-data', ORCHI_USER_DATA_DIR)
    deps.paths.add(legacyRoot)
    deps.paths.add(join(legacyRoot, 'orchi.db'))
    deps.paths.add(join(legacyRoot, 'Cache'))

    migrateLegacyUserDataIfNeeded(deps, nextRoot)

    expect(deps.moved).toEqual([
      {
        from: join(legacyRoot, 'orchi.db'),
        to: join(nextRoot, 'orchi.db')
      }
    ])
    expect(deps.paths.has(join(legacyRoot, 'Cache'))).toBe(true)
    expect(deps.paths.has(join(nextRoot, 'orchi.db'))).toBe(true)
  })

  it('no-ops when Orchi profile already exists', () => {
    const deps = createDeps()
    const legacyRoot = join('/app-data', LEGACY_USER_DATA_DIR)
    const nextRoot = join('/app-data', ORCHI_USER_DATA_DIR)
    deps.paths.add(legacyRoot)
    deps.paths.add(nextRoot)
    deps.paths.add(join(legacyRoot, 'orchi.db'))

    migrateLegacyUserDataIfNeeded(deps, nextRoot)

    expect(deps.moved).toEqual([])
  })
})

describe('recoverCorruptedHttpDiskCache', () => {
  it('removes known Chromium cache directories under userData', () => {
    const deps = createDeps()
    const userData = join('/app-data', ORCHI_USER_DATA_DIR)
    for (const dir of CHROMIUM_CACHE_DIRS) {
      deps.paths.add(join(userData, dir))
    }
    deps.paths.add(join(userData, 'orchi.db'))

    recoverCorruptedHttpDiskCache(deps)

    expect(deps.removed).toEqual(CHROMIUM_CACHE_DIRS.map((dir) => join(userData, dir)))
    expect(deps.paths.has(join(userData, 'orchi.db'))).toBe(true)
  })
})
