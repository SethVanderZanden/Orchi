import { VStack } from '@astryxdesign/core/Layout'
import { Toolbar } from '@astryxdesign/core/Toolbar'
import { Text } from '@astryxdesign/core/Text'
import { Token } from '@astryxdesign/core/Token'

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
    <VStack className="min-h-0 min-w-0 flex-1 overflow-hidden">
      <Toolbar
        label="Chat header"
        size="sm"
        dividers={['bottom']}
        startContent={
          <VStack gap={0}>
            <Text type="label" weight="semibold">
              {chat.title}
            </Text>
            <Text type="supporting" color="secondary">
              {chat.workspacePath} · {chat.messages.length} message
              {chat.messages.length === 1 ? '' : 's'}
            </Text>
            {chat.mode === 'orchestration' ? (
              <Token label="Orchestration" color="blue" size="sm" />
            ) : null}
            {chat.planFilePath ? (
              <Text type="supporting" color="secondary">
                Plan: {chat.planFilePath}
              </Text>
            ) : null}
          </VStack>
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
    </VStack>
  )
}
