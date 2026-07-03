import { Outlet } from '@tanstack/react-router'
import type { CSSProperties } from 'react'

const mainPane: CSSProperties = {
  flex: 1,
  minWidth: 0,
  minHeight: 0,
  height: '100%',
  display: 'flex',
  flexDirection: 'column'
}

const shellRoot: CSSProperties = {
  flex: 1,
  minWidth: 0,
  minHeight: 0,
  height: '100%'
}

export function WorkspaceShellLayout(): React.JSX.Element {
  return (
    <div style={shellRoot}>
      <div style={mainPane}>
        <Outlet />
      </div>
    </div>
  )
}
