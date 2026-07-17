import { getPreferredEditor } from '@/lib/preferences/preferred-editor'

export async function openWorkspaceInPreferredEditor(
  workspacePath: string
): Promise<string | null> {
  if (!workspacePath.trim()) {
    return 'No workspace path available.'
  }

  if (!window.api?.openInEditor) {
    return 'Open in editor is unavailable in this environment.'
  }

  const result = await window.api.openInEditor(workspacePath, getPreferredEditor())
  return result.ok ? null : result.error
}
