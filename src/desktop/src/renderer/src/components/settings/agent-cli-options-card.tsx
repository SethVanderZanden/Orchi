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
  addAgentCliOption,
  listAgentCliOptions,
  removeAgentCliOption,
  updateAgentCliOptionEnabled
} from '@/lib/chat/agent-cli-options-api'
import type { AgentCliOptionKind } from '@/lib/chat/types'
import { agentKeys } from '@/lib/query-keys'

const ONE_HOUR_MS = 60 * 60 * 1000

const CODEX_REASONING_PRESETS = [
  { id: 'none', label: 'None', cliValue: 'none' },
  { id: 'minimal', label: 'Minimal', cliValue: 'minimal' },
  { id: 'low', label: 'Low', cliValue: 'low' },
  { id: 'medium', label: 'Medium', cliValue: 'medium' },
  { id: 'high', label: 'High', cliValue: 'high' },
  { id: 'xhigh', label: 'Extra high', cliValue: 'xhigh' },
  { id: 'max', label: 'Max', cliValue: 'max' }
] as const

const CODEX_APPROVAL_PRESETS = [
  { id: 'untrusted', label: 'Untrusted', cliValue: 'untrusted' },
  { id: 'on-request', label: 'On request', cliValue: 'on-request' },
  { id: 'never', label: 'Never', cliValue: 'never' }
] as const

const KIND_META: Record<
  AgentCliOptionKind,
  {
    title: string
    description: string
    codexDescription: string
    docsLabel: string
    docsHref: string
    presets: ReadonlyArray<{ id: string; label: string; cliValue: string }>
  }
> = {
  model_reasoning_effort: {
    title: 'reasoning effort',
    description:
      'Values for Codex -c model_reasoning_effort. Used in mode defaults and the chat composer.',
    codexDescription:
      'Presets map to Codex model_reasoning_effort. Defaults follow the Codex advanced config docs.',
    docsLabel: 'Codex model_reasoning_effort',
    docsHref: 'https://developers.openai.com/codex/config-advanced',
    presets: CODEX_REASONING_PRESETS
  },
  approval_policy: {
    title: 'approval policy',
    description:
      'Values for Codex -c approval_policy. Used in mode defaults and the chat composer.',
    codexDescription:
      'Presets map to Codex approval_policy. Defaults follow the Codex advanced config docs.',
    docsLabel: 'Codex approval_policy',
    docsHref: 'https://developers.openai.com/codex/config-advanced',
    presets: CODEX_APPROVAL_PRESETS
  }
}

type AgentCliOptionsCardProps = {
  agentId: string
  agentLabel: string
  kind: AgentCliOptionKind
}

function invalidateCliOptionQueries(
  queryClient: ReturnType<typeof useQueryClient>,
  agentId: string
): void {
  void queryClient.invalidateQueries({ queryKey: agentKeys.cliOptionsForAgent(agentId) })
}

function openExternalDocs(href: string): void {
  window.open(href, '_blank', 'noopener,noreferrer')
}

export function AgentCliOptionsCard({
  agentId,
  agentLabel,
  kind
}: AgentCliOptionsCardProps): React.JSX.Element {
  const queryClient = useQueryClient()
  const meta = KIND_META[kind]
  const [optionId, setOptionId] = useState('')
  const [label, setLabel] = useState('')
  const [cliValue, setCliValue] = useState('')

  const optionsQuery = useQuery({
    queryKey: agentKeys.cliOptions(agentId, kind, true),
    queryFn: () => listAgentCliOptions(agentId, kind, true),
    staleTime: ONE_HOUR_MS
  })

  const addMutation = useMutation({
    mutationFn: (input: { optionId: string; label: string; cliValue: string }) =>
      addAgentCliOption(agentId, kind, input.optionId, input.label, input.cliValue),
    onSuccess: () => {
      setOptionId('')
      setLabel('')
      setCliValue('')
      invalidateCliOptionQueries(queryClient, agentId)
      toast.success('CLI option added')
    },
    onError: (error: Error) => toast.error(error.message)
  })

  const toggleMutation = useMutation({
    mutationFn: ({ id, enabled }: { id: string; enabled: boolean }) =>
      updateAgentCliOptionEnabled(agentId, kind, id, enabled),
    onSuccess: () => {
      invalidateCliOptionQueries(queryClient, agentId)
    },
    onError: (error: Error) => toast.error(error.message)
  })

  const removeMutation = useMutation({
    mutationFn: (id: string) => removeAgentCliOption(agentId, kind, id),
    onSuccess: () => {
      invalidateCliOptionQueries(queryClient, agentId)
      toast.success('CLI option removed')
    },
    onError: (error: Error) => toast.error(error.message)
  })

  const options = optionsQuery.data?.options ?? []
  const existingIds = new Set(options.map((option) => option.id))
  const isBusy = addMutation.isPending || toggleMutation.isPending || removeMutation.isPending

  function handleAdd(): void {
    const id = optionId.trim()
    if (!id) {
      toast.error('Enter an option id.')
      return
    }

    addMutation.mutate({
      optionId: id,
      label: label.trim() || id,
      cliValue: cliValue.trim() || id
    })
  }

  function handleAddPreset(preset: { id: string; label: string; cliValue: string }): void {
    if (existingIds.has(preset.id)) {
      toast.error(`Option '${preset.id}' already exists.`)
      return
    }

    addMutation.mutate({
      optionId: preset.id,
      label: preset.label,
      cliValue: preset.cliValue
    })
  }

  return (
    <Card>
      <CardHeader className="flex flex-row items-start justify-between gap-4 space-y-0">
        <div className="space-y-1">
          <CardTitle>
            {agentLabel} {meta.title}
          </CardTitle>
          <CardDescription>
            {agentId === 'codex' ? meta.codexDescription : meta.description}
          </CardDescription>
        </div>
        <Button
          variant="outline"
          size="sm"
          className="shrink-0"
          onClick={() => openExternalDocs(meta.docsHref)}
          aria-label={`Open ${meta.docsLabel} documentation`}
        >
          <BookOpen className="size-4" />
          Docs
          <ExternalLink className="size-3.5 opacity-70" />
        </Button>
      </CardHeader>
      <CardContent className="space-y-4">
        {agentId === 'codex' ? (
          <div className="space-y-2">
            <p className="text-xs text-muted-foreground">
              Suggested Codex presets from the{' '}
              <button
                type="button"
                className="underline underline-offset-2"
                onClick={() => openExternalDocs(meta.docsHref)}
              >
                advanced config docs
              </button>
              .
            </p>
            <div className="flex flex-wrap gap-2">
              {meta.presets.map((preset) => (
                <Button
                  key={preset.id}
                  variant="secondary"
                  size="sm"
                  disabled={isBusy || existingIds.has(preset.id)}
                  onClick={() => handleAddPreset(preset)}
                >
                  Add {preset.label}
                </Button>
              ))}
            </div>
          </div>
        ) : null}

        <div className="grid gap-2 sm:grid-cols-3">
          <div className="space-y-1">
            <Label htmlFor={`${agentId}-${kind}-id`}>Id</Label>
            <Input
              id={`${agentId}-${kind}-id`}
              value={optionId}
              onChange={(event) => setOptionId(event.target.value)}
              placeholder={kind === 'approval_policy' ? 'on-request' : 'medium'}
              disabled={isBusy}
            />
          </div>
          <div className="space-y-1">
            <Label htmlFor={`${agentId}-${kind}-label`}>Label</Label>
            <Input
              id={`${agentId}-${kind}-label`}
              value={label}
              onChange={(event) => setLabel(event.target.value)}
              placeholder={kind === 'approval_policy' ? 'On request' : 'Medium'}
              disabled={isBusy}
            />
          </div>
          <div className="space-y-1">
            <Label htmlFor={`${agentId}-${kind}-cli`}>CLI value</Label>
            <Input
              id={`${agentId}-${kind}-cli`}
              value={cliValue}
              onChange={(event) => setCliValue(event.target.value)}
              placeholder={kind === 'approval_policy' ? 'on-request' : 'medium'}
              disabled={isBusy}
            />
          </div>
        </div>
        <Button size="sm" onClick={handleAdd} disabled={isBusy || !optionId.trim()}>
          Add option
        </Button>

        <Separator />

        <div className="space-y-2">
          {optionsQuery.isLoading ? (
            <p className="text-sm text-muted-foreground">Loading options…</p>
          ) : options.length === 0 ? (
            <p className="text-sm text-muted-foreground">No options yet.</p>
          ) : (
            options.map((option) => (
              <div
                key={option.id}
                className="flex items-center justify-between gap-3 rounded-md border px-3 py-2"
              >
                <div className="min-w-0">
                  <p className="truncate text-sm font-medium">{option.label}</p>
                  <p className="text-xs text-muted-foreground">
                    {option.id} · {option.cliValue}
                  </p>
                </div>
                <div className="flex shrink-0 items-center gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    disabled={isBusy}
                    onClick={() =>
                      toggleMutation.mutate({ id: option.id, enabled: !option.isEnabled })
                    }
                  >
                    {option.isEnabled ? 'Disable' : 'Enable'}
                  </Button>
                  <Button
                    variant="ghost"
                    size="icon"
                    className="size-8"
                    disabled={isBusy}
                    aria-label={`Remove ${option.label}`}
                    onClick={() => removeMutation.mutate(option.id)}
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
