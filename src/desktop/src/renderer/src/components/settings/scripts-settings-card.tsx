import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Plus, ScrollText, Trash2 } from 'lucide-react'
import { toast } from 'sonner'

import { EmptyState } from '@/components/empty-state'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { NativeSelect } from '@/components/ui/native-select'
import { Textarea } from '@/components/ui/textarea'
import {
  applyOrchestrationGitDefaults,
  createScript,
  deleteScript,
  listScripts
} from '@/lib/scripts/api'
import type { ScriptEvent } from '@/lib/scripts/types'
import { scriptKeys } from '@/lib/query-keys'
import { useProjects } from '@/providers/project-provider'

const defaultStepsJson = JSON.stringify([{ kind: 'shell', command: 'npm run lint' }], null, 2)

export function ScriptsSettingsCard(): React.JSX.Element {
  const queryClient = useQueryClient()
  const { projects } = useProjects()
  const [scopeProjectId, setScopeProjectId] = useState<string>('global')
  const [name, setName] = useState('')
  const [event, setEvent] = useState<ScriptEvent>('agentFinish')
  const [modeFilter, setModeFilter] = useState<string>('any')
  const [stepsJson, setStepsJson] = useState(defaultStepsJson)

  const projectId = scopeProjectId === 'global' ? null : scopeProjectId

  const scriptsQuery = useQuery({
    queryKey: scriptKeys.lists(projectId),
    queryFn: () => listScripts(projectId)
  })

  const invalidate = async (): Promise<void> => {
    await queryClient.invalidateQueries({ queryKey: scriptKeys.all })
  }

  const createMutation = useMutation({
    mutationFn: createScript,
    onSuccess: async () => {
      setName('')
      setStepsJson(defaultStepsJson)
      toast.success('Script created')
      await invalidate()
    },
    onError: (error: Error) => toast.error(error.message)
  })

  const deleteMutation = useMutation({
    mutationFn: deleteScript,
    onSuccess: async () => {
      toast.success('Script deleted')
      await invalidate()
    },
    onError: (error: Error) => toast.error(error.message)
  })

  const defaultsMutation = useMutation({
    mutationFn: () => applyOrchestrationGitDefaults(projectId),
    onSuccess: async () => {
      toast.success('Orchestration git defaults applied (worktree on start + commit/push/PR)')
      await invalidate()
    },
    onError: (error: Error) => toast.error(error.message)
  })

  const scripts = scriptsQuery.data ?? []

  function handleCreate(): void {
    createMutation.mutate({
      name: name.trim(),
      projectId,
      stepsJson,
      bindings: [
        {
          event,
          modeFilter: modeFilter === 'any' ? null : modeFilter,
          order: 0,
          enabled: true,
          onError: 'continue'
        }
      ]
    })
  }

  return (
    <Card>
      <CardHeader className="flex flex-row items-start justify-between gap-4 space-y-0">
        <div className="space-y-1">
          <CardTitle className="text-base">Scripts</CardTitle>
          <CardDescription>
            Named scripts run on agent start/finish. Attach a mode filter for review or
            implementation turns.
          </CardDescription>
        </div>
        <Button
          variant="secondary"
          size="sm"
          onClick={() => defaultsMutation.mutate()}
          disabled={defaultsMutation.isPending}
        >
          Apply git defaults
        </Button>
      </CardHeader>
      <CardContent className="space-y-6">
        <div className="space-y-2">
          <Label htmlFor="script-scope">Scope</Label>
          <NativeSelect
            id="script-scope"
            value={scopeProjectId}
            onChange={(change) => setScopeProjectId(change.target.value)}
          >
            <option value="global">Global</option>
            {projects.map((project) => (
              <option key={project.id} value={project.id}>
                {project.name}
              </option>
            ))}
          </NativeSelect>
        </div>

        <div className="space-y-3 rounded-lg border p-3">
          <div className="space-y-2">
            <Label htmlFor="script-name">Name</Label>
            <Input
              id="script-name"
              value={name}
              onChange={(change) => setName(change.target.value)}
              placeholder="Lint after agent"
            />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-2">
              <Label htmlFor="script-event">Event</Label>
              <NativeSelect
                id="script-event"
                value={event}
                onChange={(change) => setEvent(change.target.value as ScriptEvent)}
              >
                <option value="agentStart">Agent start</option>
                <option value="agentFinish">Agent finish</option>
              </NativeSelect>
            </div>
            <div className="space-y-2">
              <Label htmlFor="script-mode">Mode</Label>
              <NativeSelect
                id="script-mode"
                value={modeFilter}
                onChange={(change) => setModeFilter(change.target.value)}
              >
                <option value="any">Any mode</option>
                <option value="default">Default</option>
                <option value="orchestration">Orchestration</option>
                <option value="implementation">Implementation</option>
                <option value="review">Review</option>
              </NativeSelect>
            </div>
          </div>
          <div className="space-y-2">
            <Label htmlFor="script-steps">Steps JSON</Label>
            <Textarea
              id="script-steps"
              value={stepsJson}
              onChange={(change) => setStepsJson(change.target.value)}
              className="min-h-28 font-mono text-xs"
            />
          </div>
          <Button
            size="sm"
            onClick={handleCreate}
            disabled={!name.trim() || createMutation.isPending}
          >
            <Plus className="size-4" />
            Add script
          </Button>
        </div>

        {scripts.length === 0 ? (
          <EmptyState
            className="py-6"
            title="No scripts yet"
            description="Create a shell or git workflow attached to agent lifecycle events."
            icon={<ScrollText className="size-8" />}
          />
        ) : (
          <ul className="divide-y rounded-lg border">
            {scripts.map((script) => (
              <li key={script.id} className="flex items-start justify-between gap-3 px-3 py-2.5">
                <div className="min-w-0 space-y-1">
                  <p className="truncate text-sm font-medium">{script.name}</p>
                  <p className="text-xs text-muted-foreground">
                    {script.projectId ? 'Project' : 'Global'}
                    {script.bindings[0]
                      ? ` · ${script.bindings[0].event}${
                          script.bindings[0].modeFilter ? ` · ${script.bindings[0].modeFilter}` : ''
                        }`
                      : ''}
                  </p>
                </div>
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={() => deleteMutation.mutate(script.id)}
                  aria-label={`Delete ${script.name}`}
                >
                  <Trash2 className="size-4" />
                </Button>
              </li>
            ))}
          </ul>
        )}
      </CardContent>
    </Card>
  )
}
