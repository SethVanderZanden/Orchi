import { useState } from 'react'
import { createFileRoute, Link } from '@tanstack/react-router'
import { FolderPlusIcon, SettingsIcon, Trash2Icon } from 'lucide-react'

import { AppPageHeader } from '@/components/layout/app-page-header'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useWorkspaces } from '@/providers/workspace-provider'
import { displayWorkspacePath } from '@/lib/workspaces/store'

export const Route = createFileRoute('/_app/settings')({
  component: SettingsPage
})

function SettingsPage(): React.JSX.Element {
  const { workspaces, addWorkspace: registerWorkspace, removeWorkspace, renameWorkspace, pickDirectory } =
    useWorkspaces()
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
    <div className="flex h-full min-h-0 flex-1 flex-col">
      <AppPageHeader title="Settings" description="Projects and app preferences" />

      <main className="mx-auto flex w-full max-w-2xl flex-1 flex-col gap-6 overflow-y-auto p-6">
        <section className="space-y-4">
          <div className="flex items-center justify-between gap-4">
            <div>
              <h2 className="text-sm font-medium">Projects</h2>
              <p className="text-muted-foreground text-sm leading-relaxed">
                Register project folders once, then create many chats per project from the sidebar.
              </p>
            </div>
            <Button variant="outline" size="sm" onClick={handlePickDirectory} disabled={isPicking}>
              <FolderPlusIcon />
              Add project
            </Button>
          </div>

          {workspaces.length === 0 ? (
            <p className="text-muted-foreground rounded-xl border border-dashed p-4 text-sm">
              No projects registered yet. Add a folder to organize chats by workspace.
            </p>
          ) : (
            <ul className="divide-y rounded-xl border">
              {workspaces.map((workspace) => (
                <li key={workspace.id} className="flex items-start gap-3 p-4">
                  <div className="min-w-0 flex-1">
                    {editingId === workspace.id ? (
                      <Input
                        value={editingName}
                        onChange={(event) => setEditingName(event.target.value)}
                        onBlur={saveEditing}
                        onKeyDown={(event) => {
                          if (event.key === 'Enter') {
                            saveEditing()
                          }
                        }}
                        autoFocus
                      />
                    ) : (
                      <button
                        type="button"
                        className="text-left text-sm font-medium hover:underline"
                        onClick={() => startEditing(workspace.id, workspace.name)}
                      >
                        {workspace.name}
                      </button>
                    )}
                    <p className="text-muted-foreground mt-1 truncate text-xs">{workspace.path}</p>
                  </div>
                  <Button
                    size="icon-sm"
                    variant="ghost"
                    aria-label={`Remove ${workspace.name}`}
                    onClick={() => removeWorkspace(workspace.id)}
                  >
                    <Trash2Icon />
                  </Button>
                </li>
              ))}
            </ul>
          )}

          <div className="space-y-2 rounded-xl border p-4">
            <Label htmlFor="manualPath">Or paste a path</Label>
            <div className="flex gap-2">
              <Input
                id="manualPath"
                value={manualPath}
                onChange={(event) => setManualPath(event.target.value)}
                placeholder="e.g. E:\Projects\Orchi"
              />
              <Button variant="secondary" onClick={handleManualAdd} disabled={!manualPath.trim()}>
                Add
              </Button>
            </div>
          </div>
        </section>

        <section className="rounded-xl border p-4">
          <p className="text-muted-foreground mb-4 text-sm">
            Jump back to a conversation without losing sidebar state.
          </p>
          <Button asChild variant="outline">
            <Link to="/">Open chats</Link>
          </Button>
        </section>

        <section className="text-muted-foreground flex items-center gap-2 text-xs">
          <SettingsIcon className="size-3.5" />
          Removing a project does not delete its chats — they appear under Other until re-registered.
        </section>
      </main>
    </div>
  )
}
