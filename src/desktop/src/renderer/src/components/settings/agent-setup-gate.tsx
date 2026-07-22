import { AgentSetupWizardDialog } from '@/components/settings/agent-setup-wizard-dialog'
import { useUserPreferences } from '@/hooks/use-user-preferences'

/**
 * Blocks the app until the user enables at least one agent (fresh DB default is none).
 * Always mount the dialog and toggle `open` — unmounting an open Radix Dialog can
 * leave `pointer-events: none` on `document.body` and freeze the UI.
 *
 * Preferences must load successfully before we treat an empty list as needing setup;
 * otherwise a transient API error would re-open the wizard every launch.
 */
export function AgentSetupGate({ children }: { children: React.ReactNode }): React.JSX.Element {
  const { needsAgentSetup, isLoading, isError, preferencesError } = useUserPreferences()
  const open = !isLoading && !isError && needsAgentSetup

  return (
    <>
      {children}
      {isError && preferencesError ? (
        <div className="fixed inset-x-0 bottom-0 z-50 border-t border-destructive/40 bg-background/95 px-4 py-3 text-sm text-destructive backdrop-blur">
          Could not load agent preferences: {preferencesError}
        </div>
      ) : null}
      <AgentSetupWizardDialog mandatory open={open} />
    </>
  )
}
