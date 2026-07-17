import { ChevronDown } from 'lucide-react'

import { ChatStatusDot } from '@/components/chat/chat-status-dot'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger
} from '@/components/ui/dropdown-menu'
import type { ChatStatusVariant } from '@/lib/chat/chat-status-variant'
import type { AgentMode } from '@/lib/chat/types'
import { cn } from '@/lib/utils'

type OverflowTab = {
  chatId: string
  title: string
  projectName: string | null
  statusVariant: ChatStatusVariant
  mode: AgentMode
  isActive: boolean
  isSplit: boolean
}

type ChatTabOverflowMenuProps = {
  tabs: OverflowTab[]
  onSelect: (chatId: string) => void
}

export function ChatTabOverflowMenu({
  tabs,
  onSelect
}: ChatTabOverflowMenuProps): React.JSX.Element | null {
  if (tabs.length === 0) {
    return null
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          type="button"
          variant="outline"
          size="sm"
          className="h-8 shrink-0 gap-1 px-2 text-sm font-normal text-muted-foreground"
          aria-label={`${tabs.length} more chat tabs`}
        >
          <span>{tabs.length}</span>
          <ChevronDown className="size-3.5" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start" className="w-64">
        <DropdownMenuLabel>More chats</DropdownMenuLabel>
        <DropdownMenuSeparator />
        {tabs.map((tab) => {
          const label = tab.projectName ? `${tab.projectName} · ${tab.title}` : tab.title

          return (
            <DropdownMenuItem key={tab.chatId} onClick={() => onSelect(tab.chatId)}>
              <ChatStatusDot variant={tab.statusVariant} mode={tab.mode} />
              <span className="min-w-0 flex-1 truncate">{label}</span>
              {tab.isSplit ? <span className="text-xs text-muted-foreground">split</span> : null}
              {tab.isActive ? (
                <span
                  className={cn('size-1.5 shrink-0 rounded-full bg-primary', tab.isSplit && 'ml-0')}
                  aria-hidden
                />
              ) : null}
            </DropdownMenuItem>
          )
        })}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
