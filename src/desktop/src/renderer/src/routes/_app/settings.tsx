import { createFileRoute, useNavigate } from '@tanstack/react-router'
import { useQuery } from '@tanstack/react-query'

import { AgentModelsCard } from '@/components/settings/agent-models-card'
import { AgentContextSizesCard } from '@/components/settings/agent-context-sizes-card'
import { AgentCliOptionsCard } from '@/components/settings/agent-cli-options-card'
import { AutoKickOffReviewCard } from '@/components/settings/auto-kick-off-review-card'
import { EnabledAgentsCard } from '@/components/settings/enabled-agents-card'
import { ModeRuntimeDefaultsCard } from '@/components/settings/mode-runtime-defaults-card'
import { DefaultChatModeCard } from '@/components/settings/default-chat-mode-card'
import { PostMessageBehaviorCard } from '@/components/settings/post-message-behavior-card'
import { PreferredEditorCard } from '@/components/settings/preferred-editor-card'
import { ProjectsSettingsCard } from '@/components/settings/projects-settings-card'
import { SelectionActionsCard } from '@/components/settings/selection-actions-card'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { PageHeader } from '@/components/ui/page-header'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { useUserPreferences } from '@/hooks/use-user-preferences'
import { listAgents } from '@/lib/chat/agent-context-sizes-api'
import { agentKeys } from '@/lib/query-keys'
import { filterAgentsByEnabled } from '@/lib/user-preferences/enabled-agents'

export const Route = createFileRoute('/_app/settings')({
  component: SettingsPage
})

function SettingsPage(): React.JSX.Element {
  const navigate = useNavigate()
  const { enabledAgentIds } = useUserPreferences()
  const agentsQuery = useQuery({
    queryKey: agentKeys.list(),
    queryFn: listAgents,
    staleTime: 60 * 60 * 1000
  })

  const catalogAgents = agentsQuery.data ?? [
    { id: 'cursor', label: 'Cursor' },
    { id: 'codex', label: 'Codex' }
  ]
  const agents = filterAgentsByEnabled(catalogAgents, enabledAgentIds)

  return (
    <div className="flex h-full min-h-0 flex-col">
      <PageHeader title="Settings" description="Projects and app preferences" />

      <div className="flex-1 overflow-y-auto p-8">
        <div className="mx-auto w-full max-w-2xl">
          <Tabs defaultValue="general" className="gap-8">
            <TabsList variant="line" className="w-full justify-start">
              <TabsTrigger value="general">General</TabsTrigger>
              <TabsTrigger value="agents">Agents</TabsTrigger>
              <TabsTrigger value="selection">Selection</TabsTrigger>
            </TabsList>

            <TabsContent value="general" className="flex flex-col gap-8">
              <ProjectsSettingsCard />
              <PreferredEditorCard />
              <DefaultChatModeCard />
              <PostMessageBehaviorCard />
              <AutoKickOffReviewCard />
              <Card>
                <CardContent className="space-y-3">
                  <p className="text-sm leading-relaxed text-muted-foreground">
                    Jump back to a conversation without losing navigator state.
                  </p>
                  <Button variant="secondary" onClick={() => navigate({ to: '/' })}>
                    Open chats
                  </Button>
                </CardContent>
              </Card>
            </TabsContent>

            <TabsContent value="agents" className="flex flex-col gap-8">
              <EnabledAgentsCard />
              <ModeRuntimeDefaultsCard />
              {agents.map((agent) => (
                <div key={agent.id} className="flex flex-col gap-8">
                  <AgentModelsCard agentId={agent.id} agentLabel={agent.label} />
                  <AgentContextSizesCard agentId={agent.id} agentLabel={agent.label} />
                  <AgentCliOptionsCard
                    agentId={agent.id}
                    agentLabel={agent.label}
                    kind="model_reasoning_effort"
                  />
                  <AgentCliOptionsCard
                    agentId={agent.id}
                    agentLabel={agent.label}
                    kind="approval_policy"
                  />
                </div>
              ))}
            </TabsContent>

            <TabsContent value="selection" className="flex flex-col gap-8">
              <SelectionActionsCard />
            </TabsContent>
          </Tabs>
        </div>
      </div>
    </div>
  )
}
