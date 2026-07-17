import { FALLBACK_MODE_OPTIONS } from '@/lib/chat/agent-mode-utils'
import type { AgentMode } from '@/lib/chat/types'

const STORAGE_KEY = 'orchi.defaultChatMode'
export const DEFAULT_CHAT_MODE: AgentMode = 'orchestration'

const KNOWN_MODE_IDS = new Set(FALLBACK_MODE_OPTIONS.map((option) => option.id.toLowerCase()))

export function isDefaultChatMode(value: unknown): value is AgentMode {
  return typeof value === 'string' && KNOWN_MODE_IDS.has(value.toLowerCase())
}

export function getDefaultChatMode(): AgentMode {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (isDefaultChatMode(raw)) {
      return raw
    }
  } catch {
    // ignore storage failures (private mode, etc.)
  }

  return DEFAULT_CHAT_MODE
}

export function setDefaultChatMode(mode: AgentMode): void {
  if (!isDefaultChatMode(mode)) {
    return
  }

  try {
    localStorage.setItem(STORAGE_KEY, mode)
  } catch {
    // ignore
  }
}

export function getDefaultChatModeLabel(mode: AgentMode): string {
  const match = FALLBACK_MODE_OPTIONS.find(
    (option) => option.id.toLowerCase() === mode.toLowerCase()
  )
  return match?.label ?? mode
}

export function getDefaultChatModeOptions(): typeof FALLBACK_MODE_OPTIONS {
  return FALLBACK_MODE_OPTIONS
}

export { STORAGE_KEY as DEFAULT_CHAT_MODE_STORAGE_KEY }
