import { FileText, Trash2 } from 'lucide-react'

import { OpenInEditorMenu } from '@/components/layout/open-in-editor-menu'
import { PageHeader } from '@/components/ui/page-header'
import { Button } from '@/components/ui/button'
import type { ChatThread } from '@/lib/chat/types'

type ChatWorkspaceHeaderProps = {
  chat: ChatThread
  projectName: string | null
  childChatCount: number
  workspacePath: string
  showPlanReview: boolean
  reviewPanelOpen: boolean
  hasReviewReady: boolean
  onToggleReviewPanel: () => void
  onDelete: () => void
  deleteDisabled: boolean
}

export function ChatWorkspaceHeader({
  chat,
  projectName,
  childChatCount,
  workspacePath,
  showPlanReview,
  reviewPanelOpen,
  hasReviewReady,
  onToggleReviewPanel,
  onDelete,
  deleteDisabled
}: ChatWorkspaceHeaderProps): React.JSX.Element {
  return (
    <PageHeader
      startContent={
        <div className="min-w-0 space-y-1">
          <p className="truncate text-sm font-semibold">{chat.title}</p>
          <p className="truncate text-xs text-muted-foreground">
            {projectName ? `${projectName} · ` : ''}
            {chat.workspacePath} · {chat.messages.length} message
            {chat.messages.length === 1 ? '' : 's'}
            {childChatCount > 0
              ? ` · ${childChatCount} child agent${childChatCount === 1 ? '' : 's'}`
              : ''}
          </p>
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
              <kbd className="pointer-events-none rounded border border-border bg-muted px-1 py-0.5 font-mono text-[10px] text-muted-foreground">
                Ctrl+R
              </kbd>
              {hasReviewReady && !reviewPanelOpen ? (
                <span
                  className="size-1.5 rounded-full bg-primary"
                  aria-label="Review ready"
                />
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
