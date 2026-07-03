import { useState } from 'react'
import { createFileRoute, useNavigate } from '@tanstack/react-router'
import { FolderPlus, Trash2 } from 'lucide-react'

import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { PageHeader } from '@/components/ui/page-header'
import { Separator } from '@/components/ui/separator'
import { useWorkspaces } from '@/providers/workspace-provider'
import { displayWorkspacePath } from '@/lib/workspaces/store'

export const Route = createFileRoute('/_app/settings')({
  component: SettingsPage
})

function SettingsPage(): React.JSX.Element {
  const navigate = useNavigate()
  const {
    workspaces,
    addWorkspace: registerWorkspace,
    removeWorkspace,
    renameWorkspace,
    pickDirectory
  } = useWorkspaces()
  const [manualPath, setManualPath] = useState('')
  const [editingId, setEditingId] = useState<string | null>(null)
  const [editingName, setEditingName] = useState('')
  const [isPicking, setIsPicking] = useState(false)

  async function handlePickDirectory(): Promise<void> {
    setIsPicking(true)
    try {
      const path = await pickDirectory()
      if (path) {
        registerWorkspace(path)
      }
    } finally {
      setIsPicking(false)
    }
  }

  function handleManualAdd(): void {
    const path = displayWorkspacePath(manualPath)
    if (!path) {
      return
    }

    registerWorkspace(path)
    setManualPath('')
  }

  function startEditing(id: string, name: string): void {
    setEditingId(id)
    setEditingName(name)
  }

  function saveEditing(): void {
    if (!editingId) {
      return
    }

    renameWorkspace(editingId, editingName)
    setEditingId(null)
    setEditingName('')
  }

  return (
    <div className="flex h-full min-h-0 flex-col">
      <PageHeader title="Settings" description="Projects and app preferences" />

      <div className="flex-1 overflow-y-auto p-6">
        <div className="mx-auto flex w-full max-w-2xl flex-col gap-6">
          <Card>
            <CardHeader className="flex flex-row items-start justify-between gap-4 space-y-0">
              <div className="space-y-1">
                <CardTitle className="text-base">Projects</CardTitle>
                <CardDescription>
                  Register project folders once, then create many chats per project from the
                  navigator.
                </CardDescription>
              </div>
              <Button
                variant="secondary"
                size="sm"
                onClick={() => void handlePickDirectory()}
                disabled={isPicking}
              >
                <FolderPlus className="size-4" />
                Add project
              </Button>
            </CardHeader>
            <CardContent className="space-y-4">
              {workspaces.length === 0 ? (
                <p className="text-sm text-muted-foreground">
                  No projects registered yet. Add a folder to organize chats by workspace.
                </p>
              ) : (
                <ul className="divide-y rounded-lg border">
                  {workspaces.map((workspace) => (
                    <li
                      key={workspace.id}
                      className="flex items-center justify-between gap-3 px-3 py-2.5"
                    >
                      <button
                        type="button"
                        className="min-w-0 flex-1 text-left"
                        onClick={() => startEditing(workspace.id, workspace.name)}
                      >
                        <p className="truncate text-sm font-medium">{workspace.name}</p>
                        <p className="truncate text-xs text-muted-foreground">{workspace.path}</p>
                      </button>
                      <Button
                        variant="ghost"
                        size="icon"
                        aria-label={`Remove ${workspace.name}`}
                        onClick={() => removeWorkspace(workspace.id)}
                      >
                        <Trash2 className="size-4" />
                      </Button>
                    </li>
                  ))}
                </ul>
              )}

              {editingId ? (
                <div className="space-y-2">
                  <Label htmlFor="project-name">Project name</Label>
                  <Input
                    id="project-name"
                    value={editingName}
                    onChange={(event) => setEditingName(event.target.value)}
                    onBlur={saveEditing}
                    autoFocus
                  />
                </div>
              ) : null}

              <Separator />

              <div className="space-y-2">
                <Label htmlFor="manual-path">Or paste a path</Label>
                <Input
                  id="manual-path"
                  value={manualPath}
                  onChange={(event) => setManualPath(event.target.value)}
                  placeholder="e.g. E:\Projects\Orchi"
                />
                <div className="flex justify-end">
                  <Button variant="secondary" onClick={handleManualAdd} disabled={!manualPath.trim()}>
                    Add
                  </Button>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="space-y-3 p-4">
              <p className="text-sm text-muted-foreground">
                Jump back to a conversation without losing navigator state.
              </p>
              <Button variant="secondary" onClick={() => navigate({ to: '/' })}>
                Open chats
              </Button>
            </CardContent>
          </Card>

          <p className="text-xs text-muted-foreground">
            Removing a project does not delete its chats — they appear under Other until
            re-registered.
          </p>
        </div>
      </div>
    </div>
  )
}
