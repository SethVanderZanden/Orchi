import { useState } from 'react'

import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from '@/components/ui/dialog'
import { useUserPreferences } from '@/hooks/use-user-preferences'
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

type SetupStep = 'agents' | 'codex-approvals'

export function AgentSetupWizardDialog({
  mandatory = false,
  open,
  onOpenChange
}: AgentSetupWizardDialogProps): React.JSX.Element {
  const { setEnabledAgentIds, isUpdating } = useUserPreferences()
  const options = getAvailableAgentOptions()
  const [step, setStep] = useState<SetupStep>('agents')
  const [selectedAgentIds, setSelectedAgentIds] = useState<string[]>([])
  const [approvalPolicyId, setApprovalPolicyId] = useState<CodexApprovalSetupOptionId>(
    DEFAULT_CODEX_APPROVAL_POLICY_ID
  )
  const [error, setError] = useState<string | null>(null)
  const [openKey, setOpenKey] = useState(open ? 'open' : 'closed')
  const [prevOpen, setPrevOpen] = useState(open)

  if (open !== prevOpen) {
    setPrevOpen(open)
    if (open) {
      setStep('agents')
      setSelectedAgentIds([])
      setApprovalPolicyId(DEFAULT_CODEX_APPROVAL_POLICY_ID)
      setError(null)
      setOpenKey(`open:${Date.now()}`)
    }
  }

  const codexSelected = selectedAgentIds.includes('codex')

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

    void completeSetup(selectedAgentIds)
  }

  async function completeSetup(
    agentIds: string[],
    codexApproval?: CodexApprovalSetupOptionId
  ): Promise<void> {
    try {
      await setEnabledAgentIds(agentIds, {
        codexApprovalPolicyId: codexApproval ?? null
      })
      onOpenChange?.(false)
    } catch (saveError) {
      setError(saveError instanceof Error ? saveError.message : 'Unable to save agent preferences.')
    }
  }

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
                Cursor, Codex, or both — we will use your selection to set the first default agent
                for each chat mode.
              </DialogDescription>
            </DialogHeader>

            <fieldset className="space-y-2" disabled={isUpdating}>
              <legend className="sr-only">Available agents</legend>
              {options.map((option) => {
                const selected = selectedAgentIds.includes(option.id)
                return (
                  <label
                    key={option.id}
                    className={cn(
                      'flex cursor-pointer items-start gap-3 rounded-lg border px-3 py-2.5 text-sm transition-colors',
                      selected ? 'border-primary bg-primary/5' : 'border-border hover:bg-accent/40',
                      isUpdating && 'pointer-events-none opacity-70'
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
        ) : (
          <>
            <DialogHeader>
              <DialogTitle>Codex approvals</DialogTitle>
              <DialogDescription>
                Choose how Codex should handle commands and file changes. You can change this later
                under Settings → Agents or per chat in the composer.
              </DialogDescription>
            </DialogHeader>

            <fieldset className="space-y-2" disabled={isUpdating}>
              <legend className="sr-only">Codex approval policy</legend>
              {CODEX_APPROVAL_SETUP_OPTIONS.map((option) => {
                const selected = approvalPolicyId === option.id
                return (
                  <label
                    key={option.id}
                    className={cn(
                      'flex cursor-pointer items-start gap-3 rounded-lg border px-3 py-2.5 text-sm transition-colors',
                      selected ? 'border-primary bg-primary/5' : 'border-border hover:bg-accent/40',
                      isUpdating && 'pointer-events-none opacity-70'
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
        )}

        {error ? <p className="text-sm text-destructive">{error}</p> : null}

        <DialogFooter className="gap-2 sm:justify-between">
          {step === 'codex-approvals' ? (
            <Button
              type="button"
              variant="ghost"
              disabled={isUpdating}
              onClick={() => {
                setError(null)
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
            disabled={isUpdating || (step === 'agents' && selectedAgentIds.length === 0)}
            onClick={() => {
              if (step === 'agents') {
                handleAgentsContinue()
                return
              }

              void completeSetup(selectedAgentIds, approvalPolicyId)
            }}
          >
            {isUpdating ? 'Saving…' : step === 'agents' && codexSelected ? 'Continue' : 'Finish'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
