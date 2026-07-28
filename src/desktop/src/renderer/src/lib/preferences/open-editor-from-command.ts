import { parseOpenEditorCommand } from '@/lib/open-editor/parse-open-editor-command'
import { getPreferredEditor } from '@/lib/preferences/preferred-editor'

export async function openEditorFromCommand(
  command: string,
  workspacePath?: string | null
): Promise<string | null> {
  const parsed = parseOpenEditorCommand(command)
  if (!parsed) {
    return 'Could not parse editor location.'
  }

  if (!window.api?.openInEditor) {
    return 'Open in editor is unavailable in this environment.'
  }

  const resolvedWorkspace = workspacePath?.trim() || parsed.workspacePath

  const result = await window.api.openInEditor(resolvedWorkspace, getPreferredEditor(), {
    relativePath: parsed.relativePath,
    line: parsed.line,
    ...(parsed.column !== undefined ? { column: parsed.column } : {})
  })

  return result.ok ? null : result.error
}
