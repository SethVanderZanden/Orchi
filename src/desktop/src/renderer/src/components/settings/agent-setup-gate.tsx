import { EnabledAgentsPickerDialog } from '@/components/settings/enabled-agents-picker-dialog'
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
      <EnabledAgentsPickerDialog
        mandatory
        open={open}
        title="Select your agents"
        description="Orchi needs to know which agents you have installed before you can chat. Select Cursor, Codex, or both from the list below. We will use your selection to set the first default agent for each chat mode."
      />
    </>
  )
}
