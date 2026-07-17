import { createFileRoute } from '@tanstack/react-router'

import { AppLayout } from '@/components/layout/app-layout'
import { AgentSetupGate } from '@/components/settings/agent-setup-gate'
import { DeleteChatProvider } from '@/providers/delete-chat-provider'
import { ChatProvider } from '@/providers/chat-provider'
import { ChatTabsProvider } from '@/providers/chat-tabs-provider'
import { ProjectProvider } from '@/providers/project-provider'

export const Route = createFileRoute('/_app')({
  component: AppLayoutRoute
})

function AppLayoutRoute(): React.JSX.Element {
  return (
    <ProjectProvider>
      <ChatProvider>
        <DeleteChatProvider>
          <ChatTabsProvider>
            <AgentSetupGate>
              <AppLayout />
            </AgentSetupGate>
          </ChatTabsProvider>
        </DeleteChatProvider>
      </ChatProvider>
    </ProjectProvider>
  )
}
