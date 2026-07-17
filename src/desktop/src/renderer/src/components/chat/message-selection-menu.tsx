import { useRef, type ReactNode } from 'react'
import { useQuery } from '@tanstack/react-query'
import { BookOpenText, MessageSquarePlus } from 'lucide-react'

import {
  ContextMenu,
  ContextMenuContent,
  ContextMenuGroup,
  ContextMenuItem,
  ContextMenuLabel,
  ContextMenuSeparator,
  ContextMenuTrigger
} from '@/components/ui/context-menu'
import { getSelectionTextWithin } from '@/lib/dom/get-selection-text-within'
import { applySelectionTemplate } from '@/lib/selection-actions/apply-template'
import { listSelectionActions } from '@/lib/selection-actions/api'
import { DEFINE_SELECTION_TEMPLATE } from '@/lib/selection-actions/types'
import { selectionActionKeys } from '@/lib/query-keys'
import { useChatTabs } from '@/providers/chat-tabs-provider'

type MessageSelectionMenuProps = {
  children: ReactNode
}

/** Right-click on a message selection → built-in and custom selection actions. */
export function MessageSelectionMenu({ children }: MessageSelectionMenuProps): React.JSX.Element {
  const { createAndOpenSplitTab, isCreatingTab } = useChatTabs()
  const selectedTextRef = useRef('')
  const customActionsQuery = useQuery({
    queryKey: selectionActionKeys.lists(),
    queryFn: listSelectionActions,
    staleTime: 60_000
  })
  const customActions = customActionsQuery.data ?? []

  return (
    <ContextMenu>
      <ContextMenuTrigger
        asChild
        onContextMenu={(event) => {
          const text = getSelectionTextWithin(event.currentTarget)
          selectedTextRef.current = text
          if (!text) {
            event.preventDefault()
          }
        }}
      >
        <div>{children}</div>
      </ContextMenuTrigger>
      <ContextMenuContent className="min-w-52">
        <ContextMenuGroup>
          <ContextMenuLabel>Selection</ContextMenuLabel>
          <ContextMenuItem
            disabled={isCreatingTab}
            onSelect={() => {
              const text = selectedTextRef.current
              if (!text) {
                return
              }
              void createAndOpenSplitTab({ initialDraft: text })
            }}
          >
            <MessageSquarePlus />
            Add to chat
          </ContextMenuItem>
          <ContextMenuItem
            disabled={isCreatingTab}
            onSelect={() => {
              const text = selectedTextRef.current
              if (!text) {
                return
              }
              void createAndOpenSplitTab({
                sendContent: applySelectionTemplate(DEFINE_SELECTION_TEMPLATE, text)
              })
            }}
          >
            <BookOpenText />
            Define selected text
          </ContextMenuItem>
        </ContextMenuGroup>

        {customActions.length > 0 ? (
          <>
            <ContextMenuSeparator />
            <ContextMenuGroup>
              <ContextMenuLabel>Custom</ContextMenuLabel>
              {customActions.map((action) => (
                <ContextMenuItem
                  key={action.id}
                  disabled={isCreatingTab}
                  onSelect={() => {
                    const text = selectedTextRef.current
                    if (!text) {
                      return
                    }
                    void createAndOpenSplitTab({
                      sendContent: applySelectionTemplate(action.template, text)
                    })
                  }}
                >
                  {action.label}
                </ContextMenuItem>
              ))}
            </ContextMenuGroup>
          </>
        ) : null}
      </ContextMenuContent>
    </ContextMenu>
  )
}
