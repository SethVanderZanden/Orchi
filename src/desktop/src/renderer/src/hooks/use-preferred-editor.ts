import { useCallback, useState, useEffect } from 'react'

import {
  getPreferredEditor,
  setPreferredEditor,
  type EditorId
} from '@/lib/preferences/preferred-editor'

type UsePreferredEditorResult = {
  preferredEditor: EditorId
  setPreferredEditor: (editor: EditorId) => void
}

export function usePreferredEditor(): UsePreferredEditorResult {
  const [preferredEditor, setPreferredEditorState] = useState<EditorId>(() => getPreferredEditor())

  useEffect(() => {
    function onStorage(event: StorageEvent): void {
      if (event.key !== 'orchi.preferredEditor') {
        return
      }

      setPreferredEditorState(getPreferredEditor())
    }

    window.addEventListener('storage', onStorage)
    return () => window.removeEventListener('storage', onStorage)
  }, [])

  const updatePreferredEditor = useCallback((editor: EditorId) => {
    setPreferredEditor(editor)
    setPreferredEditorState(editor)
  }, [])

  return {
    preferredEditor,
    setPreferredEditor: updatePreferredEditor
  }
}
