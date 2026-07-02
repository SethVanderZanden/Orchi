import { Outlet } from '@tanstack/react-router'
import { HStack, Layout, LayoutContent } from '@astryxdesign/core/Layout'
import type { CSSProperties } from 'react'

import { WorkspaceNavigator } from '@/components/workspace/workspace-navigator'

const mainPane: CSSProperties = {
  flex: 1,
  minWidth: 0,
  minHeight: 0,
  height: '100%',
  display: 'flex',
  flexDirection: 'column'
}

export function AppLayout(): React.JSX.Element {
  return (
    <Layout
      height="fill"
      content={
        <LayoutContent padding={0} isScrollable={false}>
          <HStack height="100%">
            <WorkspaceNavigator />
            <div style={mainPane}>
              <Outlet />
            </div>
          </HStack>
        </LayoutContent>
      }
    />
  )
}
