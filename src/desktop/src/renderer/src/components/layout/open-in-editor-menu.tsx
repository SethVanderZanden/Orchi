import { useState } from 'react'
import { ChevronDown } from 'lucide-react'

import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger
} from '@/components/ui/dropdown-menu'

type EditorId = 'vscode' | 'cursor'

type OpenInEditorMenuProps = {
  workspacePath: string
}

export function OpenInEditorMenu({ workspacePath }: OpenInEditorMenuProps): React.JSX.Element {
  const [error, setError] = useState<string | null>(null)
  const disabled = !workspacePath.trim()

  async function handleOpen(editor: EditorId): Promise<void> {
    if (!window.api?.openInEditor) {
      setError('Open in editor is unavailable in this environment.')
      return
    }

    setError(null)
    const result = await window.api.openInEditor(workspacePath, editor)
    if (!result.ok) {
      setError(result.error)
    }
  }

  return (
    <div className="flex flex-col items-end gap-1">
      <div className="inline-flex -space-x-px">
        <Button
          variant="outline"
          size="sm"
          disabled={disabled}
          className="h-8 rounded-r-none px-3 text-xs font-normal"
          aria-label="Open workspace in VS Code"
          onClick={() => void handleOpen('vscode')}
        >
          Open In Code
        </Button>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button
              variant="outline"
              size="sm"
              disabled={disabled}
              className="h-8 rounded-l-none px-2"
              aria-label="Open workspace in another editor"
            >
              <ChevronDown className="size-3.5 opacity-60" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onClick={() => void handleOpen('cursor')}>
              Open in Cursor
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
      {error ? <p className="max-w-48 truncate text-[10px] text-destructive">{error}</p> : null}
    </div>
  )
}
