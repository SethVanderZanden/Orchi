import { useEffect, useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Columns2, ExternalLink, FolderPlus, MessageSquare, Pin, Settings, X } from 'lucide-react'
import type { LucideIcon } from 'lucide-react'

import { ChatStatusDot } from '@/components/chat/chat-status-dot'
import {
  CommandDialog,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
  CommandShortcut
} from '@/components/ui/command'
import { useFinderCommands } from '@/hooks/use-finder-commands'
import { filterFinderCommands, isFinderCommandMode } from '@/lib/app-commands/finder-commands'
import { buildChatFinderGroups } from '@/lib/chat-finder/build-chat-finder-groups'
import { isLocalChat } from '@/lib/chat/chat-persistence'
import { searchChats } from '@/lib/chat/search-chats'
import type { ChatThread } from '@/lib/chat/types'
import { chatKeys } from '@/lib/query-keys'
import { useChat } from '@/providers/chat-context'
import { useChatTabs } from '@/providers/chat-tabs-provider'
import { useProjects } from '@/providers/project-provider'

const COMMAND_ICONS: Record<string, LucideIcon> = {
  'new-chat': MessageSquare,
  'open-beside': Columns2,
  'open-in-editor': ExternalLink,
  'close-tab': X,
  'close-all-chats': X,
  'close-all-but-pinned': X,
  'pin-chat': Pin,
  settings: Settings,
  'add-project': FolderPlus
}

type ChatCommandDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
}

const SEARCH_DEBOUNCE_MS = 200

function mergeSearchWithLocalDrafts(
  remote: ChatThread[],
  allChats: ChatThread[],
  query: string
): ChatThread[] {
  const localDrafts = allChats.filter((chat) => isLocalChat(chat.id))
  const matchingLocals = query
    ? localDrafts.filter((chat) => chat.title.toLowerCase().includes(query.toLowerCase()))
    : localDrafts

  const seen = new Set(matchingLocals.map((chat) => chat.id))
  const merged = [...matchingLocals]
  for (const chat of remote) {
    if (seen.has(chat.id)) {
      continue
    }
    seen.add(chat.id)
    merged.push(chat)
  }
  return merged
}

export function ChatCommandDialog({
  open,
  onOpenChange
}: ChatCommandDialogProps): React.JSX.Element {
  const { openChat } = useChatTabs()
  const { chats, getChatStatusVariant } = useChat()
  const { projects } = useProjects()
  const [query, setQuery] = useState('')
  const [debouncedQuery, setDebouncedQuery] = useState('')

  function handleOpenChange(nextOpen: boolean): void {
    if (!nextOpen) {
      setQuery('')
      setDebouncedQuery('')
    }
    onOpenChange(nextOpen)
  }

  useEffect(() => {
    const handle = window.setTimeout(() => {
      setDebouncedQuery(query.trim())
    }, SEARCH_DEBOUNCE_MS)

    return () => window.clearTimeout(handle)
  }, [query])

  const commandMode = isFinderCommandMode(debouncedQuery)

  const searchQuery = useQuery({
    queryKey: chatKeys.search(debouncedQuery),
    queryFn: () => searchChats({ q: debouncedQuery || undefined }),
    enabled: open && !commandMode,
    placeholderData: (previous) => previous
  })

  const mergedChats = useMemo(
    () => mergeSearchWithLocalDrafts(searchQuery.data ?? [], chats, debouncedQuery),
    [chats, debouncedQuery, searchQuery.data]
  )

  const groups = buildChatFinderGroups(mergedChats, projects)
  const recentGroup = groups.find((group) => group.id === 'recent')
  const otherGroups = groups.filter((group) => group.id !== 'recent')
  const commands = useFinderCommands(() => handleOpenChange(false))
  const filteredCommands = useMemo(
    () => filterFinderCommands(commands, debouncedQuery),
    [commands, debouncedQuery]
  )
  const hasResults = filteredCommands.length > 0 || (!commandMode && groups.length > 0)

  function renderChatGroup(group: (typeof groups)[number]): React.JSX.Element {
    return (
      <CommandGroup key={group.id} heading={group.heading}>
        {group.chats.map((chat) => (
          <CommandItem
            key={`${group.id}:${chat.id}`}
            value={`${group.id}-${chat.id}-${chat.title}`}
            onSelect={() => {
              openChat(chat.id)
              handleOpenChange(false)
            }}
          >
            <ChatStatusDot variant={getChatStatusVariant(chat)} mode={chat.mode} />
            <span className="truncate">{chat.title}</span>
          </CommandItem>
        ))}
      </CommandGroup>
    )
  }

  return (
    <CommandDialog
      open={open}
      onOpenChange={handleOpenChange}
      title="Find chat"
      description="Search chats and run commands"
      shouldFilter={false}
      compact
    >
      <CommandInput
        placeholder="Search chats… (type > for commands)"
        value={query}
        onValueChange={setQuery}
      />
      <CommandList>
        {!commandMode && searchQuery.isError ? (
          <div className="px-3 py-4 text-center text-xs text-destructive">
            {searchQuery.error instanceof Error
              ? searchQuery.error.message
              : 'Failed to search chats'}
          </div>
        ) : (
          <>
            {!hasResults ? (
              <CommandEmpty className="py-4 text-xs">
                {commandMode
                  ? 'No matching commands.'
                  : searchQuery.isFetching
                    ? 'Searching…'
                    : 'No results found.'}
              </CommandEmpty>
            ) : null}
            {!commandMode && recentGroup ? renderChatGroup(recentGroup) : null}
            {filteredCommands.length > 0 ? (
              <CommandGroup heading="Commands">
                {filteredCommands.map((command) => {
                  const Icon = COMMAND_ICONS[command.id]

                  return (
                    <CommandItem
                      key={command.id}
                      value={`command-${command.id}-${command.label}`}
                      disabled={command.disabled}
                      onSelect={command.onSelect}
                    >
                      {Icon ? <Icon /> : null}
                      <span className="truncate">{command.label}</span>
                      {command.shortcut ? (
                        <CommandShortcut className="font-mono text-[10px] tracking-normal">
                          {command.shortcut}
                        </CommandShortcut>
                      ) : null}
                    </CommandItem>
                  )
                })}
              </CommandGroup>
            ) : null}
            {!commandMode ? otherGroups.map((group) => renderChatGroup(group)) : null}
          </>
        )}
      </CommandList>
    </CommandDialog>
  )
}
