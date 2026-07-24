import log from 'electron-log/renderer'

/**
 * App-wide renderer logger. Writes to DevTools and (via IPC) the main log file.
 *
 * Usage:
 *   import { logger } from '@/lib/logger'
 *   logger.info('something happened', { chatId })
 */
export const logger = log

/** Call once at renderer boot — catches unhandled errors/rejections into the log file. */
export function setupRendererLogging(): void {
  logger.errorHandler.startCatching({
    showDialog: false,
    preventDefault: false
  })

  logger.info('Renderer logging ready')
}
