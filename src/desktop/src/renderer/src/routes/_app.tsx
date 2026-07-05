import { createFileRoute } from '@tanstack/react-router'

import { AppLayout } from '@/components/layout/app-layout'
import { DeleteChatProvider } from '@/providers/delete-chat-provider'
import { ChatProvider } from '@/providers/chat-provider'
import { ProjectProvider } from '@/providers/project-provider'
import { ProjectLayoutProvider } from '@/providers/project-layout-provider'

export const Route = createFileRoute('/_app')({
  component: AppLayoutRoute
})

function AppLayoutRoute(): React.JSX.Element {
  return (
    <ProjectProvider>
      <ChatProvider>
        <DeleteChatProvider>
          <ProjectLayoutProvider>
            <AppLayout />
          </ProjectLayoutProvider>
        </DeleteChatProvider>
      </ChatProvider>
    </ProjectProvider>
  )
}
