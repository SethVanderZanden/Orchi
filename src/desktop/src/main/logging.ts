import { dirname } from 'path'
import log from 'electron-log/main'
import { is } from '@electron-toolkit/utils'

/**
 * Plug-and-play Electron logging (main + renderer via IPC).
 * Call once before creating windows.
 */
export function setupLogging(): typeof log {
  // Injects a small preload so renderer `electron-log/renderer` can ship logs to main.
  log.initialize({ preload: true })

  log.transports.file.level = 'info'
  log.transports.console.level = is.dev ? 'debug' : 'info'
  log.transports.file.maxSize = 5 * 1024 * 1024

  log.errorHandler.startCatching({ showDialog: false })
  // Logs render-process-gone, did-fail-load, unresponsive, etc. to the file transport.
  log.eventLogger.startLogging({ level: 'warn' })

  log.info('Logging initialized', {
    platform: process.platform,
    arch: process.arch,
    electron: process.versions.electron,
    chrome: process.versions.chrome,
    node: process.versions.node,
    logFile: getLogFilePath()
  })

  return log
}

export function getLogFilePath(): string {
  return log.transports.file.getFile().path
}

export function getLogDirectory(): string {
  return dirname(getLogFilePath())
}

export { log }
