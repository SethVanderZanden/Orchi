import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { BookOpen, ExternalLink, Trash2 } from 'lucide-react'
import { toast } from 'sonner'

import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Separator } from '@/components/ui/separator'
import {
  addAgentModel,
  listAgentModels,
  removeAgentModel,
  updateAgentModelEnabled
} from '@/lib/chat/agent-models-api'
import { CODEX_MODEL_PRESETS } from '@/lib/agents/codex-presets'
import { resolveAgentSettingsStrategy } from '@/lib/agents/settings/resolve-agent-settings-strategy'
import { agentKeys } from '@/lib/query-keys'
import { cn } from '@/lib/utils'

const ONE_HOUR_MS = 60 * 60 * 1000

type CodexAgentModelsCardProps = {
  agentId: string
  agentLabel?: string
}

function invalidateAgentModelQueries(
  queryClient: ReturnType<typeof useQueryClient>,
  agentId: string
): void {
  void queryClient.invalidateQueries({ queryKey: agentKeys.modelsForAgent(agentId) })
}

function openExternalDocs(href: string): void {
  window.open(href, '_blank', 'noopener,noreferrer')
}

export function CodexAgentModelsCard({
  agentId,
  agentLabel
}: CodexAgentModelsCardProps): React.JSX.Element {
  const queryClient = useQueryClient()
  const strategy = resolveAgentSettingsStrategy(agentId)
  const [manualSlug, setManualSlug] = useState('')
  const [manualLabel, setManualLabel] = useState('')

  const modelsQuery = useQuery({
    queryKey: agentKeys.models(agentId, true),
    queryFn: () => listAgentModels(agentId, true),
    staleTime: ONE_HOUR_MS
  })

  const addMutation = useMutation({
    mutationFn: ({ modelId, label }: { modelId: string; label?: string }) =>
      addAgentModel(agentId, modelId, label),
    onSuccess: () => {
      setManualSlug('')
      setManualLabel('')
      invalidateAgentModelQueries(queryClient, agentId)
      toast.success('Model added')
    },
    onError: (error: Error) => toast.error(error.message)
  })

  const toggleMutation = useMutation({
    mutationFn: ({ modelId, enabled }: { modelId: string; enabled: boolean }) =>
      updateAgentModelEnabled(agentId, modelId, enabled),
    onSuccess: () => {
      invalidateAgentModelQueries(queryClient, agentId)
    },
    onError: (error: Error) => toast.error(error.message)
  })

  const removeMutation = useMutation({
    mutationFn: (modelId: string) => removeAgentModel(agentId, modelId),
    onSuccess: () => {
      invalidateAgentModelQueries(queryClient, agentId)
      toast.success('Model removed')
    },
    onError: (error: Error) => toast.error(error.message)
  })

  const models = modelsQuery.data?.models ?? []
  const enabledIds = new Set(models.filter((model) => model.isEnabled).map((model) => model.id))
  const isBusy = addMutation.isPending || toggleMutation.isPending || removeMutation.isPending

  function handleAddManual(): void {
    const slug = manualSlug.trim()
    if (!slug) {
      return
    }

    addMutation.mutate({
      modelId: slug,
      label: manualLabel.trim() || undefined
    })
  }

  function handleAddPreset(modelId: string, label: string): void {
    const existing = models.find((model) => model.id === modelId)
    if (existing?.isEnabled) {
      toast.message(`${label} is already enabled`)
      return
    }

    if (existing && !existing.isEnabled) {
      toggleMutation.mutate({ modelId, enabled: true })
      return
    }

    addMutation.mutate({ modelId, label })
  }

  return (
    <Card>
      <CardHeader className="flex flex-row items-start justify-between gap-4 space-y-0">
        <div className="space-y-1">
          <CardTitle className="text-base">{agentLabel ?? agentId} models</CardTitle>
          <CardDescription>
            Codex has no CLI model sync. Enable GPT-5.6 Sol / Terra / Luna (same names as Codex),
            then pick reasoning effort separately — e.g. Terra + Medium reads as “5.6 Terra Medium”.
          </CardDescription>
        </div>
        {strategy.modelsDocs ? (
          <Button
            variant="outline"
            size="sm"
            onClick={() => openExternalDocs(strategy.modelsDocs!.href)}
            aria-label={`Open ${strategy.modelsDocs.label} documentation`}
          >
            <BookOpen className="size-4" />
            Docs
            <ExternalLink className="size-3.5 opacity-70" />
          </Button>
        ) : null}
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="space-y-2">
          <Label>GPT-5.6 presets</Label>
          <div className="grid gap-2 sm:grid-cols-3">
            {CODEX_MODEL_PRESETS.map((preset) => {
              const enabled = enabledIds.has(preset.id)
              return (
                <button
                  key={preset.id}
                  type="button"
                  disabled={isBusy}
                  onClick={() => handleAddPreset(preset.id, preset.label)}
                  className={cn(
                    'rounded-lg border px-3 py-2.5 text-left transition-colors',
                    enabled ? 'border-primary bg-primary/5' : 'border-border hover:bg-accent/40',
                    isBusy && 'pointer-events-none opacity-70'
                  )}
                >
                  <span className="block text-sm font-medium">{preset.label}</span>
                  <span className="mt-0.5 block text-xs text-muted-foreground">
                    {enabled ? 'Enabled' : 'Add'} · {preset.id}
                  </span>
                </button>
              )
            })}
          </div>
        </div>

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
            No models yet. Enable a GPT-5.6 preset above or add a custom slug.
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
                      {model.source === 'built-in' ? ' · built-in' : ''}
                      {model.isDefault ? ' · suggested default' : ''}
                    </span>
                  </span>
                </label>
                <Button
                  variant="ghost"
                  size="icon"
                  aria-label={`Remove ${model.label}`}
                  disabled={isBusy}
                  onClick={() => removeMutation.mutate(model.id)}
                >
                  <Trash2 className="size-4" />
                </Button>
              </li>
            ))}
          </ul>
        )}

        <Separator />

        <div className="grid gap-2 sm:grid-cols-2">
          <div className="space-y-2">
            <Label htmlFor={`manual-model-slug-${agentId}`}>Custom slug</Label>
            <Input
              id={`manual-model-slug-${agentId}`}
              value={manualSlug}
              onChange={(event) => setManualSlug(event.target.value)}
              placeholder={strategy.manualModelPlaceholder}
              disabled={isBusy}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor={`manual-model-label-${agentId}`}>Display label (optional)</Label>
            <Input
              id={`manual-model-label-${agentId}`}
              value={manualLabel}
              onChange={(event) => setManualLabel(event.target.value)}
              placeholder="e.g. 5.6 Terra"
              disabled={isBusy}
            />
          </div>
        </div>
        <div className="flex justify-end">
          <Button
            variant="secondary"
            disabled={!manualSlug.trim() || isBusy}
            onClick={handleAddManual}
          >
            Add
          </Button>
        </div>
      </CardContent>
    </Card>
  )
}
