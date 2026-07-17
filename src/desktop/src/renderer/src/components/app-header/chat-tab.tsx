import { Columns2, Pin, PinOff, X } from 'lucide-react'

import { ChatStatusDot } from '@/components/chat/chat-status-dot'
import { Button } from '@/components/ui/button'
import type { ChatStatusVariant } from '@/lib/chat/chat-status-variant'
import type { AgentMode } from '@/lib/chat/types'
import { cn } from '@/lib/utils'

const CHAT_TAB_MIME = 'application/x-orchi-chat-tab'

type ChatTabProps = {
  chatId: string
  title: string
  projectName: string | null
  statusVariant: ChatStatusVariant
  mode: AgentMode
  isActive: boolean
  isSplit: boolean
  isPinned: boolean
  canPin: boolean
  onSelect: () => void
  onTogglePin: () => void
  onOpenBeside: () => void
  onClose: () => void
}

export function ChatTab({
  chatId,
  title,
  projectName,
  statusVariant,
  mode,
  isActive,
  isSplit,
  isPinned,
  canPin,
  onSelect,
  onTogglePin,
  onOpenBeside,
  onClose
}: ChatTabProps): React.JSX.Element {
  const label = projectName ? `${projectName} · ${title}` : title
  const PinIcon = isPinned ? PinOff : Pin
  const showOpenBeside = !isActive && !isSplit

  return (
    <div
      draggable
      onDragStart={(event) => {
        event.dataTransfer.setData(CHAT_TAB_MIME, chatId)
        event.dataTransfer.effectAllowed = 'move'
      }}
      onAuxClick={(event) => {
        if (event.button !== 1) {
          return
        }

        event.preventDefault()
        onClose()
      }}
      className={cn(
        'group/tab relative flex h-9 min-w-[140px] max-w-[240px] shrink cursor-grab items-center gap-0.5 rounded-lg active:cursor-grabbing',
        isActive
          ? 'bg-accent text-accent-foreground'
          : isSplit
            ? 'bg-accent/50 text-accent-foreground ring-1 ring-border'
            : 'text-muted-foreground hover:bg-accent/60 hover:text-foreground',
        isPinned && !isActive && 'ring-1 ring-border/60'
      )}
    >
      <button
        type="button"
        className="flex min-w-0 flex-1 items-center gap-1.5 px-2.5 py-1 text-left text-sm"
        onClick={onSelect}
        aria-current={isActive ? 'page' : undefined}
        title={`${label}${isSplit ? ' (split)' : ''}${isPinned ? ' (pinned)' : ''}`}
      >
        <ChatStatusDot variant={statusVariant} mode={mode} />
        <span className="min-w-0 truncate">
          {projectName ? (
            <>
              <span
                className={cn(isActive ? 'text-accent-foreground/70' : 'text-muted-foreground')}
              >
                {projectName}
              </span>
              <span className="mx-1 opacity-40">·</span>
            </>
          ) : null}
          <span
            className={cn(
              'font-medium',
              isActive ? 'text-accent-foreground' : 'text-foreground/80'
            )}
          >
            {title}
          </span>
        </span>
      </button>
      <Button
        type="button"
        variant="ghost"
        size="icon"
        className={cn(
          'size-5 shrink-0 text-muted-foreground opacity-0 transition-opacity',
          'group-hover/tab:opacity-100 focus-visible:opacity-100',
          (isActive || isPinned) && 'opacity-70 hover:opacity-100',
          !canPin && !isPinned && 'pointer-events-none opacity-0'
        )}
        aria-label={isPinned ? `Unpin ${title}` : `Pin ${title}`}
        title={
          isPinned
            ? 'Unpin tab'
            : canPin
              ? 'Pin tab (keeps visible when tabs overflow)'
              : 'Maximum pinned tabs reached'
        }
        disabled={!canPin && !isPinned}
        onClick={(event) => {
          event.stopPropagation()
          onTogglePin()
        }}
      >
        <PinIcon className={cn('size-3', isPinned && 'fill-current')} />
      </Button>
      <Button
        type="button"
        variant="ghost"
        size="icon"
        className={cn(
          'size-5 shrink-0 text-muted-foreground opacity-0 transition-opacity',
          'group-hover/tab:opacity-100 focus-visible:opacity-100',
          !showOpenBeside && 'pointer-events-none opacity-0'
        )}
        aria-label={`Open ${title} beside`}
        title="Open beside"
        disabled={!showOpenBeside}
        onClick={(event) => {
          event.stopPropagation()
          onOpenBeside()
        }}
      >
        <Columns2 className="size-3" />
      </Button>
      <Button
        type="button"
        variant="ghost"
        size="icon"
        className={cn(
          'mr-0.5 size-5 shrink-0 text-muted-foreground opacity-0 transition-opacity',
          'group-hover/tab:opacity-100 focus-visible:opacity-100',
          isActive && 'opacity-70 hover:opacity-100'
        )}
        aria-label={`Close ${title}`}
        onClick={(event) => {
          event.stopPropagation()
          onClose()
        }}
      >
        <X className="size-3" />
      </Button>
    </div>
  )
}
