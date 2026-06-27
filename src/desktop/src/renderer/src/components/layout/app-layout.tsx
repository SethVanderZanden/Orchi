import { Outlet } from '@tanstack/react-router'

import { ChatSidebar } from '@/components/chat/chat-sidebar'
import { SidebarInset, SidebarProvider } from '@/components/ui/sidebar'
import { TooltipProvider } from '@/components/ui/tooltip'

export function AppLayout(): React.JSX.Element {
  return (
    <TooltipProvider>
      <SidebarProvider defaultOpen className="h-svh min-h-0">
        <ChatSidebar />
        <SidebarInset className="min-h-0 overflow-hidden">
          <Outlet />
        </SidebarInset>
      </SidebarProvider>
    </TooltipProvider>
  )
}
