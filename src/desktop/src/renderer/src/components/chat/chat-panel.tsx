import { ChatLayout } from '@/components/chat/chat-layout'
import { OrchiChatComposer } from '@/components/chat/chat-composer'
import { OrchiChatMessageList } from '@/components/chat/chat-message-list'
import { PlanCards } from '@/components/orchestration/plan-cards'
import type { ChatMarker, ChatMessage } from '@/lib/chat/types'
import type { ParsedPlan } from '@/lib/orchestration/parse-plans'

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
    <ChatLayout composer={<OrchiChatComposer disabled={isSending || isKickingOff} onSend={onSend} />}>
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
  )
}
