import { createFileRoute } from '@tanstack/react-router'

import { AppLayout } from '@/components/layout/app-layout'
import { ChatProvider } from '@/providers/chat-provider'
import { WorkspaceProvider } from '@/providers/workspace-provider'

export const Route = createFileRoute('/_app')({
  component: AppLayoutRoute
})

function AppLayoutRoute(): React.JSX.Element {
  return (
    <WorkspaceProvider>
      <ChatProvider>
        <AppLayout />
      </ChatProvider>
    </WorkspaceProvider>
  )
}
