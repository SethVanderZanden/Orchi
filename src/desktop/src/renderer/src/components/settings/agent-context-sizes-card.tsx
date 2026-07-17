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
  addAgentContextSize,
  listAgentContextSizes,
  removeAgentContextSize,
  updateAgentContextSizeEnabled
} from '@/lib/chat/agent-context-sizes-api'
import { agentKeys } from '@/lib/query-keys'

const ONE_HOUR_MS = 60 * 60 * 1000

/** Codex catalog defaults from https://developers.openai.com/codex/config-advanced */
const CODEX_CONTEXT_PRESETS = [
  { id: 'compact', label: 'Compact', tokenCount: 128_000 },
  { id: 'default', label: 'Default', tokenCount: 272_000 },
  { id: 'large', label: 'Large', tokenCount: 400_000 }
] as const

const AGENT_CONTEXT_DOCS: Record<string, { label: string; href: string }> = {
  cursor: {
    label: 'Cursor CLI parameters',
    href: 'https://cursor.com/docs/cli/reference/parameters'
  },
  codex: {
    label: 'Codex model_context_window',
    href: 'https://developers.openai.com/codex/config-advanced'
  }
}

type AgentContextSizesCardProps = {
  agentId: string
  agentLabel: string
}

function invalidateContextSizeQueries(
  queryClient: ReturnType<typeof useQueryClient>,
  agentId: string
): void {
  void queryClient.invalidateQueries({ queryKey: agentKeys.contextSizesForAgent(agentId) })
}

function openExternalDocs(href: string): void {
  window.open(href, '_blank', 'noopener,noreferrer')
}

export function AgentContextSizesCard({
  agentId,
  agentLabel
}: AgentContextSizesCardProps): React.JSX.Element {
  const queryClient = useQueryClient()
  const docs = AGENT_CONTEXT_DOCS[agentId]
  const defaultTokens = agentId === 'codex' ? '272000' : '128000'
  const [sizeId, setSizeId] = useState('')
  const [label, setLabel] = useState('')
  const [tokenCount, setTokenCount] = useState(defaultTokens)

  const sizesQuery = useQuery({
    queryKey: agentKeys.contextSizes(agentId, true),
    queryFn: () => listAgentContextSizes(agentId, true),
    staleTime: ONE_HOUR_MS
  })

  const addMutation = useMutation({
    mutationFn: (input: { sizeId: string; label: string; tokenCount: number }) =>
      addAgentContextSize(agentId, input.sizeId, input.label, input.tokenCount),
    onSuccess: () => {
      setSizeId('')
      setLabel('')
      setTokenCount(defaultTokens)
      invalidateContextSizeQueries(queryClient, agentId)
      toast.success('Context size added')
    },
    onError: (error: Error) => toast.error(error.message)
  })

  const toggleMutation = useMutation({
    mutationFn: ({ id, enabled }: { id: string; enabled: boolean }) =>
      updateAgentContextSizeEnabled(agentId, id, enabled),
    onSuccess: () => {
      invalidateContextSizeQueries(queryClient, agentId)
    },
    onError: (error: Error) => toast.error(error.message)
  })

  const removeMutation = useMutation({
    mutationFn: (id: string) => removeAgentContextSize(agentId, id),
    onSuccess: () => {
      invalidateContextSizeQueries(queryClient, agentId)
      toast.success('Context size removed')
    },
    onError: (error: Error) => toast.error(error.message)
  })

  const sizes = sizesQuery.data?.contextSizes ?? []
  const existingIds = new Set(sizes.map((size) => size.id))
  const isBusy = addMutation.isPending || toggleMutation.isPending || removeMutation.isPending

  function handleAdd(): void {
    const id = sizeId.trim()
    const tokens = Number(tokenCount)
    if (!id || !Number.isFinite(tokens) || tokens <= 0) {
      toast.error('Enter a size id and a positive token count.')
      return
    }

    addMutation.mutate({
      sizeId: id,
      label: label.trim() || id,
      tokenCount: tokens
    })
  }

  function handleAddPreset(preset: (typeof CODEX_CONTEXT_PRESETS)[number]): void {
    if (existingIds.has(preset.id)) {
      toast.error(`Context size '${preset.id}' already exists.`)
      return
    }

    addMutation.mutate({
      sizeId: preset.id,
      label: preset.label,
      tokenCount: preset.tokenCount
    })
  }

  return (
    <Card>
      <CardHeader className="flex flex-row items-start justify-between gap-4 space-y-0">
        <div className="space-y-1">
          <CardTitle>{agentLabel} context sizes</CardTitle>
          <CardDescription>
            {agentId === 'codex'
              ? 'Presets map to Codex model_context_window (tokens). Defaults follow the Codex CLI catalog.'
              : `Manually curated context window presets for ${agentLabel}. Used in mode defaults and the chat composer.`}
          </CardDescription>
        </div>
        {docs ? (
          <Button
            variant="outline"
            size="sm"
            className="shrink-0"
            onClick={() => openExternalDocs(docs.href)}
            aria-label={`Open ${docs.label} documentation`}
          >
            <BookOpen className="size-4" />
            Docs
            <ExternalLink className="size-3.5 opacity-70" />
          </Button>
        ) : null}
      </CardHeader>
      <CardContent className="space-y-4">
        {agentId === 'codex' ? (
          <div className="space-y-2">
            <p className="text-xs text-muted-foreground">
              Suggested Codex presets from the{' '}
              <button
                type="button"
                className="underline underline-offset-2"
                onClick={() =>
                  openExternalDocs('https://developers.openai.com/codex/config-advanced')
                }
              >
                advanced config docs
              </button>
              : compact 128k, default 272k, large 400k.
            </p>
            <div className="flex flex-wrap gap-2">
              {CODEX_CONTEXT_PRESETS.map((preset) => (
                <Button
                  key={preset.id}
                  variant="secondary"
                  size="sm"
                  disabled={isBusy || existingIds.has(preset.id)}
                  onClick={() => handleAddPreset(preset)}
                >
                  Add {preset.label} ({preset.tokenCount.toLocaleString()})
                </Button>
              ))}
            </div>
          </div>
        ) : null}

        <div className="grid gap-2 sm:grid-cols-3">
          <div className="space-y-1">
            <Label htmlFor={`${agentId}-size-id`}>Id</Label>
            <Input
              id={`${agentId}-size-id`}
              value={sizeId}
              onChange={(event) => setSizeId(event.target.value)}
              placeholder={agentId === 'codex' ? 'default' : 'medium'}
              disabled={isBusy}
            />
          </div>
          <div className="space-y-1">
            <Label htmlFor={`${agentId}-size-label`}>Label</Label>
            <Input
              id={`${agentId}-size-label`}
              value={label}
              onChange={(event) => setLabel(event.target.value)}
              placeholder={agentId === 'codex' ? 'Default' : 'Medium'}
              disabled={isBusy}
            />
          </div>
          <div className="space-y-1">
            <Label htmlFor={`${agentId}-size-tokens`}>Tokens</Label>
            <Input
              id={`${agentId}-size-tokens`}
              value={tokenCount}
              onChange={(event) => setTokenCount(event.target.value)}
              placeholder={defaultTokens}
              disabled={isBusy}
            />
          </div>
        </div>
        <Button size="sm" onClick={handleAdd} disabled={isBusy || !sizeId.trim()}>
          Add context size
        </Button>

        <Separator />

        <div className="space-y-2">
          {sizesQuery.isLoading ? (
            <p className="text-sm text-muted-foreground">Loading context sizes…</p>
          ) : sizes.length === 0 ? (
            <p className="text-sm text-muted-foreground">No context sizes yet.</p>
          ) : (
            sizes.map((size) => (
              <div
                key={size.id}
                className="flex items-center justify-between gap-3 rounded-md border px-3 py-2"
              >
                <div className="min-w-0">
                  <p className="truncate text-sm font-medium">{size.label}</p>
                  <p className="text-xs text-muted-foreground">
                    {size.id} · {size.tokenCount.toLocaleString()} tokens
                  </p>
                </div>
                <div className="flex shrink-0 items-center gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={isBusy}
                    onClick={() => toggleMutation.mutate({ id: size.id, enabled: !size.isEnabled })}
                  >
                    {size.isEnabled ? 'Disable' : 'Enable'}
                  </Button>
                  <Button
                    variant="ghost"
                    size="icon"
                    className="size-8"
                    disabled={isBusy}
                    aria-label={`Remove ${size.label}`}
                    onClick={() => removeMutation.mutate(size.id)}
                  >
                    <Trash2 className="size-4" />
                  </Button>
                </div>
              </div>
            ))
          )}
        </div>
      </CardContent>
    </Card>
  )
}
