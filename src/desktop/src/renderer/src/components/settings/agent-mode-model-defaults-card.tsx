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
import { listAgentModels } from '@/lib/chat/agent-models-api'
import {
  listAgentModeModelDefaults,
  updateAgentModeModelDefault
} from '@/lib/chat/agent-mode-model-defaults-api'
import type { AgentModeModelDefault } from '@/lib/chat/types'
import { agentKeys } from '@/lib/query-keys'

const ONE_HOUR_MS = 60 * 60 * 1000

type AgentModeModelDefaultsCardProps = {
  agentId: string
}

function toRadioValue(modelId: string | null): string {
  return modelId ?? DEFAULT_MODEL_VALUE
}

function fromRadioValue(value: string): string | null {
  return value === DEFAULT_MODEL_VALUE ? null : value
}

function modeRowDescription(mode: string): string | null {
  if (mode === 'implementation') {
    return 'Applies to plan agents kicked off from orchestration (hidden from chat mode picker).'
  }

  return null
}

export function AgentModeModelDefaultsCard({
  agentId
}: AgentModeModelDefaultsCardProps): React.JSX.Element {
  const queryClient = useQueryClient()
  const [rowErrors, setRowErrors] = useState<Record<string, string>>({})

  const defaultsQuery = useQuery({
    queryKey: agentKeys.modeModelDefaults(agentId),
    queryFn: () => listAgentModeModelDefaults(agentId),
    staleTime: ONE_HOUR_MS
  })

  const modelsQuery = useQuery({
    queryKey: agentKeys.models(agentId),
    queryFn: () => listAgentModels(agentId, false),
    staleTime: ONE_HOUR_MS
  })

  const updateMutation = useMutation({
    mutationFn: ({ mode, modelId }: { mode: string; modelId: string | null }) =>
      updateAgentModeModelDefault(agentId, mode, modelId),
    onSuccess: (updated) => {
      setRowErrors((current) => {
        const next = { ...current }
        delete next[updated.mode]
        return next
      })

      queryClient.setQueryData(
        agentKeys.modeModelDefaults(agentId),
        (current: { defaults: AgentModeModelDefault[] } | undefined) => {
          if (!current) {
            return current
          }

          return {
            defaults: current.defaults.map((row) => (row.mode === updated.mode ? updated : row))
          }
        }
      )
    },
    onError: (error: Error, variables) => {
      setRowErrors((current) => ({ ...current, [variables.mode]: error.message }))
    }
  })

  const enabledModels = modelsQuery.data?.models ?? []
  const defaults = defaultsQuery.data?.defaults ?? []
  const isLoading = defaultsQuery.isLoading || modelsQuery.isLoading
  const loadError = defaultsQuery.error ?? modelsQuery.error

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">Mode default models</CardTitle>
        <CardDescription>
          Defaults apply to new chats in each mode and to kicked-off implementation and review
          children. Per-chat model selection still overrides these defaults.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {isLoading ? (
          <p className="text-sm text-muted-foreground">Loading mode defaults…</p>
        ) : loadError ? (
          <div className="space-y-2">
            <p className="text-sm text-destructive">
              {loadError instanceof Error ? loadError.message : 'Failed to load mode defaults.'}
            </p>
          </div>
        ) : (
          <ul className="divide-y rounded-lg border">
            {defaults.map((row) => {
              const description = modeRowDescription(row.mode)
              const selectedModel = enabledModels.find((model) => model.id === row.modelId)
              const triggerLabel = selectedModel?.label ?? 'Default (CLI)'

              return (
                <li key={row.mode} className="space-y-2 px-3 py-3">
                  <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                    <div className="min-w-0 space-y-0.5">
                      <Label htmlFor={`mode-default-${row.mode}`}>{row.label}</Label>
                      {description ? (
                        <p className="text-xs text-muted-foreground">{description}</p>
                      ) : null}
                    </div>
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild>
                        <Button
                          id={`mode-default-${row.mode}`}
                          variant="outline"
                          size="sm"
                          disabled={updateMutation.isPending}
                          className="h-8 w-full justify-between gap-1.5 px-2.5 text-xs font-normal sm:w-56"
                          aria-label={`Default model for ${row.label}`}
                        >
                          <span className="truncate">{triggerLabel}</span>
                          <ChevronDown className="size-3.5 shrink-0 opacity-60" />
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="end" className="max-h-64 overflow-y-auto">
                        <DropdownMenuRadioGroup
                          value={toRadioValue(row.modelId)}
                          onValueChange={(value) =>
                            updateMutation.mutate({
                              mode: row.mode,
                              modelId: fromRadioValue(value)
                            })
                          }
                        >
                          <DropdownMenuRadioItem value={DEFAULT_MODEL_VALUE}>
                            Default (CLI)
                          </DropdownMenuRadioItem>
                          {enabledModels.map((model) => (
                            <DropdownMenuRadioItem key={model.id} value={model.id}>
                              {model.label}
                            </DropdownMenuRadioItem>
                          ))}
                        </DropdownMenuRadioGroup>
                      </DropdownMenuContent>
                    </DropdownMenu>
                  </div>
                  {rowErrors[row.mode] ? (
                    <p className="text-sm text-destructive">{rowErrors[row.mode]}</p>
                  ) : null}
                </li>
              )
            })}
          </ul>
        )}
      </CardContent>
    </Card>
  )
}
