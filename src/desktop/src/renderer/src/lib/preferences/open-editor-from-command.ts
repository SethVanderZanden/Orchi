import { parseOpenEditorCommand } from '@/lib/open-editor/parse-open-editor-command'
import { getPreferredEditor } from '@/lib/preferences/preferred-editor'

export async function openEditorFromCommand(command: string): Promise<string | null> {
  const parsed = parseOpenEditorCommand(command)
  if (!parsed) {
    return 'Could not parse editor location.'
  }

  if (!window.api?.openInEditor) {
    return 'Open in editor is unavailable in this environment.'
  }

  const result = await window.api.openInEditor(parsed.workspacePath, getPreferredEditor(), {
    relativePath: parsed.relativePath,
    line: parsed.line,
    ...(parsed.column !== undefined ? { column: parsed.column } : {})
  })

  return result.ok ? null : result.error
}
