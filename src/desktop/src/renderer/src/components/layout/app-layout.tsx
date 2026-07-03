import { WorkspaceNavigator } from '@/components/workspace/workspace-navigator'
import { WorkspaceShellLayout } from '@/components/layout/workspace-shell-layout'

export function AppLayout(): React.JSX.Element {
  return (
    <div className="flex h-full min-h-0 min-w-0">
      <WorkspaceNavigator />
      <WorkspaceShellLayout />
    </div>
  )
}
