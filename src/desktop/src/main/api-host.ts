import { app } from 'electron'
import { spawn, type ChildProcess } from 'child_process'
import { existsSync } from 'fs'
import { join } from 'path'

const DEFAULT_PORT = '5265'

let apiProcess: ChildProcess | null = null
let resolvedApiBaseUrl: string | null = null

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

function resolveApiExecutable(): string {
  const apiName = process.platform === 'win32' ? 'Orchi.Api.exe' : 'Orchi.Api'
  return join(process.resourcesPath, 'api', apiName)
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
    stdio: 'pipe',
    windowsHide: true
  })

  apiProcess.on('error', (error) => {
    console.error('API process error:', error)
  })

  await waitForHealth(`${resolvedApiBaseUrl}/health`, 30_000)
}

export async function stopApiHost(): Promise<void> {
  if (!apiProcess) {
    return
  }

  const processToStop = apiProcess
  apiProcess = null

  processToStop.kill('SIGTERM')

  await new Promise<void>((resolve) => {
    if (processToStop.killed) {
      resolve()
      return
    }

    processToStop.once('exit', () => resolve())

    setTimeout(() => {
      if (!processToStop.killed) {
        processToStop.kill('SIGKILL')
      }

      resolve()
    }, 5_000)
  })
}
