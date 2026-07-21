import { AgentSetupWizardDialog } from '@/components/settings/agent-setup-wizard-dialog'
import { useUserPreferences } from '@/hooks/use-user-preferences'

/**
 * Blocks the app until the user enables at least one agent (fresh DB default is none).
 * Always mount the dialog and toggle `open` — unmounting an open Radix Dialog can
 * leave `pointer-events: none` on `document.body` and freeze the UI.
 */
export function AgentSetupGate({ children }: { children: React.ReactNode }): React.JSX.Element {
  const { needsAgentSetup, isLoading } = useUserPreferences()
  const open = !isLoading && needsAgentSetup

  return (
    <>
      {children}
      <AgentSetupWizardDialog mandatory open={open} />
    </>
  )
}
