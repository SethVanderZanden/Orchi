import { createFileRoute } from '@tanstack/react-router'

import { AppLayout } from '@/components/layout/app-layout'
import { ChatProvider } from '@/providers/chat-provider'
import { ProjectProvider } from '@/providers/project-provider'
import { WorkspaceLayoutProvider } from '@/providers/workspace-layout-provider'

export const Route = createFileRoute('/_app')({
  component: AppLayoutRoute
})

function AppLayoutRoute(): React.JSX.Element {
  return (
    <ProjectProvider>
      <ChatProvider>
        <WorkspaceLayoutProvider>
          <AppLayout />
        </WorkspaceLayoutProvider>
      </ChatProvider>
    </ProjectProvider>
  )
}
