import { Outlet } from '@tanstack/react-router'

export function WorkspaceShellLayout(): React.JSX.Element {
  return (
    <div className="flex h-full min-h-0 min-w-0 flex-1 bg-background">
      <div className="flex h-full min-h-0 min-w-0 flex-1 flex-col">
        <Outlet />
      </div>
    </div>
  )
}
