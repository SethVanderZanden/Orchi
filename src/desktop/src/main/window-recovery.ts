import { dialog, type BrowserWindow, type RenderProcessGoneDetails } from 'electron'
import { log } from './logging'

const MAX_RELOADS = 3
const RELOAD_WINDOW_MS = 60_000

export type WindowRecoveryOptions = {
  maxReloads?: number
  reloadWindowMs?: number
  reload?: (window: BrowserWindow) => void
  showRecoveryFailedDialog?: (window: BrowserWindow, reason: string) => Promise<void>
}

/**
 * Reloads the window when the renderer crashes (classic white-screen / blank page).
 * Caps reloads so a crash loop does not thrash forever.
 */
export function attachWindowRecovery(
  mainWindow: BrowserWindow,
  options: WindowRecoveryOptions = {}
): () => void {
  const maxReloads = options.maxReloads ?? MAX_RELOADS
  const reloadWindowMs = options.reloadWindowMs ?? RELOAD_WINDOW_MS
  const reload =
    options.reload ??
    ((window: BrowserWindow): void => {
      if (!window.isDestroyed()) {
        window.webContents.reloadIgnoringCache()
      }
    })
  const showRecoveryFailedDialog =
    options.showRecoveryFailedDialog ??
    (async (window: BrowserWindow, reason: string): Promise<void> => {
      if (window.isDestroyed()) {
        return
      }
      await dialog.showMessageBox(window, {
        type: 'error',
        title: 'Orchi needs a restart',
        message: 'The UI crashed repeatedly and could not recover.',
        detail: `Last crash reason: ${reason}\n\nOpen Settings → Diagnostics for log files, then restart Orchi.`,
        buttons: ['OK']
      })
    })

  let reloadTimestamps: number[] = []

  const tryRecover = (reason: string): void => {
    const now = Date.now()
    reloadTimestamps = reloadTimestamps.filter((stamp) => now - stamp < reloadWindowMs)

    if (reloadTimestamps.length >= maxReloads) {
      log.error('Renderer recovery aborted after repeated crashes', {
        reason,
        attempts: reloadTimestamps.length,
        maxReloads
      })
      void showRecoveryFailedDialog(mainWindow, reason)
      return
    }

    reloadTimestamps.push(now)
    log.warn('Reloading window after renderer failure', {
      reason,
      attempt: reloadTimestamps.length,
      maxReloads
    })
    reload(mainWindow)
  }

  const onRenderProcessGone = (_event: Electron.Event, details: RenderProcessGoneDetails): void => {
    log.error('Renderer process gone', details)
    if (details.reason === 'clean-exit') {
      return
    }
    tryRecover(details.reason)
  }

  const onUnresponsive = (): void => {
    log.warn('Renderer became unresponsive')
  }

  const onResponsive = (): void => {
    log.info('Renderer responsive again')
  }

  const onDidFailLoad = (
    _event: Electron.Event,
    errorCode: number,
    errorDescription: string,
    validatedURL: string,
    isMainFrame: boolean
  ): void => {
    if (!isMainFrame || errorCode === 0) {
      return
    }
    log.error('Main frame failed to load', {
      errorCode,
      errorDescription,
      validatedURL
    })
  }

  mainWindow.webContents.on('render-process-gone', onRenderProcessGone)
  mainWindow.webContents.on('unresponsive', onUnresponsive)
  mainWindow.webContents.on('responsive', onResponsive)
  mainWindow.webContents.on('did-fail-load', onDidFailLoad)

  return (): void => {
    if (mainWindow.isDestroyed()) {
      return
    }
    mainWindow.webContents.off('render-process-gone', onRenderProcessGone)
    mainWindow.webContents.off('unresponsive', onUnresponsive)
    mainWindow.webContents.off('responsive', onResponsive)
    mainWindow.webContents.off('did-fail-load', onDidFailLoad)
  }
}
