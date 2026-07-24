import { AgentsSidebarContent } from '@/components/agents-sidebar/agents-sidebar-content'
import { SidebarResizeHandle } from '@/components/layout/sidebar/sidebar-resize-handle'
import { useSidebarWidth } from '@/hooks/use-sidebar-width'

export function AgentsSidebar(): React.JSX.Element {
  const { sidebarWidth, setSidebarWidth, commitSidebarWidth } = useSidebarWidth()

  return (
    <div className="flex h-full shrink-0" style={{ width: sidebarWidth }}>
      <aside
        className="flex h-full min-w-0 flex-1 flex-col border-r border-border bg-sidebar text-sidebar-foreground"
        aria-label="Agents sidebar"
      >
        <AgentsSidebarContent />
      </aside>
      <SidebarResizeHandle
        width={sidebarWidth}
        onWidthChange={setSidebarWidth}
        onWidthCommit={commitSidebarWidth}
      />
    </div>
  )
}
