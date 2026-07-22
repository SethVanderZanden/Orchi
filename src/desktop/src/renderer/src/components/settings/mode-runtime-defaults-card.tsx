import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ChevronDown } from 'lucide-react'

import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuTrigger
} from '@/components/ui/dropdown-menu'
import { Label } from '@/components/ui/label'
import { DEFAULT_MODEL_VALUE } from '@/components/chat/chat-model-selector'
import { DEFAULT_CONTEXT_SIZE_VALUE } from '@/components/chat/chat-context-size-selector'
import { DEFAULT_CLI_OPTION_VALUE } from '@/components/chat/chat-cli-option-selector'
import { formatModelReasoningLabel } from '@/lib/agents/format-runtime-label'
import { resolveAgentSettingsStrategy } from '@/lib/agents/settings/resolve-agent-settings-strategy'
import { listAgents, listAgentContextSizes } from '@/lib/chat/agent-context-sizes-api'
import { listAgentCliOptions } from '@/lib/chat/agent-cli-options-api'
import { listAgentModels } from '@/lib/chat/agent-models-api'
import {
  listModeRuntimeDefaults,
  updateModeRuntimeDefault
} from '@/lib/chat/mode-runtime-defaults-api'
import type { ModeRuntimeDefault, UpdateModeRuntimeDefaultRequest } from '@/lib/chat/types'
import { agentKeys } from '@/lib/query-keys'
import { useUserPreferences } from '@/hooks/use-user-preferences'
import { filterAgentsByEnabled } from '@/lib/user-preferences/enabled-agents'

const ONE_HOUR_MS = 60 * 60 * 1000

function toModelRadio(modelId: string | null): string {
  return modelId ?? DEFAULT_MODEL_VALUE
}

function fromModelRadio(value: string): string | null {
  return value === DEFAULT_MODEL_VALUE ? null : value
}

function toContextRadio(contextSizeId: string | null): string {
  return contextSizeId ?? DEFAULT_CONTEXT_SIZE_VALUE
}

function fromContextRadio(value: string): string | null {
  return value === DEFAULT_CONTEXT_SIZE_VALUE ? null : value
}

function toCliRadio(optionId: string | null): string {
  return optionId ?? DEFAULT_CLI_OPTION_VALUE
}

function fromCliRadio(value: string): string | null {
  return value === DEFAULT_CLI_OPTION_VALUE ? null : value
}

type RuntimePatch = UpdateModeRuntimeDefaultRequest

function ModeDefaultRow({
  row,
  agents,
  onSaved,
  onError
}: {
  row: ModeRuntimeDefault
  agents: Array<{ id: string; label: string }>
  onSaved: (updated: ModeRuntimeDefault) => void
  onError: (mode: string, message: string) => void
}): React.JSX.Element {
  const modelsQuery = useQuery({
    queryKey: agentKeys.models(row.agentId, false),
    queryFn: () => listAgentModels(row.agentId, false),
    staleTime: ONE_HOUR_MS
  })

  const contextQuery = useQuery({
    queryKey: agentKeys.contextSizes(row.agentId, false),
    queryFn: () => listAgentContextSizes(row.agentId, false),
    staleTime: ONE_HOUR_MS
  })

  const reasoningQuery = useQuery({
    queryKey: agentKeys.cliOptions(row.agentId, 'model_reasoning_effort', false),
    queryFn: () => listAgentCliOptions(row.agentId, 'model_reasoning_effort', false),
    staleTime: ONE_HOUR_MS,
    enabled: resolveAgentSettingsStrategy(row.agentId).capabilities.has('reasoningEffort')
  })

  const approvalQuery = useQuery({
    queryKey: agentKeys.cliOptions(row.agentId, 'approval_policy', false),
    queryFn: () => listAgentCliOptions(row.agentId, 'approval_policy', false),
    staleTime: ONE_HOUR_MS,
    enabled: resolveAgentSettingsStrategy(row.agentId).capabilities.has('approvalPolicy')
  })

  const updateMutation = useMutation({
    mutationFn: (next: RuntimePatch) => updateModeRuntimeDefault(row.mode, next),
    onSuccess: (updated) => onSaved(updated),
    onError: (error: Error) => onError(row.mode, error.message)
  })

  const models = modelsQuery.data?.models ?? []
  const contextSizes = contextQuery.data?.contextSizes ?? []
  const reasoningOptions = reasoningQuery.data?.options ?? []
  const approvalOptions = approvalQuery.data?.options ?? []
  const strategy = resolveAgentSettingsStrategy(row.agentId)
  const showReasoning = strategy.capabilities.has('reasoningEffort')
  const showApproval = strategy.capabilities.has('approvalPolicy')
  const agentLabel = agents.find((agent) => agent.id === row.agentId)?.label ?? row.agentId
  const modelLabel = models.find((model) => model.id === row.modelId)?.label ?? 'Default (CLI)'
  const contextLabel =
    contextSizes.find((size) => size.id === row.contextSizeId)?.label ?? 'Default (CLI)'
  const reasoningLabel =
    reasoningOptions.find((option) => option.id === row.reasoningEffortId)?.label ?? 'Default (CLI)'
  const approvalLabel =
    approvalOptions.find((option) => option.id === row.approvalPolicyId)?.label ?? 'Default (CLI)'
  const combinedRuntimeLabel = formatModelReasoningLabel(
    row.agentId,
    row.modelId ? modelLabel : null,
    showReasoning && row.reasoningEffortId ? reasoningLabel : null
  )

  function currentPatch(overrides: Partial<RuntimePatch> = {}): RuntimePatch {
    return {
      agentId: row.agentId,
      modelId: row.modelId,
      contextSizeId: row.contextSizeId,
      reasoningEffortId: row.reasoningEffortId,
      approvalPolicyId: row.approvalPolicyId,
      ...overrides
    }
  }

  return (
    <div className="space-y-2 rounded-lg border p-3">
      <div>
        <p className="text-sm font-medium">{row.label}</p>
        <p className="text-xs text-muted-foreground">
          {row.mode}
          {showReasoning ? ` · ${combinedRuntimeLabel}` : null}
        </p>
      </div>
      <div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-3">
        <div className="space-y-1">
          <Label className="text-xs text-muted-foreground">Agent</Label>
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                variant="outline"
                size="sm"
                className="h-8 w-full justify-between gap-1.5 text-xs font-normal"
                disabled={updateMutation.isPending}
              >
                <span className="truncate">{agentLabel}</span>
                <ChevronDown className="size-3.5 opacity-60" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="start" className="max-h-64 overflow-y-auto">
              <DropdownMenuRadioGroup
                value={row.agentId}
                onValueChange={(agentId) =>
                  updateMutation.mutate(
                    currentPatch({
                      agentId,
                      modelId: null,
                      contextSizeId: null,
                      reasoningEffortId: null,
                      approvalPolicyId: null
                    })
                  )
                }
              >
                {agents.map((agent) => (
                  <DropdownMenuRadioItem key={agent.id} value={agent.id}>
                    {agent.label}
                  </DropdownMenuRadioItem>
                ))}
              </DropdownMenuRadioGroup>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>

        <div className="space-y-1">
          <Label className="text-xs text-muted-foreground">Model</Label>
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                variant="outline"
                size="sm"
                className="h-8 w-full justify-between gap-1.5 text-xs font-normal"
                disabled={updateMutation.isPending || modelsQuery.isLoading}
              >
                <span className="truncate">{modelLabel}</span>
                <ChevronDown className="size-3.5 opacity-60" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="start" className="max-h-64 overflow-y-auto">
              <DropdownMenuRadioGroup
                value={toModelRadio(row.modelId)}
                onValueChange={(value) =>
                  updateMutation.mutate(currentPatch({ modelId: fromModelRadio(value) }))
                }
              >
                <DropdownMenuRadioItem value={DEFAULT_MODEL_VALUE}>
                  Default (CLI)
                </DropdownMenuRadioItem>
                {models.map((model) => (
                  <DropdownMenuRadioItem key={model.id} value={model.id}>
                    {model.label}
                  </DropdownMenuRadioItem>
                ))}
              </DropdownMenuRadioGroup>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>

        <div className="space-y-1">
          <Label className="text-xs text-muted-foreground">Context</Label>
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                variant="outline"
                size="sm"
                className="h-8 w-full justify-between gap-1.5 text-xs font-normal"
                disabled={updateMutation.isPending || contextQuery.isLoading}
              >
                <span className="truncate">{contextLabel}</span>
                <ChevronDown className="size-3.5 opacity-60" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="start" className="max-h-64 overflow-y-auto">
              <DropdownMenuRadioGroup
                value={toContextRadio(row.contextSizeId)}
                onValueChange={(value) =>
                  updateMutation.mutate(currentPatch({ contextSizeId: fromContextRadio(value) }))
                }
              >
                <DropdownMenuRadioItem value={DEFAULT_CONTEXT_SIZE_VALUE}>
                  Default (CLI)
                </DropdownMenuRadioItem>
                {contextSizes.map((size) => (
                  <DropdownMenuRadioItem key={size.id} value={size.id}>
                    {size.label}
                  </DropdownMenuRadioItem>
                ))}
              </DropdownMenuRadioGroup>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>

        {showReasoning ? (
          <div className="space-y-1">
            <Label className="text-xs text-muted-foreground">Reasoning</Label>
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button
                  variant="outline"
                  size="sm"
                  className="h-8 w-full justify-between gap-1.5 text-xs font-normal"
                  disabled={updateMutation.isPending || reasoningQuery.isLoading}
                >
                  <span className="truncate">{reasoningLabel}</span>
                  <ChevronDown className="size-3.5 opacity-60" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="start" className="max-h-64 overflow-y-auto">
                <DropdownMenuRadioGroup
                  value={toCliRadio(row.reasoningEffortId)}
                  onValueChange={(value) =>
                    updateMutation.mutate(currentPatch({ reasoningEffortId: fromCliRadio(value) }))
                  }
                >
                  <DropdownMenuRadioItem value={DEFAULT_CLI_OPTION_VALUE}>
                    Default (CLI)
                  </DropdownMenuRadioItem>
                  {reasoningOptions.map((option) => (
                    <DropdownMenuRadioItem key={option.id} value={option.id}>
                      {option.label}
                    </DropdownMenuRadioItem>
                  ))}
                </DropdownMenuRadioGroup>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        ) : null}

        {showApproval ? (
          <div className="space-y-1">
            <Label className="text-xs text-muted-foreground">Approval</Label>
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button
                  variant="outline"
                  size="sm"
                  className="h-8 w-full justify-between gap-1.5 text-xs font-normal"
                  disabled={updateMutation.isPending || approvalQuery.isLoading}
                >
                  <span className="truncate">{approvalLabel}</span>
                  <ChevronDown className="size-3.5 opacity-60" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="start" className="max-h-64 overflow-y-auto">
                <DropdownMenuRadioGroup
                  value={toCliRadio(row.approvalPolicyId)}
                  onValueChange={(value) =>
                    updateMutation.mutate(currentPatch({ approvalPolicyId: fromCliRadio(value) }))
                  }
                >
                  <DropdownMenuRadioItem value={DEFAULT_CLI_OPTION_VALUE}>
                    Default (CLI)
                  </DropdownMenuRadioItem>
                  {approvalOptions.map((option) => (
                    <DropdownMenuRadioItem key={option.id} value={option.id}>
                      {option.label}
                    </DropdownMenuRadioItem>
                  ))}
                </DropdownMenuRadioGroup>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        ) : null}
      </div>
    </div>
  )
}

export function ModeRuntimeDefaultsCard(): React.JSX.Element {
  const queryClient = useQueryClient()
  const { enabledAgentIds } = useUserPreferences()
  const [rowErrors, setRowErrors] = useState<Record<string, string>>({})

  const defaultsQuery = useQuery({
    queryKey: agentKeys.modeDefaults(),
    queryFn: listModeRuntimeDefaults,
    staleTime: ONE_HOUR_MS
  })

  const agentsQuery = useQuery({
    queryKey: agentKeys.list(),
    queryFn: listAgents,
    staleTime: ONE_HOUR_MS
  })

  const defaults = defaultsQuery.data?.defaults ?? []
  const agents = filterAgentsByEnabled(agentsQuery.data ?? [], enabledAgentIds)

  return (
    <Card>
      <CardHeader>
        <CardTitle>Mode defaults</CardTitle>
        <CardDescription>
          Choose agent, model, context size, and (for Codex) reasoning effort and approval policy
          for each chat mode. Codex pairs look like “5.6 Terra Medium” — model plus reasoning.
          New chats and mode switches use these defaults.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-3">
        {defaults.map((row) => (
          <div key={row.mode} className="space-y-1">
            <ModeDefaultRow
              row={row}
              agents={agents}
              onSaved={(updated) => {
                setRowErrors((current) => {
                  const next = { ...current }
                  delete next[updated.mode]
                  return next
                })
                queryClient.setQueryData(
                  agentKeys.modeDefaults(),
                  (current: { defaults: ModeRuntimeDefault[] } | undefined) => {
                    if (!current) {
                      return current
                    }

                    return {
                      defaults: current.defaults.map((entry) =>
                        entry.mode === updated.mode ? updated : entry
                      )
                    }
                  }
                )
              }}
              onError={(mode, message) =>
                setRowErrors((current) => ({ ...current, [mode]: message }))
              }
            />
            {rowErrors[row.mode] ? (
              <p className="text-[11px] text-destructive">{rowErrors[row.mode]}</p>
            ) : null}
          </div>
        ))}
        {defaultsQuery.isLoading ? (
          <p className="text-sm text-muted-foreground">Loading mode defaults…</p>
        ) : null}
      </CardContent>
    </Card>
  )
}
