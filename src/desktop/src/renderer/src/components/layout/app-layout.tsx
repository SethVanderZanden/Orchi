import { HStack, Layout, LayoutContent } from '@astryxdesign/core/Layout'

import { WorkspaceShellLayout } from '@/components/layout/workspace-shell-layout'
import { WorkspaceNavigator } from '@/components/workspace/workspace-navigator'

export function AppLayout(): React.JSX.Element {
  return (
    <Layout
      height="fill"
      content={
        <LayoutContent padding={0} isScrollable={false}>
          <HStack height="100%" className="min-h-0 min-w-0">
            <WorkspaceNavigator />
            <WorkspaceShellLayout />
          </HStack>
        </LayoutContent>
      }
    />
  )
}
