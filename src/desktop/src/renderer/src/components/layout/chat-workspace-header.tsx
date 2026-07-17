import { Columns2, FileText, Trash2 } from 'lucide-react'

import { ShortcutHint } from '@/components/app-header/shortcut-hint'
import { OpenInEditorMenu } from '@/components/layout/open-in-editor-menu'
import { PageHeader } from '@/components/ui/page-header'
import { Button } from '@/components/ui/button'
import type { ChatThread } from '@/lib/chat/types'

type ChatWorkspaceHeaderProps = {
  chat: ChatThread
  projectName: string | null
  childChatCount: number
  workspacePath: string
  parentChat: ChatThread | null
  showPlanReview: boolean
  reviewPanelOpen: boolean
  hasReviewReady: boolean
  onToggleReviewPanel: () => void
  onOpenParentBeside: () => void
  onDelete: () => void
  deleteDisabled: boolean
}

export function ChatWorkspaceHeader({
  chat,
  projectName,
  childChatCount,
  workspacePath,
  parentChat,
  showPlanReview,
  reviewPanelOpen,
  hasReviewReady,
  onToggleReviewPanel,
  onOpenParentBeside,
  onDelete,
  deleteDisabled
}: ChatWorkspaceHeaderProps): React.JSX.Element {
  return (
    <PageHeader
      startContent={
        <div className="min-w-0 space-y-0.5">
          <p className="truncate text-xs text-muted-foreground">
            {projectName ? `${projectName} · ` : ''}
            {chat.workspacePath} · {chat.messages.length} message
            {chat.messages.length === 1 ? '' : 's'}
            {childChatCount > 0
              ? ` · ${childChatCount} child agent${childChatCount === 1 ? '' : 's'}`
              : ''}
          </p>
          {parentChat ? (
            <div className="flex min-w-0 items-center gap-2">
              <p className="min-w-0 truncate text-xs text-muted-foreground">
                Parent: <span className="text-foreground/80">{parentChat.title}</span>
              </p>
              <Button
                type="button"
                variant="outline"
                size="sm"
                className="h-6 shrink-0 gap-1.5 px-2 text-[11px] font-normal"
                onClick={onOpenParentBeside}
                title="Open parent beside"
                aria-label={`Open parent ${parentChat.title} beside`}
              >
                <Columns2 className="size-3" />
                Open beside
                <ShortcutHint>Ctrl+↑</ShortcutHint>
              </Button>
            </div>
          ) : null}
          {chat.planFilePath ? (
            <p className="truncate text-xs text-muted-foreground">Plan: {chat.planFilePath}</p>
          ) : null}
        </div>
      }
      endContent={
        <>
          <OpenInEditorMenu workspacePath={workspacePath} />
          {showPlanReview ? (
            <Button
              variant={reviewPanelOpen ? 'default' : 'outline'}
              size="sm"
              className="h-8 gap-1.5 px-3 text-xs font-normal"
              onClick={onToggleReviewPanel}
            >
              <FileText className="size-3.5" />
              Review
              <ShortcutHint>Ctrl+R</ShortcutHint>
              {hasReviewReady && !reviewPanelOpen ? (
                <span className="size-1.5 rounded-full bg-primary" aria-label="Review ready" />
              ) : null}
            </Button>
          ) : null}
          <Button
            variant="ghost"
            size="icon"
            className="size-8"
            aria-label={`Delete ${chat.title}`}
            disabled={deleteDisabled}
            onClick={onDelete}
          >
            <Trash2 className="size-4" />
          </Button>
        </>
      }
    />
  )
}
