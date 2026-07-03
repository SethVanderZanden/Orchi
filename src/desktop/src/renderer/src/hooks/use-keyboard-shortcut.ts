import { useEffect } from 'react'

type KeyboardShortcutOptions = {
  enabled?: boolean
  preventDefault?: boolean
}

export function useKeyboardShortcut(
  key: string,
  handler: () => void,
  { enabled = true, preventDefault = true }: KeyboardShortcutOptions = {}
): void {
  useEffect(() => {
    if (!enabled) {
      return
    }

    function handleKeyDown(event: KeyboardEvent): void {
      if (!event.ctrlKey || event.metaKey || event.altKey || event.shiftKey) {
        return
      }

      if (event.key.toLowerCase() !== key.toLowerCase()) {
        return
      }

      const target = event.target
      if (
        target instanceof HTMLElement &&
        (target.isContentEditable ||
          target.tagName === 'INPUT' ||
          target.tagName === 'TEXTAREA' ||
          target.tagName === 'SELECT')
      ) {
        return
      }

      if (preventDefault) {
        event.preventDefault()
      }

      handler()
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [enabled, handler, key, preventDefault])
}

type KeyboardCombo = {
  key: string
  shift?: boolean
  ctrl?: boolean
  alt?: boolean
  meta?: boolean
}

type KeyboardShortcutComboOptions = KeyboardShortcutOptions & {
  allowInTextarea?: boolean
}

export function useKeyboardShortcutCombo(
  combo: KeyboardCombo,
  handler: () => void,
  {
    enabled = true,
    preventDefault = true,
    allowInTextarea = false
  }: KeyboardShortcutComboOptions = {}
): void {
  const { key, shift = false, ctrl = false, alt = false, meta = false } = combo

  useEffect(() => {
    if (!enabled) {
      return
    }

    function handleKeyDown(event: KeyboardEvent): void {
      if (shift !== event.shiftKey) {
        return
      }

      if (ctrl !== event.ctrlKey) {
        return
      }

      if (alt !== event.altKey) {
        return
      }

      if (meta !== event.metaKey) {
        return
      }

      if (event.key.toLowerCase() !== key.toLowerCase()) {
        return
      }

      const target = event.target
      if (
        !allowInTextarea &&
        target instanceof HTMLElement &&
        (target.isContentEditable ||
          target.tagName === 'INPUT' ||
          target.tagName === 'TEXTAREA' ||
          target.tagName === 'SELECT')
      ) {
        return
      }

      if (preventDefault) {
        event.preventDefault()
      }

      handler()
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [allowInTextarea, alt, ctrl, enabled, handler, key, meta, preventDefault, shift])
}
