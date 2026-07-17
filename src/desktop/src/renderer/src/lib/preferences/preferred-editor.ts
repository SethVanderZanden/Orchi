export type EditorId = 'vscode' | 'cursor'

const STORAGE_KEY = 'orchi.preferredEditor'
const DEFAULT_EDITOR: EditorId = 'vscode'

export function isEditorId(value: unknown): value is EditorId {
  return value === 'vscode' || value === 'cursor'
}

export function getPreferredEditor(): EditorId {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (isEditorId(raw)) {
      return raw
    }
  } catch {
    // ignore storage failures (private mode, etc.)
  }

  return DEFAULT_EDITOR
}

export function setPreferredEditor(editor: EditorId): void {
  try {
    localStorage.setItem(STORAGE_KEY, editor)
  } catch {
    // ignore
  }
}

export function getEditorLabel(editor: EditorId): string {
  return editor === 'vscode' ? 'VS Code' : 'Cursor'
}

export function getOpenInEditorLabel(editor: EditorId): string {
  return `Open in ${getEditorLabel(editor)}`
}

export function getAlternateEditor(editor: EditorId): EditorId {
  return editor === 'vscode' ? 'cursor' : 'vscode'
}
