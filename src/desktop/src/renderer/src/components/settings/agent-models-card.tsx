import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { RefreshCw, Trash2 } from 'lucide-react'

import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Separator } from '@/components/ui/separator'
import {
  addAgentModel,
  listAgentModels,
  removeAgentModel,
  syncAgentModels,
  updateAgentModelEnabled
} from '@/lib/chat/agent-models-api'
import { agentKeys } from '@/lib/query-keys'

const ONE_HOUR_MS = 60 * 60 * 1000

type AgentModelsCardProps = {
  agentId: string
}

function invalidateAgentModelQueries(
  queryClient: ReturnType<typeof useQueryClient>,
  agentId: string
): void {
  void queryClient.invalidateQueries({ queryKey: agentKeys.modelsForAgent(agentId) })
}

export function AgentModelsCard({ agentId }: AgentModelsCardProps): React.JSX.Element {
  const queryClient = useQueryClient()
  const [manualSlug, setManualSlug] = useState('')
  const [actionError, setActionError] = useState<string | null>(null)

  const modelsQuery = useQuery({
    queryKey: agentKeys.models(agentId, true),
    queryFn: () => listAgentModels(agentId, true),
    staleTime: ONE_HOUR_MS
  })

  const syncMutation = useMutation({
    mutationFn: () => syncAgentModels(agentId),
    onSuccess: () => {
      setActionError(null)
      invalidateAgentModelQueries(queryClient, agentId)
    },
    onError: (error: Error) => setActionError(error.message)
  })

  const addMutation = useMutation({
    mutationFn: (modelId: string) => addAgentModel(agentId, modelId),
    onSuccess: () => {
      setActionError(null)
      setManualSlug('')
      invalidateAgentModelQueries(queryClient, agentId)
    },
    onError: (error: Error) => setActionError(error.message)
  })

  const toggleMutation = useMutation({
    mutationFn: ({ modelId, enabled }: { modelId: string; enabled: boolean }) =>
      updateAgentModelEnabled(agentId, modelId, enabled),
    onSuccess: () => {
      setActionError(null)
      invalidateAgentModelQueries(queryClient, agentId)
    },
    onError: (error: Error) => setActionError(error.message)
  })

  const removeMutation = useMutation({
    mutationFn: (modelId: string) => removeAgentModel(agentId, modelId),
    onSuccess: () => {
      setActionError(null)
      invalidateAgentModelQueries(queryClient, agentId)
    },
    onError: (error: Error) => setActionError(error.message)
  })

  const models = modelsQuery.data?.models ?? []
  const lastSyncedAt = modelsQuery.data?.lastSyncedAt
  const isBusy =
    syncMutation.isPending ||
    addMutation.isPending ||
    toggleMutation.isPending ||
    removeMutation.isPending

  function handleAddManual(): void {
    const slug = manualSlug.trim()
    if (!slug) {
      return
    }

    addMutation.mutate(slug)
  }

  return (
    <Card>
      <CardHeader className="flex flex-row items-start justify-between gap-4 space-y-0">
        <div className="space-y-1">
          <CardTitle className="text-base">Agent models</CardTitle>
          <CardDescription>
            Sync models from the Cursor CLI, choose which appear in chat, and add manual slugs.
          </CardDescription>
        </div>
        <Button
          variant="secondary"
          size="sm"
          disabled={isBusy}
          onClick={() => syncMutation.mutate()}
        >
          <RefreshCw className={cnIcon(syncMutation.isPending)} />
          Sync now
        </Button>
      </CardHeader>
      <CardContent className="space-y-4">
        {modelsQuery.isLoading ? (
          <p className="text-sm text-muted-foreground">Loading models…</p>
        ) : modelsQuery.error ? (
          <div className="space-y-2">
            <p className="text-sm text-destructive">
              {modelsQuery.error instanceof Error
                ? modelsQuery.error.message
                : 'Failed to load models.'}
            </p>
            <Button variant="secondary" size="sm" onClick={() => void modelsQuery.refetch()}>
              Retry
            </Button>
          </div>
        ) : models.length === 0 ? (
          <p className="text-sm text-muted-foreground">
            No models yet. Sync from the CLI or add a manual slug.
          </p>
        ) : (
          <ul className="divide-y rounded-lg border">
            {models.map((model) => (
              <li key={model.id} className="flex items-center gap-3 px-3 py-2.5">
                <label className="flex min-w-0 flex-1 cursor-pointer items-start gap-2">
                  <input
                    type="checkbox"
                    className="mt-0.5 size-4 shrink-0 rounded border border-input"
                    checked={model.isEnabled}
                    disabled={isBusy || toggleMutation.isPending}
                    onChange={(event) =>
                      toggleMutation.mutate({
                        modelId: model.id,
                        enabled: event.target.checked
                      })
                    }
                  />
                  <span className="min-w-0">
                    <span className="block truncate text-sm font-medium">{model.label}</span>
                    <span className="block truncate text-xs text-muted-foreground">
                      {model.id}
                      {model.source === 'manual' ? ' · manual' : ''}
                      {model.isDefault ? ' · CLI default' : ''}
                    </span>
                  </span>
                </label>
                {model.source === 'manual' ? (
                  <Button
                    variant="ghost"
                    size="icon"
                    aria-label={`Remove ${model.label}`}
                    disabled={isBusy}
                    onClick={() => removeMutation.mutate(model.id)}
                  >
                    <Trash2 className="size-4" />
                  </Button>
                ) : null}
              </li>
            ))}
          </ul>
        )}

        {lastSyncedAt ? (
          <p className="text-xs text-muted-foreground">
            Last synced {new Date(lastSyncedAt).toLocaleString()}
          </p>
        ) : null}

        {actionError ? <p className="text-sm text-destructive">{actionError}</p> : null}

        <Separator />

        <div className="space-y-2">
          <Label htmlFor="manual-model-slug">Add manual slug</Label>
          <Input
            id="manual-model-slug"
            value={manualSlug}
            onChange={(event) => setManualSlug(event.target.value)}
            placeholder="e.g. claude-sonnet-4"
            disabled={isBusy}
          />
          <div className="flex justify-end">
            <Button
              variant="secondary"
              disabled={!manualSlug.trim() || isBusy}
              onClick={handleAddManual}
            >
              Add
            </Button>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}

function cnIcon(spinning: boolean): string {
  return spinning ? 'size-4 animate-spin' : 'size-4'
}
