import type { CSSProperties } from 'react'
import { ChatLayout } from '@astryxdesign/core/Chat'

import { OrchiChatComposer } from '@/components/chat/chat-composer'
import { OrchiChatMessageList } from '@/components/chat/chat-message-list'
import { PlanCards } from '@/components/orchestration/plan-cards'
import type { ChatMarker, ChatMessage } from '@/lib/chat/types'
import type { ParsedPlan } from '@/lib/orchestration/parse-plans'

const chatLayout: CSSProperties = {
  flex: 1,
  minHeight: 0
}

type ChatPanelProps = {
  messages: ChatMessage[]
  markers: ChatMarker[]
  isSending: boolean
  onSend: (content: string) => void
  plans?: ParsedPlan[]
  isKickingOff?: boolean
  kickingOffPlanId?: string | null
  onKickOffPlan?: (plan: ParsedPlan) => void
}

export function ChatPanel({
  messages,
  markers,
  isSending,
  onSend,
  plans = [],
  isKickingOff = false,
  kickingOffPlanId = null,
  onKickOffPlan
}: ChatPanelProps): React.JSX.Element {
  return (
    <div className="flex h-full min-h-0 min-w-0 flex-1 flex-col overflow-hidden">
      <ChatLayout
        density="spacious"
        style={chatLayout}
        composer={<OrchiChatComposer disabled={isSending || isKickingOff} onSend={onSend} />}
      >
        <OrchiChatMessageList messages={messages} markers={markers} />
        {onKickOffPlan ? (
          <PlanCards
            plans={plans}
            isKickingOff={isKickingOff}
            kickingOffPlanId={kickingOffPlanId}
            onKickOff={onKickOffPlan}
          />
        ) : null}
      </ChatLayout>
    </div>
  )
}
