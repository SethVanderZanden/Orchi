import { Badge } from '@/components/ui/badge'
import { PageHeader } from '@/components/ui/page-header'
import { ChatPanel } from '@/components/chat/chat-panel'
import { parsePlansFromMessages } from '@/lib/orchestration/parse-plans'
import type { ChatThread } from '@/lib/chat/types'
import { useChat } from '@/providers/chat-provider'

type ChatWorkspacePanelProps = {
  chat: ChatThread
}

export function ChatWorkspacePanel({ chat }: ChatWorkspacePanelProps): React.JSX.Element {
  const {
    sendMessage,
    getMarkers,
    kickOffPlan,
    isKickingOff,
    kickingOffPlanId,
    isSending
  } = useChat()
  const plans = chat.mode === 'orchestration' ? parsePlansFromMessages(chat.messages) : []

  return (
    <div className="flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden">
      <PageHeader
        startContent={
          <div className="min-w-0 space-y-1">
            <p className="truncate text-sm font-semibold">{chat.title}</p>
            <p className="truncate text-xs text-muted-foreground">
              {chat.workspacePath} · {chat.messages.length} message
              {chat.messages.length === 1 ? '' : 's'}
            </p>
            {chat.mode === 'orchestration' ? (
              <Badge variant="secondary" className="text-[10px]">
                Orchestration
              </Badge>
            ) : null}
            {chat.planFilePath ? (
              <p className="truncate text-xs text-muted-foreground">Plan: {chat.planFilePath}</p>
            ) : null}
          </div>
        }
      />

      <ChatPanel
        messages={chat.messages}
        markers={getMarkers(chat.id)}
        isSending={isSending}
        onSend={(content) => sendMessage(chat.id, content)}
        plans={plans}
        isKickingOff={isKickingOff}
        kickingOffPlanId={kickingOffPlanId}
        onKickOffPlan={
          chat.mode === 'orchestration' ? (plan) => kickOffPlan(chat.id, plan) : undefined
        }
      />
    </div>
  )
}
