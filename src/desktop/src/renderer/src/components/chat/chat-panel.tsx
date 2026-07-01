import { useNavigate } from '@tanstack/react-router'
import { PanelRightIcon, XIcon } from 'lucide-react'
import { useDefaultLayout } from 'react-resizable-panels'

import type { ChatThread } from '@/lib/chat/types'
import { formatChatMode } from '@/lib/chat/types'
import { usePlanPreview } from '@/hooks/use-plan-preview'
import { useChat } from '@/providers/chat-provider'
import { AppPageHeader } from '@/components/layout/app-page-header'
import { ChatComposer } from '@/components/chat/chat-composer'
import { ChatMessageList } from '@/components/chat/chat-message-list'
import { ChatModeSelect } from '@/components/chat/chat-mode-select'
import { PlanPreviewPanel } from '@/components/chat/plan-preview-panel'
import { Button } from '@/components/ui/button'
import {
  ResizableHandle,
  ResizablePanel,
  ResizablePanelGroup
} from '@/components/ui/resizable'
import { cn } from '@/lib/utils'

type ChatPanelProps = {
  chat: ChatThread
}

export function ChatPanel({ chat }: ChatPanelProps): React.JSX.Element {
  const navigate = useNavigate()
  const { sendMessage, closeChat, updateChatMode, isSending, isUpdatingMode, getMarkers } = useChat()
  const planPreview = usePlanPreview(chat)

  const { defaultLayout, onLayoutChanged } = useDefaultLayout({
    id: 'orchi-chat-plan-preview',
    storage: localStorage,
    panelIds: ['chat', 'plan']
  })

  async function handleCloseChat(): Promise<void> {
    await closeChat(chat.id)
    navigate({ to: '/' })
  }

  const chatColumn = (
    <div className="flex h-full min-h-0 flex-1 flex-col">
      <AppPageHeader
        title={chat.title}
        description={`${chat.workspacePath} · ${formatChatMode(chat.mode)} · ${chat.messages.length} message${chat.messages.length === 1 ? '' : 's'}`}
      >
        {planPreview.hasPlanContent ? (
          <Button
            size="icon-sm"
            variant={planPreview.isOpen ? 'secondary' : 'ghost'}
            onClick={planPreview.togglePanel}
            aria-label="Toggle plan panel"
          >
            <PanelRightIcon />
          </Button>
        ) : null}
        <ChatModeSelect
          value={chat.mode}
          disabled={isSending || isUpdatingMode}
          onChange={(mode, attachedPlanId) => updateChatMode(chat.id, mode, attachedPlanId)}
          className="w-[140px]"
        />
        <Button size="icon-sm" variant="ghost" onClick={handleCloseChat} aria-label="Close chat">
          <XIcon />
        </Button>
      </AppPageHeader>

      <div className="flex min-h-0 flex-1 flex-col">
        <ChatMessageList
          messages={chat.messages}
          markers={getMarkers(chat.id)}
          isPlanResponseMessage={planPreview.isPlanResponseMessage}
          onOpenPlan={planPreview.openPanel}
        />
      </div>

      <div className="shrink-0 border-t bg-background/80 px-4 py-4 backdrop-blur-sm">
        <ChatComposer
          disabled={isSending}
          onSend={(content) => sendMessage(chat.id, content)}
        />
      </div>
    </div>
  )

  if (!planPreview.hasPlanContent) {
    return chatColumn
  }

  return (
    <ResizablePanelGroup
      id="orchi-chat-plan-preview"
      orientation="horizontal"
      defaultLayout={defaultLayout}
      onLayoutChanged={onLayoutChanged}
      className="h-full min-h-0"
    >
      <ResizablePanel id="chat" defaultSize="58%" minSize="35%">
        {chatColumn}
      </ResizablePanel>
      <ResizableHandle withHandle />
      <ResizablePanel
        id="plan"
        defaultSize="42%"
        minSize="22%"
        collapsible
        collapsedSize="0%"
        panelRef={planPreview.planPanelRef}
        className={cn(!planPreview.isOpen && 'min-w-0')}
      >
        <PlanPreviewPanel
          title={planPreview.planTitle}
          content={planPreview.planContent}
          isStreaming={planPreview.isStreaming}
          isLoading={planPreview.isLoadingPlan}
          planId={planPreview.planId}
          isFromApi={planPreview.isFromApi}
          onClose={planPreview.closePanel}
        />
      </ResizablePanel>
    </ResizablePanelGroup>
  )
}
