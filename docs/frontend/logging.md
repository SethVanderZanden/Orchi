# Desktop logging

## Dummy section (start here)

Think of Orchi logs like a **flight recorder** on a plane.

When the window suddenly goes blank (white screen), the UI itself is often already gone — so you cannot ask the app on-screen what happened. The flight recorder kept writing the last moments to disk: crashes, failed loads, React errors, and main-process warnings.

```
Renderer / Main  ──IPC──►  electron-log (main)  ──►  log file on disk
                                    ▲
                          Settings → Open log folder
```

**Aha:** Blank screens are usually a dead renderer process, not a missing button. Logs + auto-reload are how we recover and diagnose.

| Analogy | Orchi |
| --- | --- |
| Flight recorder | `electron-log` writing under the app userData folder |
| Cockpit alarm | Auto-reload when the renderer crashes |
| Black box folder | Settings → General → Diagnostics → Open log folder |

Everything below is the same idea with file paths and APIs.

---

## Package

Orchi uses [`electron-log`](https://github.com/megahertz/electron-log) v5 — zero config for Electron, main + renderer over IPC, rotating file transport.

## Where it is wired

| Layer | File | Role |
| --- | --- | --- |
| Main boot | `src/main/logging.ts` | `setupLogging()`, file path helpers |
| Crash recovery | `src/main/window-recovery.ts` | Reload on `render-process-gone` |
| Main entry | `src/main/index.ts` | Init logger, IPC for opening logs |
| Renderer boot | `src/renderer/src/main.tsx` | `setupRendererLogging()` |
| Renderer API | `src/renderer/src/lib/logger.ts` | `import { logger } from '@/lib/logger'` |
| React errors | `components/error-boundary.tsx` | Logs + themed recovery UI |
| UI | Settings → Diagnostics | Open log folder |

## Using the logger

```ts
import { logger } from '@/lib/logger'

logger.info('Chat opened', { chatId })
logger.warn('Slow request', { ms })
logger.error('Failed to save', error)
```

Main process:

```ts
import { log } from './logging'

log.info('API host started')
log.error('API process error:', error)
```

## Finding log files

1. Open **Settings → General → Diagnostics**
2. Click **Open log folder**

Default location (Electron `userData` logs):

- Windows: `%APPDATA%/desktop/logs/` (product name from package)
- macOS: `~/Library/Application Support/<app>/logs/`
- Linux: `~/.config/<app>/logs/`

## White screen recovery

If Chromium kills the renderer (OOM, GPU crash, etc.), Electron shows an empty white page. Orchi:

1. Logs `render-process-gone` / related Electron events
2. Reloads the window (up to 3 times per minute)
3. Shows a restart dialog if crashes keep looping
