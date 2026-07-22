import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ChevronDown } from 'lucide-react'

import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from '@/components/ui/dialog'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuTrigger
} from '@/components/ui/dropdown-menu'
import { Label } from '@/components/ui/label'
import { useUserPreferences } from '@/hooks/use-user-preferences'
import {
  CODEX_MODEL_PRESETS,
  CODEX_REASONING_PRESETS,
  DEFAULT_CODEX_MODEL_ID,
  DEFAULT_CODEX_REASONING_EFFORT_ID,
  MODE_DEFAULT_SETUP_MODES,
  formatCodexRuntimeLabel
} from '@/lib/agents/codex-presets'
import { resolveAgentSettingsStrategy } from '@/lib/agents/settings/resolve-agent-settings-strategy'
import { listAgentModels } from '@/lib/chat/agent-models-api'
import { listAgentCliOptions } from '@/lib/chat/agent-cli-options-api'
import {
  listModeRuntimeDefaults,
  updateModeRuntimeDefault
} from '@/lib/chat/mode-runtime-defaults-api'
import type { ModeRuntimeDefault, UpdateModeRuntimeDefaultRequest } from '@/lib/chat/types'
import { agentKeys } from '@/lib/query-keys'
import {
  CODEX_APPROVAL_SETUP_OPTIONS,
  DEFAULT_CODEX_APPROVAL_POLICY_ID,
  type CodexApprovalSetupOptionId
} from '@/lib/user-preferences/codex-setup'
import { getAvailableAgentOptions } from '@/lib/user-preferences/enabled-agents'
import { cn } from '@/lib/utils'

type AgentSetupWizardDialogProps = {
  mandatory?: boolean
  open: boolean
  onOpenChange?: (open: boolean) => void
}

type SetupStep = 'agents' | 'codex-approvals' | 'mode-defaults'

type ModeDraft = {
  mode: string
  label: string
  description: string
  agentId: string
  modelId: string | null
  reasoningEffortId: string | null
  approvalPolicyId: string | null
}

function buildInitialModeDrafts(
  agentIds: string[],
  approvalPolicyId: CodexApprovalSetupOptionId | null
): ModeDraft[] {
  return MODE_DEFAULT_SETUP_MODES.map((entry) => {
    const preferred =
      entry.mode === 'default'
        ? agentIds.includes('cursor')
          ? 'cursor'
          : agentIds[0]!
        : agentIds.includes('codex')
          ? 'codex'
          : agentIds[0]!

    const usesCodex = preferred === 'codex'

    return {
      mode: entry.mode,
      label: entry.label,
      description: entry.description,
      agentId: preferred,
      modelId: usesCodex ? entry.suggestedModelId : null,
      reasoningEffortId: usesCodex ? entry.suggestedReasoningEffortId : null,
      approvalPolicyId: usesCodex ? (approvalPolicyId ?? DEFAULT_CODEX_APPROVAL_POLICY_ID) : null
    }
  })
}

function ModeDefaultSetupRow({
  draft,
  agents,
  disabled,
  onChange
}: {
  draft: ModeDraft
  agents: Array<{ id: string; label: string }>
  disabled: boolean
  onChange: (next: ModeDraft) => void
}): React.JSX.Element {
  const strategy = resolveAgentSettingsStrategy(draft.agentId)
  const showReasoning = strategy.capabilities.has('reasoningEffort')
  const usesCodex = draft.agentId === 'codex'

  const modelsQuery = useQuery({
    queryKey: agentKeys.models(draft.agentId, false),
    queryFn: () => listAgentModels(draft.agentId, false),
    staleTime: 60 * 60 * 1000,
    enabled: !usesCodex
  })

  const catalogModels = usesCodex
    ? CODEX_MODEL_PRESETS.map((preset) => ({ id: preset.id, label: preset.label }))
    : (modelsQuery.data?.models ?? [])

  const reasoningOptions = CODEX_REASONING_PRESETS
  const agentLabel = agents.find((agent) => agent.id === draft.agentId)?.label ?? draft.agentId
  const modelLabel =
    catalogModels.find((model) => model.id === draft.modelId)?.label ??
    (draft.modelId ? draft.modelId : 'Default (CLI)')
  const reasoningLabel =
    reasoningOptions.find((option) => option.id === draft.reasoningEffortId)?.label ??
    'Default (CLI)'
  const summary = usesCodex
    ? formatCodexRuntimeLabel(
        draft.modelId ? modelLabel : null,
        draft.reasoningEffortId ? reasoningLabel : null
      )
    : modelLabel

  return (
    <div className="space-y-2 rounded-lg border p-3">
      <div>
        <p className="text-sm font-medium">{draft.label}</p>
        <p className="text-xs text-muted-foreground">{draft.description}</p>
        <p className="mt-1 text-xs text-muted-foreground">Preview: {summary}</p>
      </div>

      <div className={cn('grid gap-2', showReasoning ? 'sm:grid-cols-3' : 'sm:grid-cols-2')}>
        <div className="space-y-1">
          <Label className="text-xs text-muted-foreground">Agent</Label>
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                variant="outline"
                size="sm"
                className="h-8 w-full justify-between gap-1.5 text-xs font-normal"
                disabled={disabled || agents.length <= 1}
              >
                <span className="truncate">{agentLabel}</span>
                <ChevronDown className="size-3.5 opacity-60" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="start" className="max-h-64 overflow-y-auto">
              <DropdownMenuRadioGroup
                value={draft.agentId}
                onValueChange={(agentId) => {
                  const nextUsesCodex = agentId === 'codex'
                  const suggestion = MODE_DEFAULT_SETUP_MODES.find((mode) => mode.mode === draft.mode)
                  onChange({
                    ...draft,
                    agentId,
                    modelId: nextUsesCodex ? (suggestion?.suggestedModelId ?? DEFAULT_CODEX_MODEL_ID) : null,
                    reasoningEffortId: nextUsesCodex
                      ? (suggestion?.suggestedReasoningEffortId ?? DEFAULT_CODEX_REASONING_EFFORT_ID)
                      : null,
                    approvalPolicyId: nextUsesCodex
                      ? (draft.approvalPolicyId ?? DEFAULT_CODEX_APPROVAL_POLICY_ID)
                      : null
                  })
                }}
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
                disabled={disabled || (!usesCodex && modelsQuery.isLoading)}
              >
                <span className="truncate">{modelLabel}</span>
                <ChevronDown className="size-3.5 opacity-60" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="start" className="max-h-64 overflow-y-auto">
              <DropdownMenuRadioGroup
                value={draft.modelId ?? '__default__'}
                onValueChange={(value) =>
                  onChange({
                    ...draft,
                    modelId: value === '__default__' ? null : value
                  })
                }
              >
                {!usesCodex ? (
                  <DropdownMenuRadioItem value="__default__">Default (CLI)</DropdownMenuRadioItem>
                ) : null}
                {catalogModels.map((model) => (
                  <DropdownMenuRadioItem key={model.id} value={model.id}>
                    {model.label}
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
                  disabled={disabled}
                >
                  <span className="truncate">{reasoningLabel}</span>
                  <ChevronDown className="size-3.5 opacity-60" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="start" className="max-h-64 overflow-y-auto">
                <DropdownMenuRadioGroup
                  value={draft.reasoningEffortId ?? '__default__'}
                  onValueChange={(value) =>
                    onChange({
                      ...draft,
                      reasoningEffortId: value === '__default__' ? null : value
                    })
                  }
                >
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
      </div>
    </div>
  )
}

export function AgentSetupWizardDialog({
  mandatory = false,
  open,
  onOpenChange
}: AgentSetupWizardDialogProps): React.JSX.Element {
  const queryClient = useQueryClient()
  const { setEnabledAgentIds, isUpdating } = useUserPreferences()
  const options = getAvailableAgentOptions()
  const [step, setStep] = useState<SetupStep>('agents')
  const [selectedAgentIds, setSelectedAgentIds] = useState<string[]>([])
  const [approvalPolicyId, setApprovalPolicyId] = useState<CodexApprovalSetupOptionId>(
    DEFAULT_CODEX_APPROVAL_POLICY_ID
  )
  const [modeDrafts, setModeDrafts] = useState<ModeDraft[]>([])
  const [error, setError] = useState<string | null>(null)
  const [savingDefaults, setSavingDefaults] = useState(false)
  const [openKey, setOpenKey] = useState(open ? 'open' : 'closed')
  const [prevOpen, setPrevOpen] = useState(open)

  if (open !== prevOpen) {
    setPrevOpen(open)
    if (open) {
      setStep('agents')
      setSelectedAgentIds([])
      setApprovalPolicyId(DEFAULT_CODEX_APPROVAL_POLICY_ID)
      setModeDrafts([])
      setError(null)
      setOpenKey(`open:${Date.now()}`)
    }
  }

  const codexSelected = selectedAgentIds.includes('codex')
  const busy = isUpdating || savingDefaults

  const saveDefaultsMutation = useMutation({
    mutationFn: async (drafts: ModeDraft[]) => {
      const results: ModeRuntimeDefault[] = []
      for (const draft of drafts) {
        const request: UpdateModeRuntimeDefaultRequest = {
          agentId: draft.agentId,
          modelId: draft.modelId,
          contextSizeId: null,
          reasoningEffortId: draft.reasoningEffortId,
          approvalPolicyId: draft.approvalPolicyId
        }
        results.push(await updateModeRuntimeDefault(draft.mode, request))
      }
      return results
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: agentKeys.modeDefaults() })
      await queryClient.invalidateQueries({ queryKey: agentKeys.modelsForAgent('codex') })
    }
  })

  function toggleAgent(agentId: string): void {
    setError(null)
    setSelectedAgentIds((current) => {
      if (current.includes(agentId)) {
        return current.filter((id) => id !== agentId)
      }

      return [...current, agentId]
    })
  }

  function handleAgentsContinue(): void {
    if (selectedAgentIds.length === 0) {
      setError('Select at least one agent from the list.')
      return
    }

    if (selectedAgentIds.includes('codex')) {
      setStep('codex-approvals')
      return
    }

    setModeDrafts(buildInitialModeDrafts(selectedAgentIds, null))
    setStep('mode-defaults')
  }

  function handleApprovalsContinue(): void {
    setModeDrafts(buildInitialModeDrafts(selectedAgentIds, approvalPolicyId))
    setStep('mode-defaults')
  }

  async function completeSetup(): Promise<void> {
    setSavingDefaults(true)
    setError(null)

    try {
      await setEnabledAgentIds(selectedAgentIds, {
        codexApprovalPolicyId: codexSelected ? approvalPolicyId : null,
        codexReasoningEffortId: codexSelected ? DEFAULT_CODEX_REASONING_EFFORT_ID : null
      })

      // Ensure Codex catalog queries are fresh before writing mode defaults that
      // reference built-in Sol/Terra/Luna + reasoning presets.
      if (codexSelected) {
        await listAgentModels('codex', true)
        await listAgentCliOptions('codex', 'model_reasoning_effort', true)
        await listModeRuntimeDefaults()
      }

      await saveDefaultsMutation.mutateAsync(modeDrafts)
      onOpenChange?.(false)
    } catch (saveError) {
      setError(saveError instanceof Error ? saveError.message : 'Unable to save agent preferences.')
    } finally {
      setSavingDefaults(false)
    }
  }

  const selectedAgents = options.filter((option) => selectedAgentIds.includes(option.id))

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        if (mandatory && !next) {
          return
        }

        if (!next) {
          setStep('agents')
          setSelectedAgentIds([])
          setApprovalPolicyId(DEFAULT_CODEX_APPROVAL_POLICY_ID)
          setModeDrafts([])
          setError(null)
        }

        onOpenChange?.(next)
      }}
    >
      <DialogContent
        key={openKey}
        showCloseButton={!mandatory}
        className="sm:max-w-lg"
        onPointerDownOutside={(event) => {
          if (mandatory) {
            event.preventDefault()
          }
        }}
        onEscapeKeyDown={(event) => {
          if (mandatory) {
            event.preventDefault()
          }
        }}
      >
        {step === 'agents' ? (
          <>
            <DialogHeader>
              <DialogTitle>Select your agents</DialogTitle>
              <DialogDescription>
                Orchi needs to know which agents you have installed before you can chat. Select
                Cursor, Codex, or both — next you will set model defaults for each chat mode.
              </DialogDescription>
            </DialogHeader>

            <fieldset className="space-y-2" disabled={busy}>
              <legend className="sr-only">Available agents</legend>
              {options.map((option) => {
                const selected = selectedAgentIds.includes(option.id)
                return (
                  <label
                    key={option.id}
                    className={cn(
                      'flex cursor-pointer items-start gap-3 rounded-lg border px-3 py-2.5 text-sm transition-colors',
                      selected ? 'border-primary bg-primary/5' : 'border-border hover:bg-accent/40',
                      busy && 'pointer-events-none opacity-70'
                    )}
                  >
                    <input
                      type="checkbox"
                      checked={selected}
                      onChange={() => toggleAgent(option.id)}
                      className="mt-0.5 size-4 shrink-0 rounded border border-input"
                    />
                    <span className="min-w-0 space-y-0.5">
                      <span className="block font-medium">{option.label}</span>
                      <span className="block text-xs text-muted-foreground">
                        {option.description}
                      </span>
                    </span>
                  </label>
                )
              })}
            </fieldset>
          </>
        ) : null}

        {step === 'codex-approvals' ? (
          <>
            <DialogHeader>
              <DialogTitle>Codex approvals</DialogTitle>
              <DialogDescription>
                Choose how Codex should handle commands and file changes. You can change this later
                under Settings → Agents or per chat in the composer.
              </DialogDescription>
            </DialogHeader>

            <fieldset className="space-y-2" disabled={busy}>
              <legend className="sr-only">Codex approval policy</legend>
              {CODEX_APPROVAL_SETUP_OPTIONS.map((option) => {
                const selected = approvalPolicyId === option.id
                return (
                  <label
                    key={option.id}
                    className={cn(
                      'flex cursor-pointer items-start gap-3 rounded-lg border px-3 py-2.5 text-sm transition-colors',
                      selected ? 'border-primary bg-primary/5' : 'border-border hover:bg-accent/40',
                      busy && 'pointer-events-none opacity-70'
                    )}
                  >
                    <input
                      type="radio"
                      name="codex-approval-policy"
                      checked={selected}
                      onChange={() => {
                        setError(null)
                        setApprovalPolicyId(option.id)
                      }}
                      className="mt-0.5 size-4 shrink-0 border border-input"
                    />
                    <span className="min-w-0 space-y-0.5">
                      <span className="block font-medium">{option.label}</span>
                      <span className="block text-xs text-muted-foreground">
                        {option.description}
                      </span>
                    </span>
                  </label>
                )
              })}
            </fieldset>
          </>
        ) : null}

        {step === 'mode-defaults' ? (
          <>
            <DialogHeader>
              <DialogTitle>Set mode defaults</DialogTitle>
              <DialogDescription>
                Pick the model (and Codex reasoning) for Orchestrator, Implementation/default, and
                Review. For Codex, Terra + Medium shows as “5.6 Terra Medium” — the same pairing
                you choose in the Codex app.
              </DialogDescription>
            </DialogHeader>

            <div className="max-h-[50vh] space-y-3 overflow-y-auto pr-1">
              {modeDrafts.map((draft) => (
                <ModeDefaultSetupRow
                  key={draft.mode}
                  draft={draft}
                  agents={selectedAgents}
                  disabled={busy}
                  onChange={(next) =>
                    setModeDrafts((current) =>
                      current.map((entry) => (entry.mode === next.mode ? next : entry))
                    )
                  }
                />
              ))}
            </div>
          </>
        ) : null}

        {error ? <p className="text-sm text-destructive">{error}</p> : null}

        <DialogFooter className="gap-2 sm:justify-between">
          {step !== 'agents' ? (
            <Button
              type="button"
              variant="ghost"
              disabled={busy}
              onClick={() => {
                setError(null)
                if (step === 'mode-defaults') {
                  setStep(codexSelected ? 'codex-approvals' : 'agents')
                  return
                }

                setStep('agents')
              }}
            >
              Back
            </Button>
          ) : (
            <span />
          )}

          <Button
            type="button"
            disabled={busy || (step === 'agents' && selectedAgentIds.length === 0)}
            onClick={() => {
              if (step === 'agents') {
                handleAgentsContinue()
                return
              }

              if (step === 'codex-approvals') {
                handleApprovalsContinue()
                return
              }

              void completeSetup()
            }}
          >
            {busy
              ? 'Saving…'
              : step === 'mode-defaults'
                ? 'Finish'
                : 'Continue'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
