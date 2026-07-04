import { useEffect, useMemo } from 'react'

import { PageHeader } from '@/components/ui/page-header'
import { ChatPanel } from '@/components/chat/chat-panel'
import { parsePlansFromMessages } from '@/lib/orchestration/parse-plans'
import { parseReviewPlansFromMessages } from '@/lib/orchestration/parse-review-plans'
import type { ParsedReviewPlan } from '@/lib/orchestration/parse-review-plans'
import type { ChatThread } from '@/lib/chat/types'
import { findReviewChildForPlan } from '@/lib/workspaces/chat-tree'
import { useChat } from '@/providers/chat-provider'

type ChatWorkspacePanelProps = {
  chat: ChatThread
}

export function ChatWorkspacePanel({ chat }: ChatWorkspacePanelProps): React.JSX.Element {
  const {
    chats,
    sendMessage,
    getMarkers,
    getChildChats,
    getChat,
    loadChat,
    kickOffPlan,
    kickOffAllPlans,
    updateChatMode,
    getModeUpdateError,
    isChatSending,
    isPlanKickingOff,
    isParentKickingOffAny
  } = useChat()
  const plans = chat.mode === 'orchestration' ? parsePlansFromMessages(chat.messages) : []
  const childChats = getChildChats(chat.id)
  const reviewPlansByPlanId = Object.fromEntries(
    plans.map((plan) => {
      const reviewChildSummary = findReviewChildForPlan(plan.planId, childChats)
      const reviewChild = reviewChildSummary ? getChat(reviewChildSummary.id) : undefined
      const reviewPlans = reviewChild
        ? parseReviewPlansFromMessages(reviewChild.messages)
        : []
      const reviewPlan = reviewPlans.find((item) => item.planId === plan.planId) ?? reviewPlans[0]
      return [plan.planId, reviewPlan] as const
    })
  ) as Record<string, ParsedReviewPlan | undefined>

  const childChatIds = useMemo(
    () => getChildChats(chat.id).map((child) => child.id).join(','),
    [chat.id, chats, getChildChats]
  )

  useEffect(() => {
    if (chat.mode !== 'orchestration') {
      return
    }

    for (const id of childChatIds.split(',').filter(Boolean)) {
      const child = getChat(id)
      if (child && child.messages.length === 0) {
        void loadChat(id)
      }
    }
  }, [chat.id, chat.mode, childChatIds, getChat, loadChat])

  const canChangeMode = !chat.messages.some(
    (message) => message.status === 'processing' || message.status === 'streaming'
  )

  return (
    <div className="flex min-h-0 min-w-0 flex-1 flex-col overflow-hidden">
      <PageHeader
        startContent={
          <div className="min-w-0 space-y-1">
            <p className="truncate text-sm font-semibold">{chat.title}</p>
            <p className="truncate text-xs text-muted-foreground">
              {chat.workspacePath} · {chat.messages.length} message
              {chat.messages.length === 1 ? '' : 's'}
              {childChats.length > 0
                ? ` · ${childChats.length} child agent${childChats.length === 1 ? '' : 's'}`
                : ''}
            </p>
            {chat.planFilePath ? (
              <p className="truncate text-xs text-muted-foreground">Plan: {chat.planFilePath}</p>
            ) : null}
          </div>
        }
      />

      <ChatPanel
        messages={chat.messages}
        markers={getMarkers(chat.id)}
        onSend={(content) => sendMessage(chat.id, content)}
        mode={chat.mode}
        canChangeMode={canChangeMode}
        modeUpdateError={getModeUpdateError(chat.id)}
        onModeChange={(mode) => void updateChatMode(chat.id, mode)}
        plans={plans}
        parentChatId={chat.id}
        isSending={isChatSending(chat.id)}
        isPlanKickingOff={isPlanKickingOff}
        isParentKickingOffAny={isParentKickingOffAny}
        onKickOffPlan={
          chat.mode === 'orchestration' ? (plan) => kickOffPlan(chat.id, plan) : undefined
        }
        onKickOffAllPlans={
          chat.mode === 'orchestration'
            ? () => kickOffAllPlans(chat.id, plans)
            : undefined
        }
        childChats={chat.mode === 'orchestration' ? childChats : undefined}
        reviewPlansByPlanId={chat.mode === 'orchestration' ? reviewPlansByPlanId : undefined}
      />
    </div>
  )
}
