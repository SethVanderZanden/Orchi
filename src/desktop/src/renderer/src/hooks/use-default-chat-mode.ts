import { useCallback, useEffect, useState } from 'react'

import type { AgentMode } from '@/lib/chat/types'
import {
  DEFAULT_CHAT_MODE_STORAGE_KEY,
  getDefaultChatMode,
  setDefaultChatMode
} from '@/lib/preferences/default-chat-mode'

type UseDefaultChatModeResult = {
  defaultChatMode: AgentMode
  setDefaultChatMode: (mode: AgentMode) => void
}

export function useDefaultChatMode(): UseDefaultChatModeResult {
  const [defaultChatMode, setDefaultChatModeState] = useState<AgentMode>(() => getDefaultChatMode())

  useEffect(() => {
    function onStorage(event: StorageEvent): void {
      if (event.key !== DEFAULT_CHAT_MODE_STORAGE_KEY) {
        return
      }

      setDefaultChatModeState(getDefaultChatMode())
    }

    window.addEventListener('storage', onStorage)
    return () => window.removeEventListener('storage', onStorage)
  }, [])

  const updateDefaultChatMode = useCallback((mode: AgentMode) => {
    setDefaultChatMode(mode)
    setDefaultChatModeState(mode)
  }, [])

  return {
    defaultChatMode,
    setDefaultChatMode: updateDefaultChatMode
  }
}
