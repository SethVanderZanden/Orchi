import { Outlet } from '@tanstack/react-router'

import { SplitChatPane } from '@/components/layout/split-chat-pane'
import { ResizableHandle, ResizablePanel, ResizablePanelGroup } from '@/components/ui/resizable'
import { useChatTabs } from '@/providers/chat-tabs-provider'

export function ProjectShellLayout(): React.JSX.Element {
  const { splitTabId, activeTabId, clearSplit } = useChatTabs()
  const showSplit = Boolean(splitTabId && activeTabId && splitTabId !== activeTabId)

  return (
    <div className="flex h-full min-h-0 min-w-0 flex-1 bg-background">
      {showSplit && splitTabId ? (
        <ResizablePanelGroup orientation="horizontal" className="h-full min-h-0 min-w-0 flex-1">
          <ResizablePanel defaultSize="50" minSize="25" className="min-h-0 min-w-0">
            <div className="flex h-full min-h-0 min-w-0 flex-col">
              <Outlet />
            </div>
          </ResizablePanel>
          <ResizableHandle withHandle />
          <ResizablePanel defaultSize="50" minSize="25" className="min-h-0 min-w-0">
            <div className="flex h-full min-h-0 min-w-0 flex-col">
              <SplitChatPane chatId={splitTabId} onCloseSplit={clearSplit} />
            </div>
          </ResizablePanel>
        </ResizablePanelGroup>
      ) : (
        <div className="flex h-full min-h-0 min-w-0 flex-1 flex-col">
          <Outlet />
        </div>
      )}
    </div>
  )
}
