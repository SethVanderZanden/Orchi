import { useState } from 'react'
import { FolderPlus, Trash2 } from 'lucide-react'

import { EmptyState } from '@/components/empty-state'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Separator } from '@/components/ui/separator'
import { displayWorkspacePath } from '@/lib/projects/paths'
import { getDefaultWorkspace } from '@/lib/projects/group-chats'
import { useProjects } from '@/providers/project-provider'

export function ProjectsSettingsCard(): React.JSX.Element {
  const { projects, addProject, removeProject, renameProject, pickDirectory, isPendingProjects } =
    useProjects()
  const [manualPath, setManualPath] = useState('')
  const [editingId, setEditingId] = useState<string | null>(null)
  const [editingName, setEditingName] = useState('')
  const [isPicking, setIsPicking] = useState(false)

  async function handlePickDirectory(): Promise<void> {
    setIsPicking(true)
    try {
      const path = await pickDirectory()
      if (path) {
        await addProject(path)
      }
    } finally {
      setIsPicking(false)
    }
  }

  async function handleManualAdd(): Promise<void> {
    const path = displayWorkspacePath(manualPath)
    if (!path) {
      return
    }

    await addProject(path)
    setManualPath('')
  }

  function startEditing(id: string, name: string): void {
    setEditingId(id)
    setEditingName(name)
  }

  async function saveEditing(): Promise<void> {
    if (!editingId) {
      return
    }

    await renameProject(editingId, editingName)
    setEditingId(null)
    setEditingName('')
  }

  return (
    <Card>
      <CardHeader className="flex flex-row items-start justify-between gap-4 space-y-0">
        <div className="space-y-1">
          <CardTitle className="text-base">Projects</CardTitle>
          <CardDescription>
            Register project folders once, then create many chats per project.
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
        {isPendingProjects && projects.length === 0 ? (
          <p className="text-sm text-muted-foreground">Loading projects…</p>
        ) : projects.length === 0 ? (
          <EmptyState
            className="py-8"
            title="No projects yet"
            description="Add a folder to organize chats by project."
            icon={<FolderPlus className="size-8" />}
          />
        ) : (
          <ul className="divide-y rounded-lg border">
            {projects.map((project) => {
              const defaultWorkspace = getDefaultWorkspace(project)
              const workspaceCount = project.workspaces.length

              return (
                <li
                  key={project.id}
                  className="flex items-center justify-between gap-3 px-3 py-2.5"
                >
                  <button
                    type="button"
                    className="min-w-0 flex-1 text-left"
                    onClick={() => startEditing(project.id, project.name)}
                  >
                    <p className="truncate text-sm font-medium">{project.name}</p>
                    <p className="truncate text-xs text-muted-foreground">
                      {defaultWorkspace?.path ?? 'No workspace'}
                      {workspaceCount > 1 ? ` · ${workspaceCount} workspaces` : ''}
                    </p>
                  </button>
                  <Button
                    variant="ghost"
                    size="icon"
                    aria-label={`Remove ${project.name}`}
                    onClick={() => void removeProject(project.id)}
                  >
                    <Trash2 className="size-4" />
                  </Button>
                </li>
              )
            })}
          </ul>
        )}

        {editingId ? (
          <div className="space-y-2">
            <Label htmlFor="project-name">Project name</Label>
            <Input
              id="project-name"
              value={editingName}
              onChange={(event) => setEditingName(event.target.value)}
              onBlur={() => void saveEditing()}
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
            <Button
              variant="secondary"
              onClick={() => void handleManualAdd()}
              disabled={!manualPath.trim()}
            >
              Add
            </Button>
          </div>
        </div>

        <p className="text-xs text-muted-foreground">
          Removing a project does not delete its chats — they appear under Other until
          re-registered.
        </p>
      </CardContent>
    </Card>
  )
}
