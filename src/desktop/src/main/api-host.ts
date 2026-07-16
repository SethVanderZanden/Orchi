import { app } from 'electron'
import { execSync, spawn, type ChildProcess } from 'child_process'
import { existsSync } from 'fs'
import { join } from 'path'

const DEFAULT_PORT = '5265'
const STOP_SIGKILL_ESCALATION_MS = 3_000
const STOP_HARD_DEADLINE_MS = 5_000

let apiProcess: ChildProcess | null = null
let apiProcessPid: number | null = null
let exitHandlerRegistered = false

export function getApiBaseUrl(): string {
  if (resolvedApiBaseUrl) {
    return resolvedApiBaseUrl
  }

  if (process.env.ORCHI_API_URL) {
    return process.env.ORCHI_API_URL.replace(/\/$/, '')
  }

  const port = process.env.ORCHI_RUNTIME_PORT ?? DEFAULT_PORT
  return `http://127.0.0.1:${port}`
}

let resolvedApiBaseUrl: string | null = null

function resolveApiExecutable(): string {
  const apiName = process.platform === 'win32' ? 'Orchi.Api.exe' : 'Orchi.Api'
  return join(process.resourcesPath, 'api', apiName)
}

function killProcessTreeSync(pid: number): void {
  if (process.platform === 'win32') {
    try {
      execSync(`taskkill /PID ${pid} /T /F`, { stdio: 'ignore', windowsHide: true })
    } catch {
      // Process may already be gone.
    }
    return
  }

  try {
    process.kill(pid, 'SIGKILL')
  } catch {
    // Process may already be gone.
  }
}

function registerExitHandler(): void {
  if (exitHandlerRegistered) {
    return
  }

  exitHandlerRegistered = true
  process.on('exit', () => {
    killApiHostSync()
  })
}

async function waitForHealth(url: string, timeoutMs: number): Promise<void> {
  const deadline = Date.now() + timeoutMs

  while (Date.now() < deadline) {
    try {
      const response = await fetch(url)
      if (response.ok) {
        return
      }
    } catch {
      // API still starting.
    }

    await new Promise((resolve) => setTimeout(resolve, 250))
  }

  throw new Error(`API health check timed out: ${url}`)
}

export async function startApiHost(): Promise<void> {
  registerExitHandler()

  const port = process.env.ORCHI_RUNTIME_PORT ?? DEFAULT_PORT
  resolvedApiBaseUrl = `http://127.0.0.1:${port}`

  const apiExecutable = resolveApiExecutable()
  if (!existsSync(apiExecutable)) {
    throw new Error(`Bundled API not found: ${apiExecutable}`)
  }

  const databasePath = join(app.getPath('userData'), 'orchi.db')

  apiProcess = spawn(apiExecutable, [], {
    env: {
      ...process.env,
      ASPNETCORE_URLS: `http://127.0.0.1:${port}`,
      ConnectionStrings__DefaultConnection: `Data Source=${databasePath}`,
      ASPNETCORE_ENVIRONMENT: 'Production'
    },
    stdio: 'ignore',
    windowsHide: true
  })

  apiProcessPid = apiProcess.pid ?? null

  apiProcess.on('error', (error) => {
    console.error('API process error:', error)
  })

  apiProcess.on('exit', () => {
    apiProcess = null
    apiProcessPid = null
  })

  await waitForHealth(`${resolvedApiBaseUrl}/health`, 30_000)
}

export function killApiHostSync(): void {
  const pid = apiProcessPid
  if (pid === null) {
    return
  }

  apiProcess = null
  apiProcessPid = null
  killProcessTreeSync(pid)
}

export async function stopApiHost(): Promise<void> {
  if (apiProcessPid === null && apiProcess === null) {
    return
  }

  const processToStop = apiProcess
  const stopPid = apiProcessPid ?? processToStop?.pid ?? null
  apiProcess = null

  if (processToStop && !processToStop.killed) {
    processToStop.kill('SIGTERM')
  }

  await new Promise<void>((resolve) => {
    let settled = false
    const finalize = (): void => {
      if (settled) {
        return
      }

      settled = true
      apiProcessPid = null
      resolve()
    }

    if (processToStop) {
      if (processToStop.killed || processToStop.exitCode !== null) {
        finalize()
        return
      }

      processToStop.once('exit', finalize)
    }

    setTimeout(() => {
      if (processToStop && !processToStop.killed) {
        processToStop.kill('SIGKILL')
      }
    }, STOP_SIGKILL_ESCALATION_MS)

    setTimeout(() => {
      if (stopPid !== null) {
        killProcessTreeSync(stopPid)
      }

      finalize()
    }, STOP_HARD_DEADLINE_MS)
  })
}
